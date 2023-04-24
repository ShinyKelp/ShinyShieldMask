using RWCustom;
using UnityEngine;

namespace ShinyShieldMask
{
    //Adapted from Pinocular's Lancer mod (MaskOnHorn class)
    public class FaceMask
    {
        public FaceMask(Player owner)
        {
            this.player = owner;
            increment = false;
            interactionLocked = false;
        }

        public Player player;
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
            bool hasSpear = false, hasSlug = false;
            for (int i = 0; i < 2; i++)
            {
                if (player.grasps[i]?.grabbed is IPlayerEdible)
                    return false;
                if (player.grasps[i]?.grabbed is Spear)
                    hasSpear = true;
                else if (player.grasps[i]?.grabbed is Player)
                    hasSlug = true;
            }

            hasSpear = hasSpear || (!(player.spearOnBack is null) && player.spearOnBack.HasASpear);
            hasSlug = hasSlug || (!(player.slugOnBack is null) && player.slugOnBack.HasASlug);

            if (((player.CanPutSpearToBack || player.CanRetrieveSpearFromBack) && hasSpear) ||
                ((player.CanPutSlugToBack || player.CanRetrieveSlugFromBack) && hasSlug))
                return false;

            return !HasAMask && (player.grasps[0]?.grabbed is VultureMask || player.grasps[1]?.grabbed is VultureMask);
        }

        public bool CanRetrieveMaskFromFace()
        {
            int grasp = -1;
            bool hasSpear = false, hasSlug = false;
            for (int i = 0; i < 2; i++)
            {
                if (player.grasps[i] is null) 
                {
                    if(grasp == -1)
                        grasp = i; 
                    continue; 
                }
                if (player.grasps[i]?.grabbed is IPlayerEdible)
                    return false; 
                if (player.Grabability(player.grasps[i].grabbed) >= Player.ObjectGrabability.TwoHands) 
                    return false;

                if (player.grasps[i]?.grabbed is Spear)
                    hasSpear = true;
                else if (player.grasps[i]?.grabbed is Player)
                    hasSlug = true;
            }

            hasSpear = hasSpear || (!(player.spearOnBack is null) && player.spearOnBack.HasASpear);
            hasSlug = hasSlug || (!(player.slugOnBack is null) && player.slugOnBack.HasASlug);

            if (((player.CanPutSpearToBack || player.CanRetrieveSpearFromBack) && hasSpear) ||
                ((player.CanPutSlugToBack || player.CanRetrieveSlugFromBack) && hasSlug))
                return false;

            return HasAMask && grasp > -1;
        }

        public void Update(bool eu)
        {
            if (HasAMask)
            {
                if (Mask.slatedForDeletetion)
                {
                    abstractStick?.Deactivate();
                    Mask = null;
                    return;
                }

                Mask.Forbid();
                Mask.bodyChunks[0].pos = player.bodyChunks[0].pos;

                foreach (Creature.Grasp grasp in player.grasps)
                {
                    if(!(grasp is null) && grasp.grabbed is VultureMask mask)
                    {
                        mask.donned = 0;
                        mask.lastDonned = 0;
                    }
                }
            }

            increment = player.input[0].pckp && !interactionLocked
                && (CanPutMaskOnFace() || CanRetrieveMaskFromFace());

            if (player.input[0].pckp && !(player.grasps[0] is null) && player.grasps[0].grabbed is Creature creature &&
                player.CanEatMeat(creature) && creature.Template.meatPoints > 0)
                LockInteraction();
            else if (player.swallowAndRegurgitateCounter > 90)
                LockInteraction();
            
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
                    for (int i = 0; i < player.grasps.Length; ++i)
                    {
                        if (player.grasps[i] != null && player.grasps[i].grabbed is VultureMask mask)
                        {
                            Vector2 knockback = Custom.DirVec(player.grasps[i].grabbed.firstChunk.pos, player.bodyChunks[0].pos) * 2f;
                            if (mask.donned > 0.9)
                                knockback.y *= 2;
                            player.bodyChunks[0].vel += knockback;
                            MaskToFace(player.grasps[i].grabbed as VultureMask);
                            counter = 0;
                            break;
                        }
                    }
                }
            }
            else counter = 0;
            if (!player.input[0].pckp)
                interactionLocked = false;
            increment = false;
        }

        public void MaskToHand(bool eu)
        {
            if (Mask == null) return;
            for (int i = 0; i < player.grasps.Length; i++)
            {
                if (player.grasps[i] != null)
                {
                    if ((int)player.Grabability(this.player.grasps[i].grabbed) >= 3) { return; }
                }
            }
            int num = -1;
            int num2 = 0;
            while (num2 < 2 && num == -1)
            {
                if (player.grasps[num2] == null) num = num2;
                ++num2;
            }
            if (num == -1) return;
            if (player.graphicsModule != null)
                Mask.firstChunk.MoveFromOutsideMyUpdate(eu, (player.graphicsModule as PlayerGraphics).hands[num].pos);
            
            player.SlugcatGrab(Mask, num);
            Mask.donned = -1;
            Mask.lastDonned = -1;

            Mask = null;
            interactionLocked = true;
            player.noPickUpOnRelease = 20;
            player.room.PlaySound(SoundID.Vulture_Mask_Pick_Up, player.mainBodyChunk);
            abstractStick?.Deactivate();
            abstractStick = null;
        }

        public void MaskToFace(VultureMask mask)
        {
            if (Mask != null) return;
            for (int i = 0; i < player.grasps.Length; ++i)
            {
                if (player.grasps[i] != null && player.grasps[i].grabbed == mask)
                {
                    player.ReleaseGrasp(i);
                    break;
                }
            }
            Mask = mask;
            mask.Forbid();
            interactionLocked = true;
            player.noPickUpOnRelease = 20;
            player.room.PlaySound(SoundID.Slugcat_Stash_Spear_On_Back, player.mainBodyChunk.pos, 1.5f, 1.3f);
            abstractStick?.Deactivate();
            abstractStick = new AbstractFaceMask(player.abstractPhysicalObject, Mask.abstractPhysicalObject, this);
        }

        public void DropMask(bool fling = false)
        {
            if (Mask == null) return;
            Mask.firstChunk.pos = player.mainBodyChunk.pos;
            Mask.forbiddenToPlayer = 10;
            if (fling)
            {
                Vector2 dir = Custom.RNV(); if (dir.y < 0f) dir.y = -dir.y;
                Mask.firstChunk.vel = player.mainBodyChunk.vel + dir * (9f * UnityEngine.Random.value + 6f);
            }
            else
                Mask.firstChunk.vel = player.mainBodyChunk.vel + Custom.RNV() * (3f * UnityEngine.Random.value);
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
