using System;
using System.Security.Permissions;
using System.Security;
using BepInEx;
using RWCustom;
using MoreSlugcats;
using UnityEngine;
using Mono.Cecil.Cil;
using MonoMod.Cil;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ShinyShieldMask
{
    [BepInPlugin("ShinyKelp.ShinyShieldMask", "Shiny Shield Mask", "1.1.0")]
    public class ShinyShieldMaskMod : BaseUnityPlugin
    {

        private int count;
        private void OnEnable()
        {
            On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
            count = 0;
        }

        private bool IsInit;

        private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                if (IsInit) return;

                //Your hooks go here
                On.Spear.HitSomething += this.Spear_HitSomething;
                IL.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += LizardAI_IUseARelationshipTracker_UpdateDynamicRelationship;
                //On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += UsedToVultureMaskCheck;
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

        private CreatureTemplate.Relationship UsedToVultureMaskCheck(On.LizardAI.orig_IUseARelationshipTracker_UpdateDynamicRelationship orig, LizardAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            if (self.usedToVultureMask < ShinyShieldMaskOptions.vultureMaskFearDuration.Value * 40)
            {
                Debug.Log("CURRENT VULTURE MASK VALUE: " + self.usedToVultureMask);
                Debug.Log("COUNT: " + count);
                count++;
            }
            else count = 0;


            return orig(self, dRelation);
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

            //Mask fear duration randomization (change usedToVultureMask++)
            c.GotoNext(MoveType.After,
                x => x.MatchAdd()
            );
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<RelationshipTracker.DynamicRelationship, int>>((dRelation) => {
                if (ShinyShieldMaskOptions.randomFearDuration.Value)
                {
                    int a;
                    if ((dRelation.state as LizardAI.LizardTrackState).vultureMask == 1)
                    {
                        a = UnityEngine.Random.Range(-ShinyShieldMaskOptions.vultureMaskFearDuration.Value*2, ShinyShieldMaskOptions.vultureMaskFearDuration.Value*2 + 1);
                        a = (int)(a * 0.75f);
                    }
                    else
                    {
                        a = UnityEngine.Random.Range(-ShinyShieldMaskOptions.kingVultureMaskFearDuration.Value*2, ShinyShieldMaskOptions.kingVultureMaskFearDuration.Value*2 + 1);
                        a = (int)(a * 0.75f);
                    }
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

            //c.Emit(OpCodes.Ldc_I4, options.vultureMaskFearDuration.Value * 40);
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
            Debug.Log("After 600f...");

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

                else if (IsWearingMask(player, out float stunBonus, out int graspIndex))
                {
                    VultureMask mask = player.grasps[graspIndex].grabbed as VultureMask;
                    Vector2 knockback = self.firstChunk.vel * .12f / player.firstChunk.mass;
                    if (mask.King)
                        knockback *= .75f;
                    else if (mask.AbstrMsk.scavKing)
                        knockback *= .4f;
                    player.firstChunk.vel += knockback;

                    player.Stun((int)(10f * stunBonus));
                    if(stunBonus > 0f && stunBonus < 1.6f)
                        player.ReleaseGrasp(graspIndex);
                    else if(stunBonus >= 1.6f)
                        player.LoseAllGrasps();
                    self.room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, self.firstChunk);
                    HitEffect(self.firstChunk.vel, result.collisionPoint, self.room);
                    self.vibrate = 20;
                    self.ChangeMode(Weapon.Mode.Free);
                    self.firstChunk.vel = self.firstChunk.vel * -0.5f + Custom.DegToVec(UnityEngine.Random.value * 360f) * Mathf.Lerp(0.1f, 0.4f, UnityEngine.Random.value) * self.firstChunk.vel.magnitude;
                    self.SetRandomSpin();
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
                        Vector2 b = self.firstChunk.vel * self.firstChunk.mass / scavenger.bodyChunks[2].mass;
                        scavenger.bodyChunks[2].vel += b;

                        if(!scavenger.King) 
                            scavenger.Violence(self.firstChunk, self.firstChunk.vel, scavenger.bodyChunks[2], result.onAppendagePos, Creature.DamageType.Blunt, 0.02f, 5f);
                        self.room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, self.firstChunk);
                        HitEffect(self.firstChunk.vel, result.collisionPoint, self.room);
                        self.vibrate = 20;
                        self.ChangeMode(Weapon.Mode.Free);
                        self.firstChunk.vel = self.firstChunk.vel * -0.5f + Custom.DegToVec(UnityEngine.Random.value * 360f) * Mathf.Lerp(0.1f, 0.4f, UnityEngine.Random.value) * self.firstChunk.vel.magnitude;
                        self.SetRandomSpin();
                        return false;

                    }
                    else return orig(self, result, eu);
                    
                }
                else return orig(self, result, eu);
            }
            else
                return orig(self, result, eu);
        }

        public bool IsWearingMask(Player player, out float stunBonus, out int graspIndex)
        {
            
            bool isWearingMask = false;
            stunBonus = ShinyShieldMaskOptions.vultureMaskStun.Value;
            graspIndex = 0;

            for (int i = 0; i < player.grasps.Length; i++)
            {
                if (!(player.grasps[i] is null) && player.grasps[i].grabbed is VultureMask vMask && vMask.donned > .75f)
                {
                    isWearingMask = true;
                    graspIndex = i;
                    if (vMask.King)
                    {
                        stunBonus = ShinyShieldMaskOptions.vultureKingMaskStun.Value;
                    }
                    else if (vMask.AbstrMsk.scavKing)
                        stunBonus = ShinyShieldMaskOptions.scavKingMaskStun.Value;
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



    }
}