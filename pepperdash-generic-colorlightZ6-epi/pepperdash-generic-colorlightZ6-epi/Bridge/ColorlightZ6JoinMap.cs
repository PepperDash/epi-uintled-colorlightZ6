using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Generic.ColorlightZ6.Bridge
{
    public class ColorlightZ6JoinMap : JoinMapBaseAdvanced
    {
        [JoinName("brightness")] public JoinDataComplete Brightness =
            new JoinDataComplete(new JoinData {JoinNumber = 1, JoinSpan = 1},
                new JoinMetadata
                {
                    Label = "Brightness control",
                    JoinCapabilities = eJoinCapabilities.FromSIMPL,
                    JoinType = eJoinType.Analog
                });

        [JoinName("preset")] public JoinDataComplete Preset =
            new JoinDataComplete(new JoinData {JoinNumber = 2, JoinSpan = 1},
                new JoinMetadata
                {
                    Label = "Preset Recall",
                    JoinCapabilities = eJoinCapabilities.FromSIMPL,
                    JoinType = eJoinType.Analog
                });

        [JoinName("showOn")] public JoinDataComplete ShowOn =
            new JoinDataComplete(new JoinData {JoinNumber = 1, JoinSpan = 1},
                new JoinMetadata
                {
                    Label = "Show On",
                    JoinCapabilities = eJoinCapabilities.FromSIMPL,
                    JoinType = eJoinType.Digital
                });

        [JoinName("showOff")] public JoinDataComplete ShowOff =
            new JoinDataComplete(new JoinData {JoinNumber = 1, JoinSpan = 1},
                new JoinMetadata
                {
                    Label = "Show Off",
                    JoinCapabilities = eJoinCapabilities.FromSIMPL,
                    JoinType = eJoinType.Digital
                });

        public ColorlightZ6JoinMap(uint joinStart) : base(joinStart, typeof (ColorlightZ6JoinMap))
        {
        }

    }
}