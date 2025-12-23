using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugins.Colorlight.Z6
{
    public class PluginFactory: EssentialsPluginDeviceFactory<ColorlightZ6Controller>
    {
        public PluginFactory()
        {
            MinimumEssentialsFrameworkVersion = "2.24.2";

            TypeNames = new List<string>{"colorlightz6"};
        }

        #region Overrides of EssentialsDeviceFactory<ColorlightZ6Controller>

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
			Debug.LogDebug("[{0}] Factory Attempting to create new device from type: {1}", dc.Key, dc.Type);

            var config = dc.Properties.ToObject<ColorlightZ6Properties>();
	        if (config == null)
	        {
				Debug.LogInformation("[{0}] Factory: failed to read properties config for {1}", dc.Key, dc.Name);
				return null;
	        }

			var comm = CommFactory.CreateCommForDevice(dc);
	        
			if(comm != null) return new ColorlightZ6Controller(dc.Key, dc.Name, comm, config);

			Debug.LogInformation("[{0}] Factory Notice: No control object present for device {1}", dc.Key, dc.Name);
			return null;
        }

        #endregion
    }
}