using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugins
{
    public class ColorlightZ6DeviceFactory : EssentialsPluginDeviceFactory<ColorlightZ6Device>
    {
        public ColorlightZ6DeviceFactory()
        {
            MinimumEssentialsFrameworkVersion = "3.0.0";

            TypeNames = new List<string> { "colorlightz6", "colorlightsender" };
        }

        #region Overrides of EssentialsDeviceFactory<ColorlightZ6Device>

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
			Debug.LogDebug("[{Key}] Factory Attempting to create new device from type: {Type}", dc.Key, dc.Type);

            var config = dc.Properties.ToObject<ColorlightZ6PropertiesConfig>();
	        if (config == null)
	        {
				Debug.LogInformation("[{Key}] Factory: failed to read properties config for {Name}", dc.Key, dc.Name);
				return null;
	        }

			var comm = CommFactory.CreateCommForDevice(dc);
	        
			if(comm != null) return new ColorlightZ6Device(dc.Key, dc.Name, comm, config);

			Debug.LogInformation("[{Key}] Factory Notice: No control object present for device {Name}", dc.Key, dc.Name);
			return null;
        }

        #endregion
    }
}