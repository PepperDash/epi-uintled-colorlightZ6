using Newtonsoft.Json;
using PepperDash.Core;

namespace ColorlightZ6
{
    public class ColorlightZ6Properties
    {
		[JsonProperty("control")]
        public ControlPropertiesConfig Control { get; set; }

		[JsonProperty("id")]
        public ushort Id { get; set; }
    }
}