using System;
using System.Security.Permissions;
using System.Security;
using BepInEx;
using RWCustom;
using MoreSlugcats;
using UnityEngine;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Reflection;
using LancerRemix.Cat;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ShinyShieldMask
{
    [BepInPlugin("ShinyKelp.ShinyShieldMask", "Shiny Shield Mask", "1.2.2")]
    public class ShinyShieldMaskMod : BaseUnityPlugin
    {

        private bool hasLancerMod, hasDropButton;
        private FaceMasksHandler faceMasksHandler;
        private void OnEnable()
        {
            On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
        }

        private bool IsInit;

        private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                if (IsInit) return;
                hasLancerMod = false;
                foreach(ModManager.Mod mod in ModManager.ActiveMods)
                {
                    if (mod.id == "topicular.lancer")
                    {
                        hasLancerMod = true;
                        Debug.Log("Lancer mod detected.");
                    }
                    if(mod.id == "drop-button")
                    {
                        hasDropButton = true;
                        Debug.Log("Drop button detected.");
                    }
                }
                
                //Your hooks go here
                On.Spear.HitSomething += this.Spear_HitSomething;
                IL.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += LizardAI_IUseARelationshipTracker_UpdateDynamicRelationship;
                On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += LizardAI_IUseARelationshipTracker_UpdateDynamicRelationship1;
                On.RainWorldGame.ShutDownProcess += RainWorldGameOnShutDownProcess;
                On.GameSession.ctor += GameSessionOnctor;
                if (faceMasksHandler is null)
                    faceMasksHandler = new FaceMasksHandler();
                else faceMasksHandler.ClearMasks();
                SetFaceMaskHooks();
                MachineConnector.SetRegisteredOI("ShinyKelp.ShinyShieldMask", ShinyShieldMaskOptions.instance);
                Debug.Log("Finished applying hooks for Shiny Shield Mask!");
                IsInit = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }


        public void SetFaceMaskHooks()
        {
            On.Player.Update += faceMasksHandler.FaceMaskUpdate;
            On.Player.ObjectEaten += faceMasksHandler.ObjectEatenWithFaceMask;
            On.Player.Die += faceMasksHandler.DropFaceMaskOnDeath;
            On.Player.Stun += faceMasksHandler.DropMaskOnStun;
            On.Player.Destroy += faceMasksHandler.Player_Destroy;
            On.Creature.Violence += faceMasksHandler.DropFaceMaskOnViolence;
            On.ScavengerAI.WeaponScore += faceMasksHandler.ScavNoPickUpFaceMaskWeapon;
            On.ScavengerAI.CollectScore_PhysicalObject_bool += faceMasksHandler.ScavNoPickUpFaceMaskCollect;
            On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += faceMasksHandler.LizardSeeFaceMask;
            //On.Player.Grabbed += faceMasksHandler.Grabbed;
            On.Player.GrabUpdate += faceMasksHandler.Player_GrabUpdate;
            IL.Player.GrabUpdate += faceMasksHandler.Player_GrabUpdate;
            On.Player.ctor += faceMasksHandler.Player_ctor;

            On.VultureMask.DrawSprites += faceMasksHandler.FaceMaskDrawSprites;
            On.VultureMask.Update += faceMasksHandler.VultureMask_Update_Patch;
            On.PlayerGraphics.PlayerObjectLooker.HowInterestingIsThisObject += faceMasksHandler.PlayerNoLookAtFaceMask;

            faceMasksHandler.SetVariables(hasLancerMod, hasDropButton);
          
        }

        

        private void RainWorldGameOnShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
        {
            faceMasksHandler.ClearMasks();
            orig(self);
        }

        private void GameSessionOnctor(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
        {
            faceMasksHandler.ClearMasks();
            orig(self, game);
        }

        private CreatureTemplate.Relationship LizardAI_IUseARelationshipTracker_UpdateDynamicRelationship1(On.LizardAI.orig_IUseARelationshipTracker_UpdateDynamicRelationship orig, LizardAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            if ((ShinyShieldMaskOptions.eliteScavFearDuration.Value == 0) ||
                dRelation is null || dRelation.trackerRep is null || dRelation.trackerRep.representedCreature is null ||
                dRelation.trackerRep.representedCreature.realizedCreature is null ||
                dRelation.trackerRep.representedCreature.creatureTemplate.type != MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite)
                return orig(self, dRelation);

            if (self.usedToVultureMask < ShinyShieldMaskOptions.eliteScavFearDuration.Value*40 &&
                self.creature.creatureTemplate.type != CreatureTemplate.Type.RedLizard &&
                self.creature.creatureTemplate.type != CreatureTemplate.Type.BlackLizard &&
                !dRelation.trackerRep.representedCreature.realizedCreature.dead)
            {
                if (self.usedToVultureMask == -1)
                    self.usedToVultureMask = 1;
                else if (ShinyShieldMaskOptions.randomFearDuration.Value && self.usedToVultureMask == 0)
                {
                    int a = UnityEngine.Random.Range(0, ShinyShieldMaskOptions.eliteScavFearDuration.Value * 20 + 1);
                    if (UnityEngine.Random.value < .5f)
                        a = -a;
                    self.usedToVultureMask = a;
                }
                else
                    self.usedToVultureMask++;

                bool itHasPrey = false;
                foreach (ThreatTracker.ThreatCreature threat in self.threatTracker.threatCreatures)
                {
                    if (threat.creature == dRelation.trackerRep)
                    {
                        itHasPrey = true;
                    }
                }
                if(!itHasPrey)
                    self.threatTracker.AddThreatCreature(dRelation.trackerRep);

                dRelation.currentRelationship.type = CreatureTemplate.Relationship.Type.Afraid;
                dRelation.currentRelationship.intensity = 0.8f;
            }
            else if (dRelation.currentRelationship.type == CreatureTemplate.Relationship.Type.Afraid &&
                StaticWorld.creatureTemplates[self.creature.creatureTemplate.index].relationships
                [MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite.Index].type != CreatureTemplate.Relationship.Type.Afraid)
            {
                dRelation.currentRelationship.type = StaticWorld.creatureTemplates[self.creature.creatureTemplate.index].relationships
                    [MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite.Index].type;
                dRelation.currentRelationship.intensity = StaticWorld.creatureTemplates[self.creature.creatureTemplate.index].relationships
                    [MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite.Index].intensity;
                
                bool itHasPrey = false;
                foreach (ThreatTracker.ThreatCreature threat in self.threatTracker.threatCreatures)
                {
                    if (threat.creature == dRelation.trackerRep)
                    {
                        itHasPrey = true;
                    }
                }
                if (itHasPrey)
                    self.threatTracker.RemoveThreatCreature(dRelation.trackerRep.representedCreature);
            }
            else
                return orig(self, dRelation);
            return dRelation.currentRelationship;
        }

        private void LizardAI_IUseARelationshipTracker_UpdateDynamicRelationship(ILContext il)
        {
            
            ILCursor c = new ILCursor(il);

            //Change values for mask duration.
            c.GotoNext(MoveType.After,
                x => x.MatchLdcI4(700)
            );

            c.Emit(OpCodes.Pop);

            c.EmitDelegate<Func<int>>(() =>
            {
                return ShinyShieldMaskOptions.vultureMaskFearDuration.Value * 40;
            });


            c.GotoNext(MoveType.After,
                x => x.MatchLdcI4(1200)
            );

            c.Emit(OpCodes.Pop);

            c.EmitDelegate<Func<int>>(() => {
                return ShinyShieldMaskOptions.kingVultureMaskFearDuration.Value * 40;
            }
            );

            //Mask fear duration randomization.
            //We do this by setting usedToVultureMask to a random value the first frame that the lizard sees the mask.
            c.GotoNext(MoveType.After,
                x => x.MatchAdd()
            );

            c.Emit(OpCodes.Ldarg_1);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<RelationshipTracker.DynamicRelationship, LizardAI, int>>((dRelation, thisAI) => {
                if (ShinyShieldMaskOptions.randomFearDuration.Value)
                {
                    if (thisAI.usedToVultureMask == -1)
                        return 1;
                    if (thisAI.usedToVultureMask != 0)
                        return 0;
                    int a;
                    if ((dRelation.state as LizardAI.LizardTrackState).vultureMask == 1)
                    {
                        a = UnityEngine.Random.Range(0, ShinyShieldMaskOptions.vultureMaskFearDuration.Value*20 + 1);
                        if (UnityEngine.Random.value < .5f)
                            a = -a;
                    }
                    else
                    {
                        a = UnityEngine.Random.Range(0, ShinyShieldMaskOptions.kingVultureMaskFearDuration.Value * 20 + 1);
                        if (UnityEngine.Random.value < .5f)
                            a = -a;
                    }
                    if (a == -1)
                        a = 0;
                    return a;
                }
                else return 0;
            }
            );
            c.Emit(OpCodes.Add_Ovf);

            //Next two values are for the intensity of the fear throughout the duration,
            //defining start and end of the inverselerp function.
            c.GotoNext(MoveType.After,
                x => x.MatchLdcI4(700)
            );

            c.Emit(OpCodes.Pop);

            c.EmitDelegate<Func<int>>(() =>
            {
                return ShinyShieldMaskOptions.vultureMaskFearDuration.Value * 40;
            });


            c.GotoNext(MoveType.After,
                x => x.MatchLdcI4(1200)
            );

            c.Emit(OpCodes.Pop);

            c.EmitDelegate<Func<int>>(() => {
                return ShinyShieldMaskOptions.kingVultureMaskFearDuration.Value * 40;
            }
            );

            //Value to calculate in the inverselerp function.
            c = new ILCursor(il);
            c.GotoNext(MoveType.After,
                x => x.MatchConvR4(),
                x => x.MatchLdcR4(600f)
                );
            c.Emit(OpCodes.Pop);

            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<RelationshipTracker.DynamicRelationship, float>>((dRelation) =>
            {
                if ((dRelation.state as LizardAI.LizardTrackState).vultureMask == 1)
                {
                    return (ShinyShieldMaskOptions.vultureMaskFearDuration.Value * 40) * 0.7f;
                }
                else
                    return (ShinyShieldMaskOptions.kingVultureMaskFearDuration.Value * 40) * 0.7f;
            }
            );
        }

        private bool Spear_HitSomething(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
        {
            if (ShinyShieldMaskOptions.enableShieldMask.Value && result.obj is Player player && !(player is null))
            {
                if (result.chunk != player.firstChunk)
                    return orig(self, result, eu);

                bool hasLunterMask = false, hasFaceMask = false;

                if (IsWearingMask(player, out int graspIndex, out VultureMask mask) || 
                    (ShinyShieldMaskOptions.wearableMask.Value && (hasFaceMask = faceMasksHandler.IsPlayerWearingMask(player, out mask))) ||
                    (hasLancerMod && (hasLunterMask = LunterMaskCheck(player, out mask))) )
                {

                    Vector2 knockback = self.firstChunk.vel * .12f / player.firstChunk.mass;
                    Vector2 maskKnockback = Custom.RNV();
                    if (maskKnockback.y < 0f)
                        maskKnockback.y = -maskKnockback.y;
                    maskKnockback *= (9f * UnityEngine.Random.value + 6f);
                    float stunBonus = ShinyShieldMaskOptions.vultureMaskStun.Value;
                    
                    if(mask.maskGfx.overrideSprite != "" && (mask.maskGfx.overrideSprite == "KrakenMask" ||
                        mask.maskGfx.overrideSprite == "SpikeMask" || mask.maskGfx.overrideSprite == "HornedMask" ||
                        mask.maskGfx.overrideSprite == "SadMask"))
                    {
                        knockback *= .75f;
                        maskKnockback *= .65f;
                        stunBonus = ShinyShieldMaskOptions.eliteScavMaskStun.Value;
                    }
                    else if (mask.AbstrMsk.scavKing)
                    {
                        knockback *= .4f;
                        maskKnockback *= .4f;
                        stunBonus = ShinyShieldMaskOptions.scavKingMaskStun.Value;
                    }
                    else if(mask.King)
                    {
                        knockback *= .75f;
                        maskKnockback *= .65f;
                        stunBonus = ShinyShieldMaskOptions.vultureKingMaskStun.Value;
                    }

                    player.firstChunk.vel += knockback;

                    player.Stun((int)(10f * stunBonus));
                    if(stunBonus > 0f && stunBonus < 1.6f)
                    {
                        if(graspIndex >= 0)
                        {
                            player.ReleaseGrasp(graspIndex);
                        }
                        else if (hasFaceMask)
                        {
                            faceMasksHandler.ReleaseFaceMaskFromPlayer(player, false);
                        }
                        else if(hasLunterMask)
                        {
                            ReleaseLunterMask(player);
                        }
                        mask.firstChunk.vel = player.mainBodyChunk.vel + maskKnockback;
                    }
                    else if(stunBonus >= 1.6f)
                    {
                        player.LoseAllGrasps();
                        if (hasFaceMask)
                            faceMasksHandler.ReleaseFaceMaskFromPlayer(player, false);
                        else if (hasLunterMask)
                            ReleaseLunterMask(player);

                        mask.firstChunk.vel = player.mainBodyChunk.vel + maskKnockback;
                    }

                    HitEffect(self.firstChunk.vel, result.collisionPoint, self.room);
                    SetSpearBouncing(self);
                    return false;
                }
                else
                    return orig(self, result, eu);
            }
            else if(result.obj is Scavenger scavenger && !(scavenger is null) && (scavenger.Elite || scavenger.King))
            {
                if (result.chunk == scavenger.bodyChunks[2] && !scavenger.State.dead)
                {
                    bool frontalHit = Vector2.Dot(self.firstChunk.vel.normalized, scavenger.HeadLookDir) >= -(scavenger.King? ShinyShieldMaskOptions.eliteResistance.Value*1.5f : ShinyShieldMaskOptions.eliteResistance.Value) ;

                    if (frontalHit)
                    {
                        Vector2 knockback = self.firstChunk.vel * self.firstChunk.mass / scavenger.bodyChunks[2].mass;
                        scavenger.bodyChunks[2].vel += knockback;

                        if(!scavenger.King) 
                            scavenger.Violence(self.firstChunk, self.firstChunk.vel, scavenger.bodyChunks[2], result.onAppendagePos, Creature.DamageType.Blunt, 0.04f, 5f);
                        
                        HitEffect(self.firstChunk.vel, result.collisionPoint, self.room);
                        SetSpearBouncing(self);
                        return false;
                    }        
                }
            }
            return orig(self, result, eu);
        }

        private void SetSpearBouncing(Spear spear)
        {
            spear.room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, spear.firstChunk);
            spear.vibrate = 20;
            spear.ChangeMode(Weapon.Mode.Free);
            spear.firstChunk.vel = spear.firstChunk.vel * -0.5f + Custom.DegToVec(UnityEngine.Random.value * 360f) * Mathf.Lerp(0.1f, 0.4f, UnityEngine.Random.value) * spear.firstChunk.vel.magnitude;
            spear.SetRandomSpin();
        }

        public bool IsWearingMask(Player player, out int graspIndex, out VultureMask mask)
        {
            
            bool isWearingMask = false;
            graspIndex = -1;
            mask = null;
            for (int i = 0; i < player.grasps.Length; i++)
            {
                if (!(player.grasps[i] is null) && player.grasps[i].grabbed is VultureMask vMask && 
                    (vMask.donned > .75f))
                {
                    isWearingMask = true;
                    mask = vMask;
                    graspIndex = i;
                    break;
                    
                }
            }
            return isWearingMask;
        }

        public void HitEffect(Vector2 impactVelocity, Vector2 basePos, Room room)
        {
            int num = UnityEngine.Random.Range(3, 8);
            for (int i = 0; i < num; i++)
            {
                Vector2 pos = basePos + Custom.DegToVec(UnityEngine.Random.value * 360f) * 5f * UnityEngine.Random.value;
                Vector2 vel = -impactVelocity * -0.1f + Custom.DegToVec(UnityEngine.Random.value * 360f) * Mathf.Lerp(0.2f, 0.4f, UnityEngine.Random.value) * impactVelocity.magnitude;
                room.AddObject(new Spark(pos, vel, new Color(1f, 1f, 1f), null, 10, 170));
            }
            room.AddObject(new StationaryEffect(basePos, new Color(1f, 1f, 1f), null, StationaryEffect.EffectType.FlashingOrb));
        }

        private bool LunterMaskCheck(Player player, out VultureMask mask)
        {
            mask = null;
            var sub = ModifyCat.GetSub<CatSub.Cat.CatSupplement>(player);
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            if (sub is null || sub.GetType().Name != "LunterSupplement") 
                return false;
            var maskOnHorn = sub.GetType().GetField("maskOnHorn", flags).GetValue(sub);
            bool HasAMask = (bool)maskOnHorn.GetType().GetProperty("HasAMask", flags).GetGetMethod().Invoke(maskOnHorn, null);
            if (HasAMask)
                mask = (VultureMask)maskOnHorn.GetType().GetProperty("Mask", flags).GetGetMethod().Invoke(maskOnHorn, null);
            
            return HasAMask;
        }

        private void ReleaseLunterMask(Player player)
        {
            var sub = ModifyCat.GetSub<CatSub.Cat.CatSupplement>(player);
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            if (sub.GetType().Name != "LunterSupplement")
            {
                return;
            }
            var maskOnHorn = sub.GetType().GetField("maskOnHorn", flags).GetValue(sub);
            maskOnHorn.GetType().GetMethod("DropMask", flags).Invoke(maskOnHorn, new System.Object[] { false });
        }
        
    }
}