using BepInEx;
using LancerRemix.Cat;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using System.IO;
using static CatSub.Story.StoryRegistry.TimelinePointer;
using System.ComponentModel;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ShinyShieldMask
{
    [BepInPlugin("ShinyKelp.ShinyShieldMask", "Shiny Shield Mask", "1.5")]
    public class ShinyShieldMaskMod : BaseUnityPlugin
    {

        private bool hasLancerMod, hasDropButton, hasImprovedInput;
        private FaceMasksHandler faceMasksHandler;
        private const int TEMPLAR_MASK_ID = -54;
        private const int TEMPLAR_IMMUNITY_VALUE = 160;
        private void OnEnable()
        {
            On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
        }

        private bool IsInit;


        //We allow elite scavs to equip other masks. We do not allow templars to equip another mask if they lose their original.
        public static Dictionary<EntityID, KeyValuePair<bool, VultureMask.AbstractVultureMask>> EliteMasks;

        private Dictionary<EntityID, bool> MaskedTemplars = new Dictionary<EntityID, bool>();

        internal static WeakReference rwGame = new WeakReference(null);
        private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                if (IsInit) return;
                if (EliteMasks is null)
                    EliteMasks = new Dictionary<EntityID, KeyValuePair<bool, VultureMask.AbstractVultureMask>>();
                hasLancerMod = false; hasDropButton = false;
                foreach (ModManager.Mod mod in ModManager.ActiveMods)
                {
                    if (mod.id == "topicular.lancer")
                    {
                        hasLancerMod = true;
                    }
                    if (mod.id == "drop-button")
                    {
                        hasDropButton = true;
                    }
                    if (mod.id == "improved-input-config")
                    {
                        hasImprovedInput = true;
                    }
                }

                if (faceMasksHandler is null)
                    faceMasksHandler = new FaceMasksHandler();

                //Shield functionality
                On.Spear.HitSomething += Spear_HitSomething;

                //Mask fear factor (both Player and elites)
                IL.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += LizardAI_IUseARelationshipTracker_UpdateDynamicRelationship;
                On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += LizardAI_IUseARelationshipTracker_UpdateDynamicRelationship1;
                On.StaticWorld.InitStaticWorld += StaticWorld_InitStaticWorld;
                On.Lizard.Violence += GetUsedToVultureMask;

                //Elite scavenger masks stuff here
                On.Scavenger.ctor += CtorCheckMaskDictionary;
                On.ScavengerGraphics.ctor += GFXCtorCheckMaskDictionary;
                On.AbstractWorldEntity.Destroy += CheckDictionaryDeletion;
                On.ScavengerAbstractAI.ReGearInDen += CheckReplenishMask;
                On.Scavenger.PickUpAndPlaceInInventory += CheckIfNeedsEquipMask;
                IL.Scavenger.Violence += DropCustomMaskOnDeath;
                On.MoreSlugcats.VultureMaskGraphics.ctor_PhysicalObject_AbstractVultureMask_int += CheckIfMaskIsTemplar;
                On.MoreSlugcats.VultureMaskGraphics.DrawSprites += KeepGlowInTemplarMask;
                On.ScavengerAI.LikeOfPlayer += ScavengerAI_LikeOfPlayer;

                On.RainWorldGame.ShutDownProcess += RainWorldGameOnShutDownProcess;
                On.GameSession.ctor += GameSessionOnctor;
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

        private void KeepGlowInTemplarMask(On.MoreSlugcats.VultureMaskGraphics.orig_DrawSprites orig, VultureMaskGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (self.attachedTo is VultureMask mask && mask.AbstrMsk.colorSeed == TEMPLAR_MASK_ID)
            {
                sLeaser.sprites[self.firstSprite].color = Color.Lerp(sLeaser.sprites[self.firstSprite].color, RainWorld.GoldRGB, 0.5f);
            }
        }

        private void CheckIfMaskIsTemplar(On.MoreSlugcats.VultureMaskGraphics.orig_ctor_PhysicalObject_AbstractVultureMask_int orig, VultureMaskGraphics self, PhysicalObject attached, VultureMask.AbstractVultureMask abstractMask, int firstSprite)
        {
            orig(self, attached, abstractMask, firstSprite);
            if (abstractMask != null && abstractMask.colorSeed == TEMPLAR_MASK_ID)
            {
                self.maskType = VultureMask.MaskType.SCAVTEMPLAR;
                self.glimmer = true;
                self.ignoreDarkness = true;
            }
        }
        private float ScavengerAI_LikeOfPlayer(On.ScavengerAI.orig_LikeOfPlayer orig, ScavengerAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            if (dRelation != null && dRelation.trackerRep != null && dRelation.trackerRep.representedCreature != null && dRelation.trackerRep.representedCreature.realizedCreature != null && dRelation.trackerRep.representedCreature.realizedCreature is Player player)
            {
                if (player.scavengerImmunity > 0 && player.scavengerImmunity < 2400)
                {
                    int scavImmunity = player.scavengerImmunity;
                    player.scavengerImmunity = 0;
                    float origLikeOfPlayer = orig(self, dRelation);
                    float immunityIncrease = Mathf.Min(0.5f * (float)scavImmunity / (float)TEMPLAR_IMMUNITY_VALUE, 0.45f);
                    player.scavengerImmunity = scavImmunity;
                    return Mathf.Min(origLikeOfPlayer + immunityIncrease, 1f);
                }
                else return orig(self, dRelation);
            }
            else return orig(self, dRelation);
        }


        private void GetUsedToVultureMask(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
        {
            orig(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
            if (!ShinyShieldMaskOptions.attacksFearDuration.Value)
                return;
            if(source != null && source.owner is Weapon weapon)
            {
                if (weapon.thrownBy is Scavenger scav && (scav.Elite || scav.King || scav.Templar || scav.Disciple) && !scav.readyToReleaseMask)
                {
                    if (self.Template.visualRadius > 200)
                    {
                        CreatureTemplate.Relationship.Type vulRel;
                        if (scav.King || scav.Disciple)
                            vulRel = self.Template.relationships[CreatureTemplate.Type.KingVulture.Index].type;
                        else
                            vulRel = self.Template.relationships[CreatureTemplate.Type.Vulture.Index].type;

                        if (vulRel != CreatureTemplate.Relationship.Type.Attacks && vulRel != CreatureTemplate.Relationship.Type.Eats)
                        {
                            if (self.AI.usedToVultureMask != 0 && self.AI.usedToVultureMask <= ShinyShieldMaskOptions.eliteScavFearDuration.Value * 40)
                            {
                                self.AI.usedToVultureMask += 80;
                                if (self.AI.usedToVultureMask == 0)
                                    self.AI.usedToVultureMask++;
                            }
                        }
                    }
                }
                else if(weapon.thrownBy is Player player)
                {
                    VultureMask mask= null;
                    for(int i = 0; i < player.grasps.Length; ++i)
                    {
                        if (player.grasps[i] != null && player.grasps[i].grabbed is VultureMask m)
                        {
                            mask = m;
                            break;
                        }
                    }
                    if(mask is null)
                    {
                        if (FaceMasksHandler.PlayerFaceMasks.TryGetValue(player, out FaceMask fMask) && fMask.HasAMask)
                            mask = fMask.Mask;
                    }
                    if(mask != null)
                    {
                        CreatureTemplate.Relationship.Type vulRel;
                        float duration = 0;
                        if (mask.King)
                        {
                            vulRel = self.Template.relationships[CreatureTemplate.Type.KingVulture.Index].type;
                            duration = ShinyShieldMaskOptions.kingVultureMaskFearDuration.Value * 40;
                        }
                        else
                        {
                            vulRel = self.Template.relationships[CreatureTemplate.Type.Vulture.Index].type;
                            duration = ShinyShieldMaskOptions.vultureMaskFearDuration.Value * 40;
                        }

                        if (vulRel != CreatureTemplate.Relationship.Type.Attacks && vulRel != CreatureTemplate.Relationship.Type.Eats)
                        {
                            if (self.AI.usedToVultureMask != 0 && self.AI.usedToVultureMask <= duration)
                            {
                                self.AI.usedToVultureMask += 80;
                                if (self.AI.usedToVultureMask == 0)
                                    self.AI.usedToVultureMask++;
                            }
                        }
                    }
                }
            }
                    
                    
        }

        private void StaticWorld_InitStaticWorld(On.StaticWorld.orig_InitStaticWorld orig)
        {
            orig();
            if(StaticWorld.creatureTemplates[CreatureTemplate.Type.GreenLizard.index].relationships[CreatureTemplate.Type.Vulture.index].type == CreatureTemplate.Relationship.Type.Afraid &&
                StaticWorld.creatureTemplates[CreatureTemplate.Type.GreenLizard.index].relationships[CreatureTemplate.Type.Vulture.index].intensity == 0.9f)
            {
                StaticWorld.creatureTemplates[CreatureTemplate.Type.GreenLizard.index].relationships[CreatureTemplate.Type.Vulture.index].type = CreatureTemplate.Relationship.Type.Ignores;
                StaticWorld.creatureTemplates[CreatureTemplate.Type.GreenLizard.index].relationships[CreatureTemplate.Type.Vulture.index].intensity = 0f;
            }
        }

        private void DropCustomMaskOnDeath(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After,
                x => x.MatchStfld<Scavenger>("readyToReleaseMask")
                );
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_2);
            c.EmitDelegate<Action<Scavenger, Vector2?>>((scavenger, velocity) =>
                {
                    VultureMaskGraphics maskfx = (scavenger.graphicsModule as ScavengerGraphics).maskGfx;

                    VultureMask.AbstractVultureMask abstractVultureMask = null;
                    if (EliteMasks.ContainsKey(scavenger.abstractCreature.ID) && EliteMasks[scavenger.abstractCreature.ID].Value != null)
                    {
                        abstractVultureMask = EliteMasks[scavenger.abstractCreature.ID].Value;
                        abstractVultureMask.pos = scavenger.abstractCreature.pos;
                    }
                    else
                    {
                        //This check should never be reached, but just in case
                        abstractVultureMask = new VultureMask.AbstractVultureMask(
                            scavenger.room.world, null, scavenger.room.GetWorldCoordinate(scavenger.firstChunk.pos), scavenger.room.game.GetNewID(),
                            scavenger.abstractCreature.ID.RandomSeed, maskfx.King, maskfx.ScavKing, maskfx.overrideSprite);
                    }

                    scavenger.room.abstractRoom.AddEntity(abstractVultureMask);
                    abstractVultureMask.RealizeInRoom();
                    if(abstractVultureMask.realizedObject is VultureMask realizedMask)
                    {
                        realizedMask.rotVel = new Vector2(20f, 0f);
                        realizedMask.firstChunk.vel = (!velocity.HasValue ? Vector2.zero : velocity.Value) * 20f;
                    }
                    if (EliteMasks.ContainsKey(scavenger.abstractCreature.ID))
                    {
                        EliteMasks[scavenger.abstractCreature.ID] = new KeyValuePair<bool, VultureMask.AbstractVultureMask>(
                            false,
                            abstractVultureMask);
                    }
                }
            );
            c.Emit(OpCodes.Ret);
        }

        private void CheckIfNeedsEquipMask(On.Scavenger.orig_PickUpAndPlaceInInventory orig, Scavenger self, PhysicalObject obj, bool lethalityBypass = false)
        {
            orig(self, obj, lethalityBypass);
            if(obj is VultureMask && (self.Elite || self.King) && self.readyToReleaseMask)
            {
                (self.abstractCreature.abstractAI as ScavengerAbstractAI).bringPearlHome = true;
            }
        }

        private void CheckReplenishMask(On.ScavengerAbstractAI.orig_ReGearInDen orig, ScavengerAbstractAI self)
        {
            orig(self);
            if (self.parent.creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.ScavengerElite)
            {
                if (EliteMasks.ContainsKey(self.parent.ID) && !EliteMasks[self.parent.ID].Key)
                {
                    for (int i = self.parent.stuckObjects.Count - 1; i >= 0; i--)
                    {
                        if (self.parent.stuckObjects[i] is AbstractPhysicalObject.CreatureGripStick && self.parent.stuckObjects[i].A == self.parent)
                        {
                            if (self.parent.stuckObjects[i].B.type == AbstractPhysicalObject.AbstractObjectType.VultureMask)
                            {
                                EliteMasks[self.parent.ID] = new KeyValuePair<bool, VultureMask.AbstractVultureMask>(
                                    true,
                                    self.parent.stuckObjects[i].B as VultureMask.AbstractVultureMask);
                                self.parent.stuckObjects[i].Deactivate();
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void CheckDictionaryDeletion(On.AbstractWorldEntity.orig_Destroy orig, AbstractWorldEntity self)
        {
            orig(self);

            if (self is AbstractCreature creature && (creature.creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.ScavengerElite ||
                creature.creatureTemplate.type == Watcher.WatcherEnums.CreatureTemplateType.ScavengerTemplar))
            {
                EliteMasks.Remove(creature.ID);
                MaskedTemplars.Remove(creature.ID);
            }
        }

        private void CtorCheckMaskDictionary(On.Scavenger.orig_ctor orig, Scavenger self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (self.Elite || self.King || self.Templar)
            {
                if (EliteMasks.ContainsKey(self.abstractCreature.ID))
                {
                    self.readyToReleaseMask = !EliteMasks[self.abstractCreature.ID].Key;
                    //Dictionary's true means it has a mask, false it doesn't. readyToReleaseMask must be the opposite.
                }
                else if (MaskedTemplars.ContainsKey(self.abstractCreature.ID))
                {
                    self.readyToReleaseMask = MaskedTemplars[self.abstractCreature.ID];
                }
                else
                {
                    UnityEngine.Random.InitState(self.abstractCreature.ID.RandomSeed);
                    float chance = ShinyShieldMaskOptions.masklessEliteChance.Value / 10f;
                    if (self.Templar)
                        chance *= 0.75f;
                    if (self.King)
                        chance = 0f;
                    bool hasMask = UnityEngine.Random.value > chance;
                    if (self.Templar)
                        MaskedTemplars.Add(self.abstractCreature.ID, hasMask);
                    else
                        EliteMasks.Add(abstractCreature.ID, new KeyValuePair<bool, VultureMask.AbstractVultureMask>(hasMask, null));
                    self.readyToReleaseMask = !hasMask;
                }
            }
        }

        private void GFXCtorCheckMaskDictionary(On.ScavengerGraphics.orig_ctor orig, ScavengerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);

            //This check should never happen, but just in case
            if (self.scavenger.Elite || self.scavenger.King)
            {
                if (!EliteMasks.ContainsKey(self.scavenger.abstractCreature.ID))
                {
                    EliteMasks.Add(self.scavenger.abstractCreature.ID, new KeyValuePair<bool, VultureMask.AbstractVultureMask>(!self.scavenger.readyToReleaseMask, null));
                }
            }

            if (EliteMasks.ContainsKey(self.scavenger.abstractCreature.ID))
            {
                if (EliteMasks[self.scavenger.abstractCreature.ID].Value is null && !self.scavenger.readyToReleaseMask)
                {
                    VultureMask.AbstractVultureMask mask = new VultureMask.AbstractVultureMask(self.scavenger.abstractCreature.world, null, self.scavenger.abstractCreature.pos, self.scavenger.abstractCreature.world.game.GetNewID(), 
                        self.scavenger.abstractCreature.ID.RandomSeed, self.maskGfx.King, self.maskGfx.ScavKing, self.maskGfx.overrideSprite);
                    EliteMasks[self.scavenger.abstractCreature.ID] = new KeyValuePair<bool, VultureMask.AbstractVultureMask>(EliteMasks[self.scavenger.abstractCreature.ID].Key, mask);
                }
                else if(!self.scavenger.readyToReleaseMask)
                {
                    self.maskGfx = new VultureMaskGraphics(self.scavenger,
                        EliteMasks[self.scavenger.abstractCreature.ID].Value, self.MaskSprite);
                    self.maskGfx.GenerateColor(EliteMasks[self.scavenger.abstractCreature.ID].Value.colorSeed);
                }
            }
        }

        public void SetFaceMaskHooks()
        {
            On.Player.Update += faceMasksHandler.FaceMaskUpdate;
            On.Player.ObjectEaten += faceMasksHandler.ObjectEatenWithFaceMask;
            On.Player.Die += faceMasksHandler.DropFaceMaskOnDeath;
            On.Player.Stun += faceMasksHandler.DropMaskOnStun;
            //On.Player.Destroy += faceMasksHandler.Player_Destroy;
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

            faceMasksHandler.SetVariables(hasLancerMod, hasDropButton, hasImprovedInput);
        }

        private void RainWorldGameOnShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
        {
            EliteMasks.Clear();
            MaskedTemplars.Clear();
            orig(self);
        }

        private void GameSessionOnctor(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
        {
            rwGame = new WeakReference(game);
            orig(self, game);
        }

        private CreatureTemplate.Relationship LizardAI_IUseARelationshipTracker_UpdateDynamicRelationship1(On.LizardAI.orig_IUseARelationshipTracker_UpdateDynamicRelationship orig, LizardAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            //If user did not set elite fear, or something is wrong with the parameters
            if ((ShinyShieldMaskOptions.eliteScavFearDuration.Value == 0) ||
                dRelation is null || dRelation.trackerRep is null || dRelation.trackerRep.representedCreature is null ||
                dRelation.trackerRep.representedCreature.realizedCreature is null)
                return orig(self, dRelation);
            //If target is not elite or king scav
            CreatureTemplate.Type targetType = dRelation.trackerRep.representedCreature.creatureTemplate.type;
            if(!(targetType == DLCSharedEnums.CreatureTemplateType.ScavengerElite ||
                targetType == MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing ||
                targetType == Watcher.WatcherEnums.CreatureTemplateType.ScavengerTemplar ||
                targetType == Watcher.WatcherEnums.CreatureTemplateType.ScavengerDisciple))
                return orig(self, dRelation);
            bool isKing = targetType == MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing || targetType == Watcher.WatcherEnums.CreatureTemplateType.ScavengerDisciple;
            //If target is elite without a mask on
            if (dRelation.trackerRep.representedCreature.realizedCreature is Scavenger scav && (scav.Elite || scav.Templar) && scav.readyToReleaseMask)
                return orig(self, dRelation);
            //If user is blind
            if (self.lizard.Template.visualRadius < 200)
                return orig(self, dRelation);
            CreatureTemplate.Relationship baseRep = self.lizard.Template.relationships[dRelation.trackerRep.representedCreature.creatureTemplate.type.index];
            CreatureTemplate.Relationship vultureRep = self.lizard.Template.relationships[CreatureTemplate.Type.Vulture.Index];
            CreatureTemplate.Relationship kingVultureRep = self.lizard.Template.relationships[CreatureTemplate.Type.KingVulture.Index];

            //If user naturally attacks vultures
            if(isKing)
                if (kingVultureRep.type == CreatureTemplate.Relationship.Type.Attacks ||
                kingVultureRep.type == CreatureTemplate.Relationship.Type.Eats)
                    return orig(self, dRelation);
            else
                if (vultureRep.type == CreatureTemplate.Relationship.Type.Attacks ||
                vultureRep.type == CreatureTemplate.Relationship.Type.Eats)
                    return orig(self, dRelation);
            int maxUsed = (!isKing) ? ShinyShieldMaskOptions.eliteScavFearDuration.Value * 40 : ShinyShieldMaskOptions.eliteScavFearDuration.Value * 80;
            if (self.usedToVultureMask < maxUsed && !dRelation.trackerRep.representedCreature.realizedCreature.dead)
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

                bool itHasThreat = false, itHasPrey = false;
                foreach (ThreatTracker.ThreatCreature threat in self.threatTracker.threatCreatures)
                {
                    if (threat.creature == dRelation.trackerRep)
                    {
                        itHasThreat = true;
                        break;
                    }
                }
                foreach (PreyTracker.TrackedPrey prey in self.preyTracker.prey)
                {
                    if (prey.critRep == dRelation.trackerRep)
                    {
                        itHasPrey = true;
                        break;
                    }
                }
                if (itHasPrey)
                    self.preyTracker.ForgetPrey(dRelation.trackerRep.representedCreature);

                if (!itHasThreat && vultureRep.type == CreatureTemplate.Relationship.Type.Afraid)
                    self.threatTracker.AddThreatCreature(dRelation.trackerRep);
                if (isKing)
                {
                    dRelation.currentRelationship.type = kingVultureRep.type;
                    dRelation.currentRelationship.intensity = kingVultureRep.intensity * Mathf.InverseLerp(maxUsed, 0, self.usedToVultureMask * 0.7f);
                }
                else
                {
                    dRelation.currentRelationship.type = vultureRep.type;
                    dRelation.currentRelationship.intensity = vultureRep.intensity * Mathf.InverseLerp(maxUsed, 0, self.usedToVultureMask * 0.7f);
                }
                return dRelation.currentRelationship;
            }
            else if (dRelation.currentRelationship.type != baseRep.type)
            {
                bool itHasThreat = false;
                foreach (ThreatTracker.ThreatCreature threat in self.threatTracker.threatCreatures)
                {
                    if (threat.creature == dRelation.trackerRep)
                    {
                        itHasThreat = true;
                    }
                }
                if (itHasThreat && baseRep.type != CreatureTemplate.Relationship.Type.Afraid)
                    self.threatTracker.RemoveThreatCreature(dRelation.trackerRep.representedCreature);

                bool itHasPrey = false;
                foreach (PreyTracker.TrackedPrey prey in self.preyTracker.prey)
                {
                    if (prey.critRep == dRelation.trackerRep)
                    {
                        itHasPrey = true;
                        break;
                    }
                }
                if (!itHasPrey && (baseRep.type == CreatureTemplate.Relationship.Type.Eats ||
                    baseRep.type == CreatureTemplate.Relationship.Type.Attacks))
                    self.preyTracker.AddPrey(dRelation.trackerRep);

                dRelation.currentRelationship = baseRep;
                return dRelation.currentRelationship;
            }
            else
                return orig(self, dRelation);
        }

        //Lizards afraid of vulture masks: Now depends on their relationship with vultures and their visual radius.
        //If they eat/attack vultures or are blind, they ignore masks. Otherwise, if they are not afraid of vultures,
        //they simply ignore the mask user like greens.
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


            //Resetting: now un-hardcoding lizard's vulture mask behaviours.
            c.Index = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld<CreatureTemplate.Type>("BlackLizard"));  //Blind lizards, instead of black lizards, ignore masks
            c.Index++;
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<LizardAI, bool>>(self =>
            {
                return self.lizard.Template.visualRadius > 200;
            });
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld<CreatureTemplate.Type>("RedLizard"));    //Vulture-attacking lizards, instead of red lizards, ignore masks
            c.Index++;
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<LizardAI, RelationshipTracker.DynamicRelationship, bool>>((self, dRelation) =>
            {
                if ((dRelation.state is LizardAI.LizardTrackState state) && state.vultureMask == 2)
                    return self.lizard.Template.relationships[CreatureTemplate.Type.KingVulture.Index].type != CreatureTemplate.Relationship.Type.Attacks &&
                            self.lizard.Template.relationships[CreatureTemplate.Type.KingVulture.Index].type != CreatureTemplate.Relationship.Type.Eats;
                else
                    return self.lizard.Template.relationships[CreatureTemplate.Type.Vulture.Index].type != CreatureTemplate.Relationship.Type.Attacks &&
                        self.lizard.Template.relationships[CreatureTemplate.Type.Vulture.Index].type != CreatureTemplate.Relationship.Type.Eats;
            });

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld<CreatureTemplate.Type>("GreenLizard"));  //Vulture-ignoring lizards is now deprecated
            c.Index++;
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<bool>>(() => { return false; });


            c.Index = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(0.4f),
                x => x.MatchMul()
                );
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<LizardAI, RelationshipTracker.DynamicRelationship, CreatureTemplate.Relationship.Type>>((self, dRelation) =>
            {
                int vultureIndex = ((dRelation.state is LizardAI.LizardTrackState state) && state.vultureMask == 2) ? CreatureTemplate.Type.KingVulture.index : CreatureTemplate.Type.Vulture.index;

                return self.lizard.Template.relationships[vultureIndex].type;
            });
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<LizardAI, RelationshipTracker.DynamicRelationship, float>>((self, dRelation) =>
            {
                int vultureIndex = ((dRelation.state is LizardAI.LizardTrackState state) && state.vultureMask == 2) ? CreatureTemplate.Type.KingVulture.index : CreatureTemplate.Type.Vulture.index;

                float intensity = self.lizard.Template.relationships[vultureIndex].intensity;
                if ((dRelation.state as LizardAI.LizardTrackState).vultureMask == 1)
                    intensity *= Mathf.InverseLerp(ShinyShieldMaskOptions.vultureMaskFearDuration.Value * 40, 0, self.usedToVultureMask * 0.7f);
                else
                    intensity *= Mathf.InverseLerp(ShinyShieldMaskOptions.kingVultureMaskFearDuration.Value * 40, 0, self.usedToVultureMask * 0.7f);

                return intensity;
            });
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
                    
                    if ((mask.maskGfx.overrideSprite != null && (mask.maskGfx.overrideSprite == "KrakenMask" ||
                        mask.maskGfx.overrideSprite == "SpikeMask" || mask.maskGfx.overrideSprite == "HornedMask" ||
                        mask.maskGfx.overrideSprite == "SadMask"))
                        || mask.maskGfx.maskType == VultureMask.MaskType.SCAVTEMPLAR)
                    {
                        knockback *= .85f;
                        maskKnockback *= .75f;
                        stunBonus = ShinyShieldMaskOptions.eliteScavMaskStun.Value;
                    }
                    else if (mask.AbstrMsk.scavKing)
                    {
                        knockback *= .65f;
                        maskKnockback *= .55f;
                        stunBonus = ShinyShieldMaskOptions.scavKingMaskStun.Value;
                    }
                    else if (mask.King)
                    {
                        knockback *= .85f;
                        maskKnockback *= .75f;
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

                    HitEffect(self, player, self.firstChunk.vel, result.collisionPoint, self.room);
                    SetSpearBouncing(self);
                    return false;
                }
                else
                    return orig(self, result, eu);
            }
            else if(result.obj is Scavenger scavenger && !(scavenger is null) && (scavenger.Elite || scavenger.King || scavenger.Templar || scavenger.Disciple))
            {
                if (result.chunk == scavenger.bodyChunks[2] && !scavenger.State.dead && !scavenger.readyToReleaseMask && !scavenger.KarmicArmorProtected)
                {
                    bool frontalHit = Vector2.Dot(self.firstChunk.vel.normalized, scavenger.HeadLookDir) >= -(scavenger.King ? ShinyShieldMaskOptions.eliteResistance.Value * 1.5f : ShinyShieldMaskOptions.eliteResistance.Value);

                    if (frontalHit)
                    {
                        Vector2 knockback = self.firstChunk.vel * self.firstChunk.mass / scavenger.bodyChunks[2].mass;
                        scavenger.bodyChunks[2].vel += knockback;

                        if (!scavenger.King)
                            scavenger.Violence(self.firstChunk, self.firstChunk.vel, scavenger.bodyChunks[2], result.onAppendagePos, Creature.DamageType.Blunt, 0.04f, 5f);

                        //de-mask scavenger
                        if (ShinyShieldMaskOptions.demaskableElites.Value && !scavenger.King && !scavenger.Disciple)
                        {
                            scavenger.readyToReleaseMask = true;
                            VultureMaskGraphics maskfx = (scavenger.graphicsModule as ScavengerGraphics).maskGfx;

                            VultureMask.AbstractVultureMask abstractVultureMask = null;
                            if (scavenger.Elite && EliteMasks.ContainsKey(scavenger.abstractCreature.ID) && EliteMasks[scavenger.abstractCreature.ID].Value != null)
                            {
                                abstractVultureMask = EliteMasks[scavenger.abstractCreature.ID].Value;
                                abstractVultureMask.pos = scavenger.abstractCreature.pos;
                            }
                            else if (scavenger.Templar)
                            {
                                abstractVultureMask = new VultureMask.AbstractVultureMask(
                                    scavenger.room.world, null, scavenger.room.GetWorldCoordinate(scavenger.firstChunk.pos), scavenger.room.game.GetNewID(),
                                    TEMPLAR_MASK_ID, false, false, null);
                                if (!MaskedTemplars.ContainsKey(scavenger.abstractCreature.ID))
                                    MaskedTemplars.Add(scavenger.abstractCreature.ID, false);
                                else
                                    MaskedTemplars[scavenger.abstractCreature.ID] = false;
                            }
                            else
                            {
                                abstractVultureMask = new VultureMask.AbstractVultureMask(
                                    scavenger.room.world, null, scavenger.room.GetWorldCoordinate(scavenger.firstChunk.pos), scavenger.room.game.GetNewID(),
                                    scavenger.abstractCreature.ID.RandomSeed, maskfx.King, maskfx.ScavKing, maskfx.overrideSprite);
                            }

                            self.room.abstractRoom.AddEntity(abstractVultureMask);
                            abstractVultureMask.RealizeInRoom();
                            if (abstractVultureMask.realizedObject is VultureMask realizedMask)
                            {
                                realizedMask.rotVel = new Vector2(20f, 0f);
                                realizedMask.firstChunk.vel = self.firstChunk.vel.normalized * 20f;
                            }
                            if (EliteMasks.ContainsKey(scavenger.abstractCreature.ID))
                            {
                                EliteMasks[scavenger.abstractCreature.ID] = new KeyValuePair<bool, VultureMask.AbstractVultureMask>(
                                    false,
                                    abstractVultureMask);
                            }
                        }


                        HitEffect(self, scavenger, self.firstChunk.vel, result.collisionPoint, self.room);
                        SetSpearBouncing(self);

                        return false;
                    }
                }
                else if (scavenger.King && !scavenger.State.dead && scavenger.armorPieces > 0)
                {
                    HitEffect(self, scavenger, self.firstChunk.vel, result.collisionPoint, self.room);
                }
                else if (scavenger.Templar && scavenger.dead && ShinyShieldMaskOptions.demaskableElites.Value && !scavenger.readyToReleaseMask)
                {
                    scavenger.readyToReleaseMask = true;
                    VultureMaskGraphics maskfx = (scavenger.graphicsModule as ScavengerGraphics).maskGfx;
                    VultureMask.AbstractVultureMask abstractVultureMask = new VultureMask.AbstractVultureMask(
                        scavenger.room.world, null, scavenger.room.GetWorldCoordinate(scavenger.firstChunk.pos), scavenger.room.game.GetNewID(),
                        TEMPLAR_MASK_ID, false, false, null);
                    self.room.abstractRoom.AddEntity(abstractVultureMask);
                    abstractVultureMask.RealizeInRoom();
                    if (abstractVultureMask.realizedObject is VultureMask realizedMask)
                    {
                        realizedMask.rotVel = new Vector2(20f, 0f);
                        realizedMask.firstChunk.vel = self.firstChunk.vel.normalized * 20f;
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

        public void HitEffect(Spear spear, Creature hitCrit, Vector2 impactVelocity, Vector2 basePos, Room room)
        {
            if(spear is ElectricSpear eSpear)
            {
                eSpear.Zap();
                eSpear.Electrocute(hitCrit);
                hitCrit.stun = 40;
                if(hitCrit is Scavenger scav && scav.King)
                    scav.explosionDamageCooldown = 0;
            }

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