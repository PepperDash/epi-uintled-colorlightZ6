using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;                          				// For Basic SIMPL# Classes
using Crestron.SimplSharpPro; // For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Bridges;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.Devices;
using pepperdash_generic_colorlightZ6_epi;
using pepperdash_generic_colorlightZ6_epi.Bridge;

namespace Pepperdash.Essentials.ColorlightZ6
{
    public class ColorlightZ6Controller:ReconfigurableDevice, IBridge
    {
        private CTimer _heartbeatTimer;
        private const long HeartbeatTime = 1000;
        private readonly ushort _id;

        private readonly ColorlightZ6JoinMap _joinMap = new ColorlightZ6JoinMap();

        public IBasicCommunication Communications { get; private set; }

        public static void LoadPlugin()
        {
            Debug.Console(0, "Loading Colorlight Z6 plugin...");
            PepperDash.Essentials.Core.DeviceFactory.AddFactoryForType("colorlightz6", BuildDevice);
        }

        public static ColorlightZ6Controller BuildDevice(DeviceConfig dc)
        {
            Debug.Console(0, "Creating Colorlight Z6 controller...");
            var comm = CommFactory.CreateCommForDevice(dc);
            var device = new ColorlightZ6Controller(comm, dc);

            return device;
        }

        public ColorlightZ6Controller(IBasicCommunication comm, DeviceConfig dc):base(dc)
        {
            
            Communications = comm;

            var socket = Communications as ISocketStatus;

            if (socket != null)
            {
                socket.ConnectionChange += SocketOnConnectionChange;
            }

            Communications.BytesReceived += CommunicationsOnBytesReceived;

            var props = JsonConvert.DeserializeObject<ColorlightZ6Properties>(dc.Properties.ToString());

            _id = props.Id;

            Debug.Console(0,this, "Creating Colorlight Z6 controller with id {0}", _id);
        }

        private void CommunicationsOnBytesReceived(object sender, GenericCommMethodReceiveBytesArgs genericCommMethodReceiveBytesArgs)
        {
            Debug.Console(0,this,"Device Response: {0}", BitConverter.ToString(genericCommMethodReceiveBytesArgs.Bytes));  
        }

        public override bool CustomActivate()
        {
            Debug.Console(0, this, "Activating Colorlight Z6 {0}", _id);
            Communications.Connect();
            return true;
        }

        private void SocketOnConnectionChange(object sender, GenericSocketStatusChageEventArgs genericSocketStatusChageEventArgs)
        {
            if (genericSocketStatusChageEventArgs.Client.IsConnected)
            {
                if (_heartbeatTimer == null)
                {
                    _heartbeatTimer = new CTimer(SendHeartbeat,null, 0, HeartbeatTime);
                }

                return;
            }

            _heartbeatTimer.Stop();
            _heartbeatTimer.Dispose();
            _heartbeatTimer = null;
        }

        private void SendHeartbeat(object o)
        {
            Communications.SendBytes(new byte[]{0x99, 0x99, 0x04, 0x00});
        }

        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey)
        {
            Debug.Console(0, this, "Connecting to SIMPL Bridge with joinStart {0}", joinStart);

            _joinMap.OffsetJoinNumbers(joinStart);

            Debug.Console(0, this, "Mapping SetBrightness to join {0}", _joinMap.Brightness);
            trilist.SetUShortSigAction(_joinMap.Brightness, SetBrightness);

            Debug.Console(0, this, "Mapping RecallPreset to join {0}", _joinMap.Preset);
            trilist.SetUShortSigAction(_joinMap.Preset, RecallPreset);

            Debug.Console(0, this, "Mapping SetShowOn to join {0}", _joinMap.ShowOn);
            trilist.SetBoolSigAction(_joinMap.ShowOn, SetShowOn);

            Debug.Console(0, this, "Mapping SetShowOff to join {0}", _joinMap.ShowOff);
            trilist.SetBoolSigAction(_joinMap.ShowOff, SetShowOff);
        }

        public void SetBrightness(ushort brightness)
        {
            var brightnessPercent = brightness/(float) 65535.0;

            Debug.Console(0,this,"Brightness Level {0} Percent {1}", brightness, brightnessPercent * 100);

            var brightnessBytes = BitConverter.GetBytes(brightnessPercent);

            var commandBase = new byte[]
            {
                0x21, 0x00, 0x14, 0x00, 0x00, 0x00, (byte) (_id >> 8), (byte) (_id & 0xFF), 0xFF, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00
            };

            var command = commandBase.Concat(brightnessBytes).ToArray();

            Debug.Console(0, this, "Brightness Command {0}", BitConverter.ToString(command));

            Communications.SendBytes(command);
        }

        public void RecallPreset(ushort preset)
        {
            var command = new byte[]
            {
                0x74, 0x00, 0x11, 0x00, 0x00, 0x00, (byte) (_id >> 8), (byte) (_id & 0xFF), 0xFF, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, (byte) preset
            };

            Debug.Console(0, this, "Preset Command {0}", BitConverter.ToString(command));
            Communications.SendBytes(command);
        }

        public void SetShowOn(bool notUsed)
        {
            var command = new byte[]
            {
                0x11, 0x00, 0x11, 0x00, 0x00, 0x00, (byte) (_id >> 8), (byte) (_id & 0xFF), 0xFF, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x01
            };

            Communications.SendBytes(command);
        }

        public void SetShowOff(bool notUsed)
        {
            var command = new byte[]
            {
                0x11, 0x00, 0x11, 0x00, 0x00, 0x00, (byte) (_id >> 8), (byte) (_id & 0xFF), 0xFF, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00
            };

            Communications.SendBytes(command);
        }
    }
}

