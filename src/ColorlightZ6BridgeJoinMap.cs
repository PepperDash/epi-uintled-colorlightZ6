using PepperDash.Essentials.Core;

namespace ColorlightZ6
{
    public class ColorlightZ6JoinMap : JoinMapBaseAdvanced
    {
        [JoinName("Brightness")] public JoinDataComplete Brightness =
            new JoinDataComplete(
				new JoinData
				{
					JoinNumber = 1, 
					JoinSpan = 1
				},
                new JoinMetadata
                {
                    Description = "Brightness control",
                    JoinCapabilities = eJoinCapabilities.FromSIMPL,
                    JoinType = eJoinType.Analog
                });

        [JoinName("Preset")] public JoinDataComplete Preset =
            new JoinDataComplete(
				new JoinData
				{
					JoinNumber = 2, 
					JoinSpan = 1
				},
                new JoinMetadata
                {
                    Description = "Preset Recall",
                    JoinCapabilities = eJoinCapabilities.FromSIMPL,
                    JoinType = eJoinType.Analog
                });

        [JoinName("ShowOn")] public JoinDataComplete ShowOn =
            new JoinDataComplete(
				new JoinData
				{
					JoinNumber = 1, 
					JoinSpan = 1
				},
                new JoinMetadata
                {
                    Description = "Show On",
                    JoinCapabilities = eJoinCapabilities.FromSIMPL,
                    JoinType = eJoinType.Digital
                });

        [JoinName("ShowOff")] public JoinDataComplete ShowOff =
            new JoinDataComplete(
				new JoinData
				{
					JoinNumber = 1, 
					JoinSpan = 1
				},
                new JoinMetadata
                {
                    Description = "Show Off",
                    JoinCapabilities = eJoinCapabilities.FromSIMPL,
                    JoinType = eJoinType.Digital
                });

		[JoinName("Name")]
		public JoinDataComplete DeviceName = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Device Name",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});

        public ColorlightZ6JoinMap(uint joinStart) 
			: base(joinStart, typeof (ColorlightZ6JoinMap))
        {
        }

    }
}