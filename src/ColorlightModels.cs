using System.Collections.Generic;

namespace PepperDash.Essentials.Plugins
{
    /// <summary>
    /// Sender model codes from the Class A protocol (Response Frame for Detecting Sender Parameters, byte 12).
    /// </summary>
    public static class ColorlightModels
    {
        private static readonly Dictionary<byte, string> Names = new Dictionary<byte, string>
        {
            { 0x10, "Z4" },
            { 0x11, "Z6" },
            { 0x13, "X2" },
            { 0x14, "X4" },
            { 0x15, "S6F" },
            { 0x16, "S6" },
            { 0x18, "X4e" },
            { 0x1A, "X8" },
            { 0x1B, "X16" },
            { 0x1C, "X16B" },
            { 0x1D, "X16C" },
            { 0x1E, "X1" },
            { 0x1F, "Z4F" },
            { 0x22, "X3" },
            { 0x23, "Z6 PRO" },
            { 0x26, "CM8" },
            { 0x27, "X5" },
            { 0x28, "X6" },
            { 0x29, "X7" },
            { 0x2A, "X2s" },
            { 0x2B, "X4s" },
            { 0x2D, "S10" },
            { 0x31, "X12" },
            { 0x32, "S20" },
            { 0x33, "X20" },
            { 0x35, "S10 PRO" },
            { 0x3B, "S20F" },
            { 0x3D, "S7" },
            { 0x3E, "S10 PRO-U" },
            { 0x40, "S5 PRO" },
            { 0x42, "X16E" },
            { 0x44, "Z6 PRO G2" },
            { 0x45, "X8E" }
        };

        public static string GetName(byte code)
        {
            string name;
            return Names.TryGetValue(code, out name) ? name : string.Format("Unknown (0x{0:X2})", code);
        }
    }
}
