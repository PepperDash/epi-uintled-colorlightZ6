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
            Brightness = 0;
            Preset = 1;
            ShowOn = 0;
            ShowOff = 1;
        }

        public override void OffsetJoinNumbers(uint joinStart)
        {
            var joinOffset = joinStart + 1;
            var props = GetType().GetCType().GetProperties().Where(o => o.PropertyType == typeof (uint)).ToList();
            foreach (var prop in props)
            {
                prop.SetValue(this, (uint) prop.GetValue(this, null) + joinOffset, null);
            }
        }
    }
}