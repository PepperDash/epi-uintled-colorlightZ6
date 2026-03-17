using Newtonsoft.Json;
using PepperDash.Core;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugins.Colorlight.Z6
{
    public class ColorlightZ6Properties
    {
        [JsonProperty("control")]
        public ControlPropertiesConfig Control { get; set; }

        [JsonProperty("id")]
        public ushort Id { get; set; }

        /// <summary>
        /// Optional per-input configuration for friendly names and visibility.
        /// </summary>
        [JsonProperty("friendlyNames")]
        public List<ColorlightInputFriendlyName> FriendlyNames { get; set; }

        public ColorlightZ6Properties()
        {
            FriendlyNames = new List<ColorlightInputFriendlyName>();
        }
    }

    public class ColorlightInputFriendlyName
    {
        /// <summary>
        /// 1-based input number (1-7) corresponding to the device input.
        /// </summary>
        [JsonProperty("inputNumber")]
        public int InputNumber { get; set; }

        /// <summary>
        /// Friendly name to appear on the SIMPL bridge.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// When true, the input is hidden from the SIMPL bridge UI (button disabled/blank).
        /// </summary>
        [JsonProperty("hideInput")]
        public bool HideInput { get; set; }
    }
}