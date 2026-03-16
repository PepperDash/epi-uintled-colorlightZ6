using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using System;
using System.Linq;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugins.Colorlight.Z6
{
	public class ColorlightZ6Controller : EssentialsBridgeableDevice, ICommunicationMonitor
	{
		private Thread _queueProcess;
		private readonly CrestronQueue<byte[]> _myQueue = new CrestronQueue<byte[]>(100);
		private CTimer _heartbeatTimer;
		// per manufacturer documentation, heartbeat must be sent every 1-second
		private const long HeartbeatTime = 1000;
		private readonly ushort _id;
		public IntFeedback InputNumberFeedback;
		public List<BoolFeedback> InputFeedback;
		private List<bool> _inputFeedback;
		private int _inputNumber;
		private bool _powerIsOn;
		private bool _powerIsOff;
		private BasicTriList _trilist;
		private ColorlightZ6JoinMap _joinMap;

		public IBasicCommunication Communications { get; private set; }
		public StatusMonitorBase CommunicationMonitor { get; private set; }


		public ColorlightZ6Controller(string key, string name, IBasicCommunication comm, ColorlightZ6Properties config)
			: base(key, name)
		{
			Communications = comm;

			var socket = Communications as ISocketStatus;
			if (socket != null)
			{
				socket.ConnectionChange += SocketOnConnectionChange;
			}

			Communications.BytesReceived += CommunicationsOnBytesReceived;
			// Heartbeat every 2 minutes, retry connection every 3 minutes, mark offline after 5 minutes without successful communication
			CommunicationMonitor = new GenericCommunicationMonitor(this, Communications, 120000, 180000, 300000, SendHeartbeat);
			CommunicationMonitor.StatusChange += CommunicationMonitor_StatusChage;

			InputNumberFeedback = new IntFeedback(() =>
            {
                return InputNumber;
            });

			_id = config.Id;

			this.LogInformation($"Creating Colorlight Z6 controller with id {_id}");

			_inputFeedback = new List<bool>();
            InputFeedback = new List<BoolFeedback>();
		}

		public override void Initialize()
		{
			this.LogInformation($"Initialize: Colorlight Z6 {_id}");
			
			Communications.Connect();
			CommunicationMonitor.Start();

			base.Initialize();
		}	

		private void CommunicationsOnBytesReceived(object sender, GenericCommMethodReceiveBytesArgs genericCommMethodReceiveBytesArgs)
		{
			this.LogVerbose($"CommunicationsOnBytesReceived: {BitConverter.ToString(genericCommMethodReceiveBytesArgs.Bytes)}");

			_myQueue.Enqueue(genericCommMethodReceiveBytesArgs.Bytes);

			if (_queueProcess == null || _queueProcess.ThreadState == Thread.eThreadStates.ThreadFinished) return;

			_queueProcess = new Thread(ProcessQueue, null);
		}

		private void SocketOnConnectionChange(object sender, GenericSocketStatusChageEventArgs genericSocketStatusChageEventArgs)
		{
			if (genericSocketStatusChageEventArgs.Client.IsConnected)
			{
				if (_heartbeatTimer == null)
				{
					_heartbeatTimer = new CTimer(o => SendHeartbeat(), null, 0, HeartbeatTime);
				}

				return;
			}

			_heartbeatTimer.Stop();
			_heartbeatTimer.Dispose();
			_heartbeatTimer = null;
			ResetFakeFeedback();
		}

		private void CommunicationMonitor_StatusChage(object sender, MonitorStatusChangeEventArgs args)
		{
			CommunicationMonitor.IsOnlineFeedback.FireUpdate();
		}

		private object ProcessQueue(object obj)
		{
			while (!_myQueue.IsEmpty)
			{
				var myResponse = _myQueue.Dequeue();

				this.LogVerbose($"ProcessQueue: {myResponse}");
			}
			return null;
		}	

		public int InputNumber
        {
            get { return _inputNumber; }
            private set
            {
                if (_inputNumber == value) return;

                _inputNumber = value;
                InputNumberFeedback.FireUpdate();
                UpdateBooleanFeedback(value);
            }
        }

		        /// <summary>
        /// Updates Digital Route Feedback for Simpl EISC
        /// </summary>
        /// <param name="data">currently routed source</param>
        private void UpdateBooleanFeedback(int data)
        {
            try
            {
                if (data < 0 || data >= _inputFeedback.Count)
                {
                    Debug.LogVerbose(this, "Input index {0} out of range for _inputFeedback (size {1})", data, _inputFeedback.Count);
                    return;
                }

                if (_inputFeedback[data])
                {
                    return;
                }

                _inputFeedback[data] = true;
                foreach (var item in InputFeedback)
                {
                    var update = item;
                    update.FireUpdate();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(this, "{0}", e.Message);
            }
        }
		
		public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
		{
			this.LogInformation($"Connecting to SIMPL Bridge with joinStart {joinStart}");

			var joinMap = new ColorlightZ6JoinMap(joinStart);
			_trilist = trilist;
			_joinMap = joinMap;

			if (bridge != null)
			{
				bridge.AddJoinMap(Key, joinMap);
			}

			var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

			if (customJoins != null)
			{
				joinMap.SetCustomJoinData(customJoins);
			}

			this.LogWarning($"Linking to Trilist '{trilist.ID.ToString("X")}'");
			this.LogInformation($"Linking to Bridge Type {GetType().Name}");

			trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

			if (CommunicationMonitor != null)
			{
				CommunicationMonitor.IsOnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
			}

			trilist.SetSigTrueAction(joinMap.PowerOn.JoinNumber, PowerOn);
			trilist.SetSigTrueAction(joinMap.PowerOff.JoinNumber, PowerOff);
			trilist.SetUShortSigAction(joinMap.Preset.JoinNumber, RecallPreset); 
			trilist.SetUShortSigAction(joinMap.Brightness.JoinNumber, SetBrightness);

			// populate input names on InputNamesOffset serial joins
			var inputNames = new[] { "HDMI", "DVI", "DVI-2", "DVI-3", "DVI-4", "SDI", "SDI-2" };
			for (var i = 0; i < inputNames.Length; i++)
			{
				trilist.SetString((uint)(_joinMap.InputNamesOffset.JoinNumber + i), inputNames[i]);
			}

			// input (analog select)
			trilist.SetUShortSigAction(joinMap.InputSelect.JoinNumber, SelectInput);

			// input (analog feedback)
			if (InputNumberFeedback != null)
				InputNumberFeedback.LinkInputSig(trilist.UShortInput[joinMap.InputSelect.JoinNumber]);

			trilist.OnlineStatusChange += (o, a) =>
			{
				if (!a.DeviceOnLine) return;

				trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

                if (InputNumberFeedback != null)
                    InputNumberFeedback.FireUpdate();

				UpdatePowerFeedback();

			};
		}

		private void UpdatePowerFeedback()
		{
			if (_trilist == null || _joinMap == null) return;

			// Enforce invariant via the shared command/feedback joins:
			//  - PowerOn/PowerIsOn mapped to join 2
			//  - PowerOff/PowerIsOff mapped to join 1
			_trilist.BooleanInput[_joinMap.PowerOn.JoinNumber].BoolValue = _powerIsOn;
			_trilist.BooleanInput[_joinMap.PowerOff.JoinNumber].BoolValue = _powerIsOff;
		}

		private void ResetFakeFeedback()
		{
			// When device goes offline, clear fake power and input state
			_powerIsOn = false;
			_powerIsOff = false;
			InputNumber = 0;
			UpdatePowerFeedback();
		}

		private void SelectInput(ushort input)
		{
			// Map Crestron analog (1-7) to device-specific input codes
			// 1: HDMI   -> 0x10
			// 2: DVI    -> 0x01
			// 3: DVI-2  -> 0x02
			// 4: DVI-3  -> 0x03
			// 5: DVI-4  -> 0x04
			// 6: SDI    -> 0x20
			// 7: SDI-2  -> 0x21
			if (input < 1 || input > 7)
			{
				InputNumber = 0;
				return;
			}

			InputNumber = input;

			byte inputCode;
			switch (input)
			{
				case 1:
					inputCode = 0x10;
					break;
				case 2:
					inputCode = 0x01;
					break;
				case 3:
					inputCode = 0x02;
					break;
				case 4:
					inputCode = 0x03;
					break;
				case 5:
					inputCode = 0x04;
					break;
				case 6:
					inputCode = 0x20;
					break;
				case 7:
					inputCode = 0x21;
					break;
				default:
					inputCode = 0x00;
					break;
			}

			var command = new byte[]
			{
				0x33, 0x00, 0x12, 0x00, 0x00, 0x00,
				(byte)(_id >> 8), (byte)(_id & 0xFF), 0xFF,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00,
				inputCode
			};

			SendBytes(command);
		}

		public void SendBytes(byte[] command)
		{
			if (command == null)
			{
				this.LogVerbose("SendBytes: command bytes are null");
				return;
			}

			if (!Communications.IsConnected)
			{
				this.LogVerbose("SendBytes: communications not connected, attempting connection...");
				Communications.Connect();
			}

			this.LogVerbose($"SendBytes: {BitConverter.ToString(command)}");
			Communications.SendBytes(command);
		}

		private void SendHeartbeat()
		{
			//var command = new byte[] { 0x99, 0x99, 0x04, 0x00 };
			var command = new byte[] { 0x99, 0x99, 0x04, 0x00 };

			SendBytes(command);
		}

		public void SetBrightness(ushort brightness)
		{
			// Scale Crestron ushort 0-65535 to a float 0.00-1.00
			// Round to 2 decimal places so key values map as:
			//  65535 -> 1.00f, 49149 -> 0.75f, 32767 -> 0.50f, 16383 -> 0.25f
			var brightnessPercent = (float)Math.Round(brightness / 65535.0f, 2);

			this.LogVerbose($"SetBrightness: Level {brightness} Percent {brightnessPercent * 100}");

			var brightnessBytes = BitConverter.GetBytes(brightnessPercent);

			var commandBase = new byte[]
            {
                0x21, 0x00, 0x14, 0x00, 0x00, 0x00, (byte) (_id >> 8), (byte) (_id & 0xFF), 0xFF, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00
            };

			var command = commandBase.Concat(brightnessBytes).ToArray();

			SendBytes(command);
		}

		public void RecallPreset(ushort preset)
		{
			var command = new byte[]
            {
                0x74, 0x00, 0x11, 0x00, 0x00, 0x00, (byte) (_id >> 8), (byte) (_id & 0xFF), 0xFF, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, (byte)(preset - 1) // per manufacturer documentation, preset value is 0-based, but SIMPL uses 1-based
            };
			
			SendBytes(command);
		}

		/// <summary>
		/// PowerOn command triggers `Show On` within manufacturers API
		/// </summary>
		public void PowerOn()
		{
			_powerIsOn = true;
			_powerIsOff = false;
			UpdatePowerFeedback();

			var command = new byte[]
            {
                0x11, 0x00, 0x11, 0x00, 0x00, 0x00, (byte) (_id >> 8), (byte) (_id & 0xFF), 0xFF, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x01
            };

			SendBytes(command);
		}

		/// <summary>
		/// PowerOff command triggers `Show Off` within manufacturers API
		/// </summary>
		public void PowerOff()
		{
			_powerIsOn = false;
			_powerIsOff = true;
			UpdatePowerFeedback();

			var command = new byte[]
            {
                0x11, 0x00, 0x11, 0x00, 0x00, 0x00, (byte) (_id >> 8), (byte) (_id & 0xFF), 0xFF, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00
            };

			SendBytes(command);
		}
	}
}

