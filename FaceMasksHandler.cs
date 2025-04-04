using LancerRemix.Cat;
using RWCustom;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Permissions;
using System.Security;
using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using DropButton;
using ImprovedInput;
using MoreSlugcats;
using System.Runtime.CompilerServices;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ShinyShieldMask
{   
    public class FaceMasksHandler
    {
        public static bool hasLancerMod = false, hasDropButton = false, hasImprovedInput = false;
        public static ConditionalWeakTable<Player, FaceMask> PlayerFaceMasks;
        //public static ConditionalWeakTable<FaceMask, Player> FaceMaskPlayers; //Double dictionaries to find either value easily
        internal static object maskButton;

        private bool MaskHasPlayer(FaceMask mask, out Player player)
        {
            player = null;

            if(ShinyShieldMaskMod.rwGame.Target is RainWorldGame game && game.Players != null)
            {
                foreach(AbstractCreature playerAbs in game.Players)
                {
                    if(playerAbs != null && (playerAbs.realizedCreature is Player listedPlayer) && 
                        PlayerFaceMasks.TryGetValue(listedPlayer, out FaceMask listedMask) && mask == listedMask)
                    {
                        player = listedPlayer;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool MaskHasPlayer(VultureMask mask, out Player player)
        {
            player = null;
            if (ShinyShieldMaskMod.rwGame.Target is RainWorldGame game && game.Players != null)
            {
                foreach (AbstractCreature playerAbs in game.Players)
                {
                    if (playerAbs != null && playerAbs.realizedCreature is Player listedPlayer &&
                        PlayerFaceMasks.TryGetValue(listedPlayer, out FaceMask fMask) &&
                        fMask != null && fMask.Mask == mask)
                    {
                        player = listedPlayer;
                        return true;
                    }
                }
            }
            return false;
        }

        private bool MaskHasPlayer(FaceMask mask)
        {

            if (ShinyShieldMaskMod.rwGame.Target is RainWorldGame game && game.Players != null)
            {
                foreach (AbstractCreature playerAbs in game.Players)
                {
                    if (playerAbs != null && (playerAbs.realizedCreature is Player listedPlayer) &&
                        PlayerFaceMasks.TryGetValue(listedPlayer, out FaceMask listedMask) && mask == listedMask)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool MaskHasPlayer(VultureMask mask)
        {
            if (ShinyShieldMaskMod.rwGame.Target is RainWorldGame game && game.Players != null)
            {
                foreach (AbstractCreature playerAbs in game.Players)
                {
                    if (playerAbs != null && playerAbs.realizedCreature is Player listedPlayer &&
                        PlayerFaceMasks.TryGetValue(listedPlayer, out FaceMask fMask) &&
                        fMask != null && fMask.Mask == mask)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public FaceMasksHandler()
        {
            PlayerFaceMasks = new ConditionalWeakTable<Player, FaceMask>();
        }

        public void SetVariables(bool hasLancer = false, bool hasDrop = false, bool hasImprovedInputs = false)
        {
            hasLancerMod = hasLancer;
            hasDropButton = hasDrop;
            hasImprovedInput = hasImprovedInputs;
            if (hasImprovedInput)
                CreateImprovedInput();
        }

        private void CreateImprovedInput()
        {
            maskButton = PlayerKeybind.Register("ShinyShieldMask:MaskButton", "Shiny Shield Mask", "MaskButton", KeyCode.C, KeyCode.JoystickButton3);
        }

        private void HandleLancerConstructor(Player player)
        {
            var sub = ModifyCat.GetSub<CatSub.Cat.CatSupplement>(player);
            if (sub?.GetType().Name != "LunterSupplement")
            {
                FaceMask fMask = new FaceMask(player);
                PlayerFaceMasks.Add(player, fMask);
            }
        }
        
        public bool IsPlayerWearingMask(Player player, out VultureMask mask)
        {
            mask = null;
            if (PlayerFaceMasks.TryGetValue(player, out FaceMask fMask) && fMask.HasAMask)
                mask = fMask.Mask;
            
            return !(mask is null);
        }

        public void ReleaseFaceMaskFromPlayer(Player player, bool fling = false)
        {
            if (!PlayerFaceMasks.TryGetValue(player, out FaceMask fMask))
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
                else if (!PlayerFaceMasks.TryGetValue(self, out _))
                {
                    FaceMask fMask = new FaceMask(self);
                    PlayerFaceMasks.Add(self, fMask);
                }
            }
        }

        public void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            //Soft dependency of drop button: drop facemask if drop button pressed while holding nothing else
            if (hasDropButton && CheckIfDropPressed(self) 
                && PlayerFaceMasks.TryGetValue(self, out FaceMask fMask) && fMask.HasAMask)
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
                hasAnyGrasp = hasAnyGrasp || (!(self.spearOnBack is null) && self.spearOnBack.HasASpear) || (!(self.slugOnBack is null) && self.slugOnBack.HasASlug);

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
                if (PlayerFaceMasks.TryGetValue(self, out FaceMask fMask) && fMask.HasAMask &&
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
                if (!hasFly && self.wantToPickUp > 0 && self.pickUpCandidate is VultureMask && PlayerFaceMasks.TryGetValue(self, out FaceMask fMask) &&!fMask.HasAMask 
                && ((self.grasps[0] != null && self.Grabability(self.grasps[0].grabbed) >= Player.ObjectGrabability.TwoHands) ||
                (self.grasps[1] != null && self.Grabability(self.grasps[1].grabbed) >= Player.ObjectGrabability.TwoHands) || (self.grasps[0] != null && self.grasps[1] != null)))
                {
                    fMask.MaskToFace(self.pickUpCandidate as VultureMask);
                    self.wantToPickUp = 0;
                }
            });
        }

        public void ObjectEatenWithFaceMask(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible edible)
        {
            orig(self, edible);
            if(PlayerFaceMasks.TryGetValue(self, out FaceMask mask))
                mask.LockInteraction();
        }

        public void DropMaskOnStun(On.Player.orig_Stun orig, Player self, int st)
        {
            orig(self, st);
            if(PlayerFaceMasks.TryGetValue(self, out FaceMask mask) && mask.HasAMask)
            {
                if (st > UnityEngine.Random.Range(40, 80) && (!ModManager.MMF || !MMF.cfgJetfishItemProtection.Value ||
                    self.stunDamageType != Creature.DamageType.Blunt))
                    mask.DropMask();
            }
        }

        public void DropFaceMaskOnDeath(On.Player.orig_Die orig, Player self)
        {
            orig(self);
            if (PlayerFaceMasks.TryGetValue(self, out FaceMask mask))
                mask.DropMask(true);
        }

        public void FaceMaskUpdate(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (PlayerFaceMasks.TryGetValue(self, out FaceMask fMask))
                fMask.Update(eu);
        }

        public float PlayerNoLookAtFaceMask(On.PlayerGraphics.PlayerObjectLooker.orig_HowInterestingIsThisObject orig, PlayerGraphics.PlayerObjectLooker self, PhysicalObject obj)
        {
            if (obj is VultureMask vmask && PlayerFaceMasks.TryGetValue(self.owner.player, out FaceMask fMask))
            {
                if (fMask.Mask == vmask)
                    return 0f;
            }
            return orig(self, obj);
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
                MaskHasPlayer(abstractMask.faceMask, out Player player))
            {
                self.donned = 1f;
                self.lastDonned = 1f;
                self.Grabbed(abstractMask.abstractGrasp);
                orig(self, eu);
                self.grabbedBy.Clear();
                if(player.bodyMode == Player.BodyModeIndex.Crawl)
                {
                    self.viewFromSide = Custom.LerpAndTick(self.viewFromSide, Mathf.Sign(player.bodyChunks[0].pos.x - player.bodyChunks[1].pos.x), 0.11f, 0.143333335f);
                }
                else if(player.animation != Player.AnimationIndex.ClimbOnBeam && player.bodyMode != Player.BodyModeIndex.CorridorClimb)
                    self.viewFromSide = Custom.LerpAndTick(self.viewFromSide, (float)player.input[0].x, 0.11f, 0.143333335f);
                if(self.AbstrMsk.scavKing && player.standing)
                {
                    if(self.viewFromSide < 0.1f && self.viewFromSide > -0.1f)
                    {
                        self.viewFromSide = Mathf.Sign(self.viewFromSide) * 0.02f;
                    }
                }
                
            }
            else
                orig(self, eu);

        }
        
        public void Player_GraphicsModuleUpdatedPatch(On.Player.orig_GraphicsModuleUpdated orig, Player self, bool actuallyViewed, bool eu)
        {
            orig(self, actuallyViewed, eu);
            if(PlayerFaceMasks.TryGetValue(self, out FaceMask fMask))
            {
                if (fMask.HasAMask && actuallyViewed)
                {
                    fMask.Mask.bodyChunks[0].MoveFromOutsideMyUpdate(eu,
                        (self.graphicsModule as PlayerGraphics).hands[fMask.abstractStick.abstractGrasp.graspUsed].pos);
                }
            }
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
                MaskHasPlayer(abstractMask.faceMask, out Player player))
            {
                self.donned = 1f;
                self.lastDonned = 1f;
                self.Grabbed(abstractMask.abstractGrasp);
                orig(self, sLeaser, rCam, timeStacker, camPos);

                self.grabbedBy.Clear();
            }
            else
                orig(self, sLeaser, rCam, timeStacker, camPos);

        }

        public int ScavNoPickUpFaceMaskWeapon(On.ScavengerAI.orig_WeaponScore orig, ScavengerAI self, PhysicalObject obj, bool pickupDropInsteadOfWeaponSelection, bool reallyWantsSpear = false)
        {
            if(obj is VultureMask mask)
            {
                if (MaskHasPlayer(mask))
                    return 0;

            }
            return orig(self, obj, pickupDropInsteadOfWeaponSelection, reallyWantsSpear);
        }

        public int ScavNoPickUpFaceMaskCollect(On.ScavengerAI.orig_CollectScore_PhysicalObject_bool orig, ScavengerAI self, PhysicalObject obj, bool weaponFiltered)
        {
            if (obj is VultureMask mask)
            {
                if (MaskHasPlayer(mask))
                    return 0;
            }
            return orig(self, obj, weaponFiltered);
        }

        public void DropFaceMaskOnViolence(On.Creature.orig_Violence orig, Creature self,
            BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
            if (!(source is null) && source.owner is Creature creature && creature.abstractCreature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Leech)
                return;
            if ((self is Player player) && PlayerFaceMasks.TryGetValue(player, out FaceMask mask) && mask.HasAMask)
            {
                if (source != null && source.owner is JetFish && ModManager.MMF && MMF.cfgJetfishItemProtection.Value)
                    return;
                VultureMask vMask = mask.Mask;
                if (vMask.maskGfx.overrideSprite != "" && (vMask.maskGfx.overrideSprite == "KrakenMask" ||
                        vMask.maskGfx.overrideSprite == "SpikeMask" || vMask.maskGfx.overrideSprite == "HornedMask" ||
                        vMask.maskGfx.overrideSprite == "SadMask") && ShinyShieldMaskOptions.eliteScavMaskStun.Value > 0)
                    mask.DropMask();
                else if (vMask.King && ShinyShieldMaskOptions.vultureKingMaskStun.Value > 0)
                    mask.DropMask();
                else if(vMask.maskGfx.ScavKing && ShinyShieldMaskOptions.scavKingMaskStun.Value > 0)
                    mask.DropMask();
                else if(ShinyShieldMaskOptions.vultureMaskStun.Value > 0)
                    mask.DropMask();
            }
        }

        public CreatureTemplate.Relationship LizardSeeFaceMask(On.LizardAI.orig_IUseARelationshipTracker_UpdateDynamicRelationship orig, LizardAI self, RelationshipTracker.DynamicRelationship dRelation)
        {

            if (dRelation.trackerRep.representedCreature?.realizedCreature is Player player && PlayerFaceMasks.TryGetValue(player, out FaceMask faceMask) && faceMask.HasAMask)
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
            if(grasp.grabber.abstractCreature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Leech ||
                grasp.grabber.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.TentaclePlant)
                return;
            if (PlayerFaceMasks.TryGetValue(self, out FaceMask mask))
                mask.DropMask();
        }
    }
}
