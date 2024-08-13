using BepInEx.Logging;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace ShinyShieldMask
{
    public class ShinyShieldMaskOptions : OptionInterface
    {

        public static ShinyShieldMaskOptions instance = new ShinyShieldMaskOptions();

        public static readonly Configurable<float> vultureMaskStun = instance.config.Bind<float>("VultureMaskStun", 1.8f, new ConfigAcceptableRange<float>(0f, 10f));
        public static readonly Configurable<float> vultureKingMaskStun = instance.config.Bind<float>("VultureKingMaskStun", 1f, new ConfigAcceptableRange<float>(0f, 10f));
        public static readonly Configurable<float> scavKingMaskStun = instance.config.Bind<float>("KingMaskStun", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public static readonly Configurable<float> eliteScavMaskStun = instance.config.Bind<float>("EliteScavMaskStun", 1.8f, new ConfigAcceptableRange<float>(0f, 10f));
        public static readonly Configurable<float> eliteResistance = instance.config.Bind<float>("EliteResistance", 0f, new ConfigAcceptableRange<float>(0f, 1f));
        public static readonly Configurable<bool> enableShieldMask = instance.config.Bind<bool>("EnableShieldMask", true);
        public static readonly Configurable<bool> randomFearDuration = instance.config.Bind<bool>("RandomFearDuration", false);
        public static readonly Configurable<bool> attacksFearDuration = instance.config.Bind<bool>("AttackFearDuration", false);
        public static readonly Configurable<int> vultureMaskFearDuration = instance.config.Bind<int>("VultureMaskFear", 18, new ConfigAcceptableRange<int>(0, 999));
        public static readonly Configurable<int> kingVultureMaskFearDuration = instance.config.Bind<int>("KingVultureMaskFear", 30, new ConfigAcceptableRange<int>(0, 999));
        public static readonly Configurable<int> eliteScavFearDuration = instance.config.Bind<int>("EliteScavFear", 0, new ConfigAcceptableRange<int>(0, 999));
        public static readonly Configurable<bool> wearableMask = instance.config.Bind<bool>("WearableMask", false);
        public static readonly Configurable<bool> wearableMaskAlternateInput = instance.config.Bind<bool>("WearableMaskAltInput", false);
        public static readonly Configurable<bool> scavKingMaskImmunity = instance.config.Bind<bool>("ScavKingMaskImmunity", false);
        public static readonly Configurable<bool> demaskableElites = instance.config.Bind<bool>("DemaskableElites", false);
        public static readonly Configurable<float> masklessEliteChance = instance.config.Bind<float>("MasklessEliteChance", 0f, new ConfigAcceptableRange<float>(0f, 10f));


        private UIelement[] UIArrPlayerOptions;

        public override void Initialize()
        {
            var opTab = new OpTab(this, "Options");
            this.Tabs = new[]
            {
            opTab
            };

            UIArrPlayerOptions = new UIelement[]
            {
                new OpLabel(10f, 570f, "Options", true),

                new OpRect(new Vector2 (0f, 248f), new Vector2(257f, 305f)),


                new OpCheckBox(enableShieldMask, new Vector2(15f, 520f)),
                new OpLabel(43f, 523f, "Enable shield mask funcionality"),
                
                new OpLabel(15f, 493f, "Stun applied to Slugcat when \ndeflecting with:"),

                new OpLabel(15f, 470f, "Vulture Mask"),
                new OpFloatSlider(vultureMaskStun,new Vector2(20f, 440f), 200)
                {
                    description = "If the stun value is 0, the mask won't be dropped on hit. \nIf it's 1.6 or greater, all items will be dropped."
                },
                new OpLabel(15f, 410f, "King Vulture Mask"),
                new OpFloatSlider(vultureKingMaskStun,new Vector2(20f,380f), 200)
                {
                    description = "If the stun value is 0, the mask won't be dropped on hit. \nIf it's 1.6 or greater, all items will be dropped."
                },
                new OpLabel(15f, 350f, "Elite Scav Mask"),
                new OpFloatSlider(eliteScavMaskStun,new Vector2(20f,320f), 200)
                {
                    description = "If the stun value is 0, the mask won't be dropped on hit. \nIf it's 1.6 or greater, all items will be dropped."
                },
                 new OpLabel(15f, 290f, "Scav King Mask"),
                new OpFloatSlider(scavKingMaskStun,new Vector2(20f,260f), 200)
                {
                    description = "If the stun value is 0, the mask won't be dropped on hit. \nIf it's 1.6 or greater, all items will be dropped."
                },

                new OpRect(new Vector2(282f, 248f), new Vector2(260f, 305f)),
                new OpLabel(295f, 505f, "Elite Scavenger mask protection."),
                new OpFloatSlider(eliteResistance, new Vector2(310f, 470f), 60)
                {
                    description = "Higher value, likelier to deflect a headshot. \nWorks similar to vultures."
                },

                new OpLabel(295f, 435f, "Lizard afraid of elite scavenger duration"),
                new OpUpdown(eliteScavFearDuration, new Vector2(310f, 395f), 60)
                {
                    description = "Lizards will be afraid of elite scavengers for this duration (zero = vanilla behaviour).\nCAUTION: This timer is SHARED with slugcat's mask timer!"
                },
               new OpLabel(328f, 338f, "Demaskable elites", false),
                new OpCheckBox(demaskableElites, new Vector2(300f, 335f))
                {
                    description = "When an elite scav mask blocks a spear, it will fall off of the scav."
                },

                new OpLabel(295f, 290f, "Maskless Elite chance"),
                new OpFloatSlider(masklessEliteChance,new Vector2(300,260f), 200)
                {
                    description = "Chance for an elite scavenger to spawn without a mask. (10 = 100%)"
                },
                new OpRect(new Vector2(0f, 37f), new Vector2(257f, 185f)),
                new OpUpdown(vultureMaskFearDuration, new Vector2(15f, 175f), 60f),
                new OpLabel(80f, 180f, "Normal masks fear duration\n (seconds)")
                {
                    description = "Includes normal and elite masks. Vanilla duration: 17.5s"
                },
                new OpUpdown(kingVultureMaskFearDuration, new Vector2(15f, 130f), 60f),
                new OpLabel(80f, 135f, "King masks fear duration\n (seconds)")
                {
                    description = "Includes vulture king and scav king masks. Vanilla duration: 30s"
                },
                new OpCheckBox(randomFearDuration, new Vector2(15f, 85f))
                {
                    description = "Up to 50% more or less duration."
                },
                new OpLabel(43f, 88f, "Randomize fear duration"),
                new OpCheckBox(attacksFearDuration, new Vector2(15f, 55f))
                {
                    description = "Attacking a lizard will make the mask diguise run out faster."
                },
                new OpLabel(43f, 58f, "Attacks reduce duration"),

                new OpRect(new Vector2(282f, 72f), new Vector2(250f, 150f)),
                new OpCheckBox(wearableMask, new Vector2(360f, 154f))
                {
                    description = "Hold GRAB/BIND to place the mask on your face.",
                },
                new OpLabel(298f, 189f, "Wearable vulture mask", true),
                new OpCheckBox(wearableMaskAlternateInput, new Vector2(300f, 124f))
                {
                    description = "Input to wear/release mask will be UP+GRAB/BIND instead."
                },
                new OpLabel(328f, 127f, "Alternate Input"),
                new OpCheckBox(scavKingMaskImmunity, new Vector2(300f, 94f))
                {
                    description = "A worn Scavenger King mask will pacify scavengers, same as holding it."
                },
                new OpLabel(328f, 97f, "ScavKing Mask immunity"),

            };

            opTab.AddItems(UIArrPlayerOptions);

        }

    }
}