using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugins
{
    public class ColorlightZ6PropertiesConfig
    {
        [JsonProperty("control")]
        public ControlPropertiesConfig Control { get; set; }

        [JsonProperty("id")]
        public ushort Id { get; set; }

        /// <summary>
        /// Optional override of the status poll timing: pollInterval, timeToWarning and timeToError in ms.
        /// Defaults are 30 s, 2 min and 5 min. IsOnline goes low at timeToWarning.
        /// </summary>
        [JsonProperty("communicationMonitorProperties")]
        public CommunicationMonitorConfig CommunicationMonitorProperties { get; set; }

        /// <summary>
        /// Optional brightness in percent (0-100, decimals allowed) to set after PowerOn turns blackout off.
        /// Values outside 0-100 are clamped, with a Warning at startup. If null, PowerOn leaves brightness unchanged.
        /// </summary>
        [JsonProperty("powerOnBrightness")]
        public double? PowerOnBrightness { get; set; }

        /// <summary>
        /// Optional per-input configuration for friendly names and visibility.
        /// </summary>
        [JsonProperty("friendlyNames")]
        public List<ColorlightInputFriendlyName> FriendlyNames { get; set; }

        public ColorlightZ6PropertiesConfig()
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

        /// <summary>
        /// Optional override for which device input to select when this button is pressed.
        /// If null, defaults to using inputNumber as the routed input.
        /// </summary>
        [JsonProperty("inputSelect")]
        public int? InputSelect { get; set; }

        /// <summary>
        /// Optional protocol input code (decimal, 0-255) the sender uses for this input number.
        /// Overrides the built-in default, which follows the Z6 (e.g. 16 = 0x10 HDMI, 80 = 0x50 DVI).
        /// </summary>
        [JsonProperty("inputCode")]
        public byte? InputCode { get; set; }

        /// <summary>
        /// Optional brightness level (0-65535) to apply when this button is pressed.
        /// </summary>
        [JsonProperty("brightness")]
        public ushort? Brightness { get; set; }

        /// <summary>
        /// Optional preset number to recall when this button is pressed.
        /// </summary>
        [JsonProperty("preset")]
        public ushort? Preset { get; set; }
    }
}