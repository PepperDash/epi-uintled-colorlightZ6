using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugins.Colorlight.Z6
{
    public class ColorlightZ6JoinMap : JoinMapBaseAdvanced
	{
        #region Digital Joins

        // PowerOff triggers `Show Off` within manufacturers API
        [JoinName("PowerOff")]
        public JoinDataComplete PowerOff =
            new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Power Off / Power Is Off",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        // PowerOn triggers `Show On` within manufacturers API
        [JoinName("PowerOn")]
        public JoinDataComplete PowerOn =
            new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Power On / Power Is On",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("InputSelectOffset")]
        public JoinDataComplete InputSelectOffset = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 11,
                JoinSpan = 7
            },
            new JoinMetadata
            {
                Description = "Input Select",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });    

		[JoinName("IsOnline")]
		public JoinDataComplete IsOnline = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 50,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Is Online",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Digital
			});

        //[JoinName("IsTwoWayDisplay")] - This device is NOT a two-way display, so do not uncomment these lines
        //public JoinDataComplete IsTwoWayDisplay = new JoinDataComplete(
        //    new JoinData
        //    {
        //        JoinNumber = 3,
        //        JoinSpan = 1
        //    },
        //    new JoinMetadata
        //    {
        //        Description = "Is Two Way Display",
        //        JoinCapabilities = eJoinCapabilities.ToSIMPL,
        //        JoinType = eJoinType.Digital
        //    });

        // [JoinName("InputSelectOffset")]
        // public JoinDataComplete InputSelectOffset = new JoinDataComplete(
        //     new JoinData
        //     {
        //         JoinNumber = 11,
        //         JoinSpan = 10
        //     },
        //     new JoinMetadata
        //     {
        //         Description = "Input Select",
        //         JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
        //         JoinType = eJoinType.Digital
        //     });

        //[JoinName("ButtonVisibilityOffset")]
        //public JoinDataComplete ButtonVisibilityOffset = new JoinDataComplete(
        //    new JoinData
        //    {
        //        JoinNumber = 41,
        //        JoinSpan = 10
        //    },
        //    new JoinMetadata
        //    {
        //        Description = "Button Visibility Offset",
        //        JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
        //        JoinType = eJoinType.DigitalSerial
        //    });		
		
		#endregion


		#region Analog Joins

        [JoinName("InputSelect")] 
        public JoinDataComplete InputSelect =
            new JoinDataComplete(
                new JoinData
                {
                    JoinNumber = 11,
                    JoinSpan = 1
                },
                new JoinMetadata
                {
                    Description = "Input Select (command and feedback)",
                    JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                    JoinType = eJoinType.Analog
                });

		[JoinName("Brightness")] 
		public JoinDataComplete Brightness =
            new JoinDataComplete(
				new JoinData
				{
					JoinNumber = 33, 
					JoinSpan = 1
				},
                new JoinMetadata
                {
                    Description = "Brightness control",
                    JoinCapabilities = eJoinCapabilities.FromSIMPL,
                    JoinType = eJoinType.Analog
                });

        [JoinName("Preset")] 
        public JoinDataComplete Preset =
            new JoinDataComplete(
				new JoinData
				{
					JoinNumber = 21, 
					JoinSpan = 1
				},
                new JoinMetadata
                {
                    Description = "Preset Recall",
                    JoinCapabilities = eJoinCapabilities.FromSIMPL,
                    JoinType = eJoinType.Analog
                });

		#endregion


		#region Serial joins

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

        [JoinName("InputNamesOffset")]
        public JoinDataComplete InputNamesOffset = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 11,
                JoinSpan = 10
            },
            new JoinMetadata
            {
                Description = "Input Names Offset",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });		

		#endregion

		public ColorlightZ6JoinMap(uint joinStart) 
			: base(joinStart, typeof (ColorlightZ6JoinMap))
        {
        }

    }
}