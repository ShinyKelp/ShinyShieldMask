﻿using BepInEx;
using LancerRemix.Cat;
using MoreSlugcats;
using RWCustom;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Permissions;
using System.Security;
using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ShinyShieldMask
{
    public class FaceMaskHooks
    {
        private bool hasLancerMod = false, hasDropButton = false;
        public static Dictionary<Player, FaceMask> playerFaceMasks;
        public static Dictionary<FaceMask, Player> faceMaskPlayers; //Double dictionaries to find either value easily

        public FaceMaskHooks()
        {
            playerFaceMasks?.Clear();
            faceMaskPlayers?.Clear();
            playerFaceMasks = new Dictionary<Player, FaceMask>();
            faceMaskPlayers = new Dictionary<FaceMask, Player>();
        }

        public void SetVariables(bool hasLancer = false, bool hasDrop = false)
        {
            hasLancerMod = hasLancer;
            hasDropButton = hasDrop;
        }


        public void Player_Destroy(On.Player.orig_Destroy orig, Player self)
        {
            Debug.Log("Called player destroy.");
            if (playerFaceMasks.ContainsKey(self))
            {
                playerFaceMasks.TryGetValue(self, out FaceMask fMask);
                playerFaceMasks.Remove(self);
                faceMaskPlayers.Remove(fMask);
            }
            orig(self);
        }


        private void HandleLancerConstructor(Player player)
        {
            var sub = ModifyCat.GetSub<CatSub.Cat.CatSupplement>(player);
            if (sub?.GetType().Name != "LunterSupplement")
            {
                FaceMask fMask = new FaceMask(player);
                playerFaceMasks.Add(player, fMask);
                faceMaskPlayers.Add(fMask, player);
            }
        }

        public bool IsPlayerWearingMask(Player player, out VultureMask mask)
        {
            mask = null;
            if (playerFaceMasks.TryGetValue(player, out FaceMask fMask) && fMask.HasAMask)
                mask = fMask.Mask;
            
            return !(mask is null);
        }

        public void ReleaseFaceMaskFromPlayer(Player player, bool fling = false)
        {
            if (!playerFaceMasks.TryGetValue(player, out FaceMask fMask))
                return;
            fMask.DropMask(fling);
        }

        public void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (ShinyShieldMaskOptions.wearableMask.Value)
            {
                //Don't add faceMask to Lunter (already has one, albeit slightly different)
                if (hasLancerMod)
                    HandleLancerConstructor(self);
                else if (!playerFaceMasks.ContainsKey(self))
                {
                    FaceMask fMask = new FaceMask(self);
                    playerFaceMasks.Add(self, fMask);
                    faceMaskPlayers.Add(fMask, self);
                }
            }
        }

        public void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            //Soft dependency of drop button: drop facemask if drop button pressed while holding nothing else
            if (hasDropButton && CheckIfDropPressed(self) 
                && playerFaceMasks.TryGetValue(self, out FaceMask fMask) && fMask.HasAMask)
            {
                bool hasAnyGrasp = false;
                foreach(Creature.Grasp grasp in self.grasps)
                {
                    if (!(grasp is null))
                    {
                        hasAnyGrasp = true;
                        break;
                    }
                }
                hasAnyGrasp = hasAnyGrasp || (bool)self.spearOnBack?.HasASpear || (bool)self.slugOnBack?.HasASlug;

                if (!hasAnyGrasp)
                {
                    self.room?.socialEventRecognizer.CreaturePutItemOnGround(fMask.Mask, self);
                    fMask.DropMask();
                    self.wantToPickUp = 0;
                    return;
                }
            }
            orig(self, eu);
            
        }

        private bool CheckIfDropPressed(Player player)
        {
            return ImprovedInput.CustomInputExt.IsKeyBound(player, DropButton.Api.Drop) && DropButton.Api.JustPressedDrop(player);
        }

        public void Player_GrabUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.Index = 0;
            //Drop mask on ground
            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>("spearOnBack"),
                x => x.MatchLdfld<Player.SpearOnBack>("spear")
            );
            c.Index += 8;
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<Player>>((self) =>
            {
                self.wantToPickUp = 0;
            });

            c.Index = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchLdfld<StoryGameSession>("saveState"),
                x => x.MatchLdfld<SaveState>("wearingCloak")
            );
            c.Index -= 15;
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<Player>>((self) =>
            {
                if (playerFaceMasks.TryGetValue(self, out FaceMask fMask) && fMask.HasAMask &&
                (self.room is null || !self.room.game.IsStorySession || !self.room.game.GetStorySession.saveState.wearingCloak))
                {
                    self.room?.socialEventRecognizer.CreaturePutItemOnGround(fMask.Mask, self);
                    fMask.DropMask(false);
                    self.wantToPickUp = 0;
                }
            });


            //Grab mask from ground
            c.GotoNext(MoveType.After,
                x => x.MatchLdcI4(0),
                x => x.MatchStloc(50)
            );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<Player>>((self) =>
            {
                bool hasFly = false;
                foreach(Creature.Grasp grasp in self.grasps)
                {
                    if (grasp?.grabbed is Fly)
                        hasFly = true;
                }
                if (!hasFly && self.wantToPickUp > 0 && self.pickUpCandidate is VultureMask && playerFaceMasks.TryGetValue(self, out FaceMask fMask) &&!fMask.HasAMask 
                && ((self.grasps[0] != null && self.Grabability(self.grasps[0].grabbed) >= Player.ObjectGrabability.TwoHands) ||
                (self.grasps[1] != null && self.Grabability(self.grasps[1].grabbed) >= Player.ObjectGrabability.TwoHands) || (self.grasps[0] != null && self.grasps[1] != null)))
                {
                    Debug.Log("Mask straight to face");
                    fMask.MaskToFace(self.pickUpCandidate as VultureMask);
                    self.wantToPickUp = 0;
                }
            });
        }

        public void ObjectEatenWithFaceMask(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible edible)
        {
            orig(self, edible);

            if(playerFaceMasks.TryGetValue(self, out FaceMask mask))
                mask.LockInteraction();
            
        }

        public void DropMaskOnStun(On.Player.orig_Stun orig, Player self, int st)
        {
            orig(self, st);
            if(playerFaceMasks.TryGetValue(self, out FaceMask mask))
            {
                if (mask.HasAMask && st > UnityEngine.Random.Range(40, 80))
                    mask.DropMask();
            }
        }

        public void DropFaceMaskOnDeath(On.Player.orig_Die orig, Player self)
        {
            orig(self);
            if (playerFaceMasks.TryGetValue(self, out FaceMask mask))
                mask.DropMask(true);
        }

        public void FaceMaskUpdate(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (playerFaceMasks.TryGetValue(self, out FaceMask fMask))
                fMask.Update(eu);
        }

        public void VultureMask_Update_Patch(On.VultureMask.orig_Update orig, VultureMask self, bool eu)
        {
            FaceMask.AbstractFaceMask abstractMask = null;
            foreach (var stick in self.abstractPhysicalObject.stuckObjects)
            {
                if (stick is FaceMask.AbstractFaceMask)
                {
                    abstractMask = stick as FaceMask.AbstractFaceMask;
                    break;
                }
            }

            if (!(abstractMask is null) &&
                faceMaskPlayers.TryGetValue(abstractMask.faceMask, out Player player))
            {
                self.donned = 1f;
                self.lastDonned = 1f;

                self.Grabbed(abstractMask.abstractGrasp);
                orig(self, eu);
                self.grabbedBy.Clear();
                self.viewFromSide = Custom.LerpAndTick(self.viewFromSide, (float)player.input[0].x, 0.11f, 0.093333335f);

            }
            else
                orig(self, eu);
        }

        public void FaceMaskDrawSprites(On.VultureMask.orig_DrawSprites orig, VultureMask self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            FaceMask.AbstractFaceMask abstractMask = null;
            foreach (var stick in self.abstractPhysicalObject.stuckObjects)
            {
                if (stick is FaceMask.AbstractFaceMask)
                {
                    abstractMask = stick as FaceMask.AbstractFaceMask;
                    break;
                }
            }

            if (!(abstractMask is null) &&
                faceMaskPlayers.TryGetValue(abstractMask.faceMask, out Player player))
            {
                self.donned = 1f;
                self.lastDonned = 1f;
                self.Grabbed(new Creature.Grasp(player, self, 0, 0, Creature.Grasp.Shareability.CanNotShare, 0f, false));
                orig(self, sLeaser, rCam, timeStacker, camPos);
                self.grabbedBy.Clear();

            }
            else
                orig(self, sLeaser, rCam, timeStacker, camPos);
        }

        public float ScavNoPickUpFaceMask(On.ScavengerAI.orig_PickUpItemScore orig, ScavengerAI self, ItemTracker.ItemRepresentation rep)
        {
            if(ModManager.MMF && MMF.cfgHunterBackspearProtect.Value && rep.representedItem.realizedObject is VultureMask mask)
            {
                foreach (var pair in playerFaceMasks)
                {
                    if (pair.Value.HasAMask && pair.Value.Mask == mask)
                        return 0f;
                }
            }
            return orig(self, rep);
        }


        public void DropFaceMaskOnViolence(On.Creature.orig_Violence orig, Creature self,
            BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
            if ((self is Player player) && playerFaceMasks.TryGetValue(player, out FaceMask mask))
                mask.DropMask();
        }

        public CreatureTemplate.Relationship LizardSeeFaceMask(On.LizardAI.orig_IUseARelationshipTracker_UpdateDynamicRelationship orig, LizardAI self, RelationshipTracker.DynamicRelationship dRelation)
        {

            if (dRelation.trackerRep.representedCreature?.realizedCreature is Player player && playerFaceMasks.TryGetValue(player, out FaceMask faceMask) && faceMask.HasAMask)
            {
                Creature.Grasp auxGrasp = player.grasps[1];
                player.grasps[1] = faceMask.abstractStick.abstractGrasp;

                CreatureTemplate.Relationship result = orig(self, dRelation);
                
                player.grasps[1] = auxGrasp;
                return result;
                
            }
            return orig(self, dRelation);
        }

        public void Grabbed(On.Player.orig_Grabbed orig, Player self, Creature.Grasp grasp)
        {
            orig(self, grasp);
            if (playerFaceMasks.TryGetValue(self, out FaceMask mask))
                mask.DropMask();
        }

        public void ClearMasks()
        {
            Debug.Log("Clearing all faceMasks");
            playerFaceMasks.Clear();
            faceMaskPlayers.Clear();
        }

    }
}