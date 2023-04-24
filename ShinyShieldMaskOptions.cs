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
        public static readonly Configurable<int> vultureMaskFearDuration = instance.config.Bind<int>("VultureMaskFear", 18, new ConfigAcceptableRange<int>(0, 999));
        public static readonly Configurable<int> kingVultureMaskFearDuration = instance.config.Bind<int>("KingVultureMaskFear", 30, new ConfigAcceptableRange<int>(0, 999));
        public static readonly Configurable<int> eliteScavFearDuration = instance.config.Bind<int>("EliteScavFear", 0, new ConfigAcceptableRange<int>(0, 999));
        public static readonly Configurable<bool> wearableMask = instance.config.Bind<bool>("WearableMask", false);
        public static readonly Configurable<bool> wearableMaskAlternateInput = instance.config.Bind<bool>("WearableMaskAltInput", false);

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
                new OpLabel(15f, 490f, "Vulture Mask stun value"),
                new OpFloatSlider(vultureMaskStun,new Vector2(20f, 460f), 200)
                {
                    description = "If the stun value is 0, the mask won't be dropped on hit. \nIf it's 1.6 or greater, all items will be dropped."
                },
                new OpLabel(15f, 430f, "King Vulture Mask stun value"),
                new OpFloatSlider(vultureKingMaskStun,new Vector2(20f,400f), 200)
                {
                    description = "If the stun value is 0, the mask won't be dropped on hit. \nIf it's 1.6 or greater, all items will be dropped."
                },
                new OpLabel(15f, 370f, "Elite Scav Mask stun value"),
                new OpFloatSlider(eliteScavMaskStun,new Vector2(20f,340f), 200)
                {
                    description = "If the stun value is 0, the mask won't be dropped on hit. \nIf it's 1.6 or greater, all items will be dropped."
                },
                 new OpLabel(15f, 310f, "Scav King Mask stun value"),
                new OpFloatSlider(scavKingMaskStun,new Vector2(20f,280f), 200)
                {
                    description = "If the stun value is 0, the mask won't be dropped on hit. \nIf it's 1.6 or greater, all items will be dropped."
                },
                new OpLabel(15f, 255f, "Stun applied to Slugcat when deflecting."),

                new OpRect(new Vector2(282f, 365f), new Vector2(260f, 188f)),
                new OpLabel(295f, 510f, "Elite Scavenger mask protection. \nHigher value, likelier to deflect a headshot. \nWorks similar to vultures."),
                new OpFloatSlider(eliteResistance, new Vector2(310f, 460f), 60),

                new OpLabel(295f, 420f, "Lizard afraid of elite scavenger duration"),
                new OpUpdown(eliteScavFearDuration, new Vector2(310f, 380f), 60)
                {
                    description = "Lizards will be afraid of elite scavengers for this duration (zero = vanilla behaviour).\nCAUTION: This timer is SHARED with slugcat's mask timer!"
                },

                new OpRect(new Vector2(0f, 72f), new Vector2(257f, 150f)),
                new OpUpdown(vultureMaskFearDuration, new Vector2(15f, 175f), 60f),
                new OpLabel(80f, 180f, "Normal masks fear duration\n (seconds)"),
                new OpUpdown(kingVultureMaskFearDuration, new Vector2(15f, 130f), 60f),
                new OpLabel(80f, 135f, "King masks fear duration\n (seconds)"),
                new OpCheckBox(randomFearDuration, new Vector2(15f, 85f))
                {
                    description = "Up to 50% more or less duration"
                },
                new OpLabel(43f, 88f, "Randomize fear duration"),

                new OpRect(new Vector2(282f, 223f), new Vector2(250f, 105f)),
                new OpCheckBox(wearableMask, new Vector2(360f, 260f))
                {
                    description = "Hold GRAB to place the mask on your face.",
                },
                new OpLabel(298f, 295f, "Wearable vulture mask", true),
                new OpCheckBox(wearableMaskAlternateInput, new Vector2(300f, 230f))
                {
                    description = "Input to grab/release mask will be UP+GRAB instead."
                },
                new OpLabel(328f, 233f, "Alternate Input"),
                
            };

            opTab.AddItems(UIArrPlayerOptions);

        }

    }
}