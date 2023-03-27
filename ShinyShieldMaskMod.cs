using System;
using System.Security.Permissions;
using System.Security;
using BepInEx;
using RWCustom;
using MoreSlugcats;
using UnityEngine;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ShinyShieldMask
{
    [BepInPlugin("ShinyKelp.ShinyShieldMask", "Shiny Shield Mask", "1.0.0")]
    public class ShinyShieldMaskMod : BaseUnityPlugin
    {
        private void OnEnable()
        {
            On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
        }

        public ShinyShieldMaskMod()
        {
            try
            {
                options = new ShinyShieldMaskOptions(this, Logger);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }

        }
        private ShinyShieldMaskOptions options;

        private bool IsInit;

        private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                if (IsInit) return;

                //Your hooks go here
                Debug.Log("Init ShinyShieldMask");
                On.Spear.HitSomething += this.Spear_HitSomething;
                MachineConnector.SetRegisteredOI("ShinyKelp.ShinyShieldMask", this.options);
                Debug.Log("Finished applying hooks!");

                IsInit = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }


        private bool Spear_HitSomething(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
        {
            if (options.enableShieldMask.Value && result.obj is Player player && !(player is null))
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
                    if(stunBonus > 0f && stunBonus < 1.8f)
                        player.ReleaseGrasp(graspIndex);
                    else if(stunBonus >= 1.8f)
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
                    bool frontalHit = Vector2.Dot(self.firstChunk.vel.normalized, scavenger.HeadLookDir) >= 0f - options.eliteResistance.Value;

                    if (frontalHit)
                    {
                        Vector2 b = self.firstChunk.vel * self.firstChunk.mass / scavenger.bodyChunks[2].mass;
                        scavenger.bodyChunks[2].vel += b;

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
            stunBonus = options.vultureMaskStun.Value;
            graspIndex = 0;

            for (int i = 0; i < player.grasps.Length; i++)
            {
                if (!(player.grasps[i] is null) && player.grasps[i].grabbed is VultureMask vMask && vMask.donned > .75f)
                {
                    isWearingMask = true;
                    graspIndex = i;
                    if (vMask.King)
                    {
                        stunBonus = options.vultureKingMaskStun.Value;
                    }
                    else if (vMask.AbstrMsk.scavKing)
                        stunBonus = options.scavKingMaskStun.Value;
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