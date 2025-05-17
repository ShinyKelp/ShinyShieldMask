using ImprovedInput;
using RWCustom;
using System;
using UnityEngine;

namespace ShinyShieldMask
{
    //Adapted from Pinocular's Lancer mod (MaskOnHorn class)
    public class FaceMask
    {
        public FaceMask(Player owner)
        {
            playerRef = new WeakReference(owner);
            increment = false;
            interactionLocked = false;
        }
        private WeakReference playerRef;

        public Player Player { get { return playerRef.Target is Player player ? player : null; } }
        public VultureMask Mask { get; private set; }
        private bool increment;
        private bool interactionLocked;
        internal AbstractFaceMask abstractStick;
        private int counter;
        public bool HasAMask => Mask != null;

        public void LockInteraction()
        {
            increment = false;
            interactionLocked = true;
        }

        public bool CanPutMaskOnFace()
        {
            return !HasAMask && (Player.grasps[0]?.grabbed is VultureMask || Player.grasps[1]?.grabbed is VultureMask);
        }

        public bool CanRetrieveMaskFromFace()
        {
            int grasp = -1;
            for (int i = 0; i < 2; i++)
            {
                if (Player.grasps[i] is null)
                {
                    if (grasp == -1)
                        grasp = i;
                    continue;
                }
                if (Player.Grabability(Player.grasps[i].grabbed) >= Player.ObjectGrabability.TwoHands)
                    return false;
            }
            return HasAMask && grasp > -1;
        }

        private bool IsPressingMaskButton()
        {
            if (!FaceMasksHandler.hasImprovedInput)
            {
                if (!ShinyShieldMaskOptions.wearableMaskAlternateInput.Value)
                    return Player.input[0].pckp;
                else return Player.input[0].pckp && Player.input[0].y > 0;
            }
            else return CheckImprovedInput();
        }
        private bool CheckImprovedInput()
        {
            if (!ShinyShieldMaskOptions.wearableMaskAlternateInput.Value)
                return Player.IsPressed(FaceMasksHandler.maskButton as PlayerKeybind);
            else return Player.IsPressed(FaceMasksHandler.maskButton as PlayerKeybind) && Player.input[0].y > 0;
        }

        public void CheckForbiddenInteractions()
        {
            if (FaceMasksHandler.hasImprovedInput)
                return;
            if (Player.input[0].pckp && !(Player.grasps[0] is null) && Player.grasps[0].grabbed is Creature creature &&
                (Player.CanEatMeat(creature) || Player.CanMaulCreature(creature)))
                LockInteraction();
            else if (!ShinyShieldMaskOptions.wearableMaskAlternateInput.Value && (CanPutMaskOnFace() || CanRetrieveMaskFromFace()))
            {
                if (Player.swallowAndRegurgitateCounter > 0)
                {
                    LockInteraction();
                    return;
                }
                bool hasSpear = false, hasSlug = false;
                for (int i = 0; i < 2; i++)
                {
                    if (Player.grasps[i]?.grabbed is IPlayerEdible)
                    {
                        LockInteraction();
                        return;
                    }
                    if (Player.grasps[i]?.grabbed is Spear)
                        hasSpear = true;
                    else if (Player.grasps[i]?.grabbed is Player)
                        hasSlug = true;
                }

                hasSpear = hasSpear || (!(Player.spearOnBack is null) && Player.spearOnBack.HasASpear);
                hasSlug = hasSlug || (!(Player.slugOnBack is null) && Player.slugOnBack.HasASlug);
                if (((Player.CanPutSpearToBack || Player.CanRetrieveSpearFromBack) && hasSpear) ||
                        ((Player.CanPutSlugToBack || Player.CanRetrieveSlugFromBack) && hasSlug))
                    LockInteraction();
            }
        }

        public void Update(bool eu)
        {
            if (HasAMask)
            {
                if (ShinyShieldMaskOptions.scavKingMaskImmunity.Value && Mask.maskGfx.ScavKing)
                    Player.scavengerImmunity = 2400;
                else if (ShinyShieldMaskOptions.templarMaskImmunity.Value && Mask.maskGfx.maskType == VultureMask.MaskType.SCAVTEMPLAR)
                    Player.scavengerImmunity = 160;
                if (Mask.slatedForDeletetion)
                {
                    abstractStick?.Deactivate();
                    Mask = null;
                    return;
                }
                Mask.Forbid();
                foreach (Creature.Grasp grasp in Player.grasps)
                {
                    if(!(grasp is null) && grasp.grabbed is VultureMask mask)
                    {
                        mask.donned = 0;
                        mask.lastDonned = 0;
                    }
                }
            }

            CheckForbiddenInteractions();
            increment = IsPressingMaskButton() && (CanPutMaskOnFace() || CanRetrieveMaskFromFace());
            
            if (!interactionLocked && increment)
            {
                ++counter;
                if (Mask != null && counter > 20)
                {
                    MaskToHand(eu);
                    counter = 0;
                }
                else if (Mask == null && counter > 20)
                {
                    for (int i = 0; i < Player.grasps.Length; ++i)
                    {
                        if (Player.grasps[i] != null && Player.grasps[i].grabbed is VultureMask mask)
                        {
                            Vector2 knockback = Custom.DirVec(Player.grasps[i].grabbed.firstChunk.pos, Player.bodyChunks[0].pos) * 2f;
                            if (mask.donned > 0.9)
                                knockback.y *= 2;
                            Player.bodyChunks[0].vel += knockback;
                            MaskToFace(Player.grasps[i].grabbed as VultureMask);
                            counter = 0;
                            break;
                        }
                    }
                }
            }
            else counter = 0;
            if (!Player.input[0].pckp)
                interactionLocked = false;
            increment = false;
        }

        public void MaskToHand(bool eu)
        {
            if (Mask == null) return;
            Mask.firstChunk.pos = Player.mainBodyChunk.pos;
            for (int i = 0; i < Player.grasps.Length; i++)
            {
                if (Player.grasps[i] != null)
                {
                    if ((int)Player.Grabability(this.Player.grasps[i].grabbed) >= 3) { return; }
                }
            }
            int num = -1;
            int num2 = 0;
            while (num2 < 2 && num == -1)
            {
                if (Player.grasps[num2] == null) num = num2;
                ++num2;
            }
            if (num == -1) return;
            if (Player.graphicsModule != null)
                Mask.firstChunk.MoveFromOutsideMyUpdate(eu, (Player.graphicsModule as PlayerGraphics).hands[num].pos);
            
            Player.SlugcatGrab(Mask, num);
            Mask.donned = -1;
            Mask.lastDonned = -1;

            Mask = null;
            interactionLocked = true;
            Player.noPickUpOnRelease = 20;
            Player.room.PlaySound(SoundID.Vulture_Mask_Pick_Up, Player.mainBodyChunk);
            abstractStick?.Deactivate();
            abstractStick = null;
        }

        public void MaskToFace(VultureMask mask)
        {
            if (Mask != null) return;
            for (int i = 0; i < Player.grasps.Length; ++i)
            {
                if (Player.grasps[i] != null && Player.grasps[i].grabbed == mask)
                {
                    Player.ReleaseGrasp(i);
                    break;
                }
            }
            Mask = mask;
            mask.Forbid();
            interactionLocked = true;
            Player.noPickUpOnRelease = 20;
            Player.room.PlaySound(SoundID.Slugcat_Stash_Spear_On_Back, Player.mainBodyChunk.pos, 1.5f, 1.3f);
            abstractStick?.Deactivate();
            abstractStick = new AbstractFaceMask(Player.abstractPhysicalObject, Mask.abstractPhysicalObject, this);
        }

        public void DropMask(bool fling = false)
        {
            if (Mask == null) return;
            Mask.firstChunk.pos = Player.mainBodyChunk.pos;
            Mask.forbiddenToPlayer = 10;
            if (fling)
            {
                Vector2 dir = Custom.RNV(); if (dir.y < 0f) dir.y = -dir.y;
                Mask.firstChunk.vel = Player.mainBodyChunk.vel + dir * (9f * UnityEngine.Random.value + 6f);
            }
            else
                Mask.firstChunk.vel = Player.mainBodyChunk.vel + Custom.RNV() * (3f * UnityEngine.Random.value);
            Mask = null;
            abstractStick?.Deactivate();
            abstractStick = null;
        }

        public class AbstractFaceMask : AbstractPhysicalObject.AbstractObjectStick
        {
            public AbstractFaceMask(AbstractPhysicalObject player, AbstractPhysicalObject mask, FaceMask faceMask) : base(player, mask)
            {
                this.faceMask = faceMask;
                this.abstractGrasp = new Creature.Grasp((player.realizedObject as Player), (mask.realizedObject as VultureMask), 0, 0, Creature.Grasp.Shareability.CanNotShare, 0f, false);
            }

            public readonly Creature.Grasp abstractGrasp;
            public readonly FaceMask faceMask;

            public AbstractPhysicalObject Player
            {
                get { return A; }
                set { A = value; }
            }

            public AbstractPhysicalObject Mask
            {
                get { return B; }
                set { B = value; }
            }

            public override string SaveToString(int roomIndex)
            {
                return string.Concat(new string[]
                {
                    roomIndex.ToString(),
                    "<stkA>gripStk<stkA>",
                    A.ID.ToString(),
                    "<stkA>",
                    B.ID.ToString(),
                    "<stkA>",
                    "2",
                    "<stkA>",
                    "1"
                });
            }
        }
    }
}
