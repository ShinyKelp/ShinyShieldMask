using BepInEx.Logging;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using UnityEngine;

namespace ShinyShieldMask
{
        public class ShinyShieldMaskOptions : OptionInterface
    {
        private readonly ManualLogSource Logger;

        public ShinyShieldMaskOptions(ShinyShieldMaskMod modInstance, ManualLogSource loggerSource)
        {
            Logger = loggerSource;
            vultureMaskStun = this.config.Bind<float>("vultureMaskStun", 1.8f, new ConfigAcceptableRange<float>(0f, 10f));
            vultureKingMaskStun = this.config.Bind<float>("vultureKingMaskStun", 1f, new ConfigAcceptableRange<float>(0f, 10f));
            scavKingMaskStun = this.config.Bind<float>("kingMaskStun", 0f, new ConfigAcceptableRange<float>(0f, 10f));
            eliteResistance = this.config.Bind<float>("eliteResistance", .3f, new ConfigAcceptableRange<float>(0f, 1f));
            enableShieldMask = this.config.Bind<bool>("enableShieldMask", true);

        }

        public readonly Configurable<float> vultureMaskStun;
        public readonly Configurable<float> vultureKingMaskStun;
        public readonly Configurable<float> scavKingMaskStun;
        public readonly Configurable<float> eliteResistance;
        public readonly Configurable<bool> enableShieldMask;
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
            new OpLabel(10f, 300f, "If the stun value is 0, the mask won't be dropped on hit. If it's 1.8 or greater, all items will be dropped."),
            new OpLabel(250f, 490f, "Elite Scavenger mask protection (none at zero)"),
            new OpFloatSlider(eliteResistance, new Vector2(250f, 460f), 60)
            };

            opTab.AddItems(UIArrPlayerOptions);

        }

        public override void Update()
        {
            
        }

    }
}