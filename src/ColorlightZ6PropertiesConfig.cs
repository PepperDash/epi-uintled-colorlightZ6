using Newtonsoft.Json;
using PepperDash.Core;

namespace PepperDash.Essentials.Plugins.Colorlight.Z6
{
    public class ColorlightZ6Properties
    {
		[JsonProperty("control")]
        public ControlPropertiesConfig Control { get; set; }

		[JsonProperty("id")]
        public ushort Id { get; set; }
    }
}