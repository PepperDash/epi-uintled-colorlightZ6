using System.Collections.Generic;
using PepperDash.Core;
using Pepperdash.Essentials.Generic.ColorlightZ6;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PepperDash.Essentials.Generic.ColorlightZ6
{
    public class PluginFactory: EssentialsPluginDeviceFactory<ColorlightZ6Controller>
    {
        public PluginFactory()
        {
            MinimumEssentialsFrameworkVersion = "1.5.5";

            TypeNames = new List<string>{"colorlightz6"};
        }

        #region Overrides of EssentialsDeviceFactory<ColorlightZ6Controller>

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            Debug.Console(0, "Creating Colorlight Z6 controller...");
            var comm = CommFactory.CreateCommForDevice(dc);

            var config = dc.Properties.ToObject<ColorlightZ6Properties>();

            var device = new ColorlightZ6Controller(dc.Key, dc.Name, comm, config);

            return device;
        }

        #endregion
    }
}