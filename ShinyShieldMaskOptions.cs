using BepInEx.Logging;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace ShinyShieldMask
{
    public class ShinyShieldMaskOptions : OptionInterface
    {
        private readonly ManualLogSource Logger;

        public static ShinyShieldMaskOptions instance = new ShinyShieldMaskOptions();

        public static readonly Configurable<float> vultureMaskStun = instance.config.Bind<float>("vultureMaskStun", 1.8f, new ConfigAcceptableRange<float>(0f, 10f));
        public static readonly Configurable<float> vultureKingMaskStun = instance.config.Bind<float>("vultureKingMaskStun", 1f, new ConfigAcceptableRange<float>(0f, 10f));
        public static readonly Configurable<float> scavKingMaskStun = instance.config.Bind<float>("kingMaskStun", 0f, new ConfigAcceptableRange<float>(0f, 10f));
        public static readonly Configurable<float> eliteResistance = instance.config.Bind<float>("eliteResistance", .3f, new ConfigAcceptableRange<float>(0f, 1f));
        public static readonly Configurable<bool> enableShieldMask = instance.config.Bind<bool>("enableShieldMask", true);
        public static readonly Configurable<int> vultureMaskFearDuration = instance.config.Bind<int>("vultureMaskFear", 18, new ConfigAcceptableRange<int>(0, 999));
        public static readonly Configurable<int> kingVultureMaskFearDuration = instance.config.Bind<int>("kingVultureMaskFear", 30, new ConfigAcceptableRange<int>(0, 999));
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
                new OpLabel(10f, 560f, "Options", true),
                new OpCheckBox(enableShieldMask, new Vector2(10f, 520f)),
                new OpLabel(38f, 525f, "Enable shield mask funcionality"),
                new OpLabel(10f, 490f, "Vulture Mask stun value"),
                new OpFloatSlider(vultureMaskStun,new Vector2(10f,460f), 200),
                new OpLabel(10f, 430f, "King Vulture Mask stun value"),
                new OpFloatSlider(vultureKingMaskStun,new Vector2(10f,400f), 200),
                new OpLabel(10f, 370f, "Scav King Mask stun value"),
                new OpFloatSlider(scavKingMaskStun,new Vector2(10f,340f), 200),
                new OpLabel(10f, 305f, "Stun applied to Slugcat when deflecting. \nIf the stun value is 0, the mask won't be dropped on hit. \nIf it's 1.6 or greater, all items will be dropped."),
                new OpLabel(265f, 510f, "Elite Scavenger mask protection. \nHigher value, likelier to deflect a headshot. \nWorks similar to vultures."),
                new OpFloatSlider(eliteResistance, new Vector2(280f, 460f), 60),
                new OpUpdown(vultureMaskFearDuration, new Vector2(10f, 200f), 60f),
                new OpLabel(75f, 205f, "Vulture mask fear duration (seconds)"),
                new OpUpdown(kingVultureMaskFearDuration, new Vector2(10f, 140f), 60f),
                new OpLabel(75f, 145f, "Vulture King/Scav king mask fear duration (seconds)")
            };

            opTab.AddItems(UIArrPlayerOptions);

        }

    }
}