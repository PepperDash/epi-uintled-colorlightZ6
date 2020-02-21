using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Reflection;
using PepperDash.Essentials.Core;

namespace pepperdash_generic_colorlightZ6_epi.Bridge
{
    public class ColorlightZ6JoinMap:JoinMapBase
    {
        public uint Brightness { get; set; }
        public uint Preset { get; set; }
        public uint ShowOn { get; set; }
        public uint ShowOff { get; set; }

        public ColorlightZ6JoinMap()
        {
            Brightness = 1;
            Preset = 2;
            ShowOn = 1;
            ShowOff = 2;
        }

        public override void OffsetJoinNumbers(uint joinStart)
        {
            var joinOffset = joinStart - 1;

            Brightness = Brightness + joinOffset;
            Preset = Preset + joinOffset;
            ShowOn = ShowOn + joinOffset;
            ShowOff = ShowOff + joinOffset;
        }

    }
}