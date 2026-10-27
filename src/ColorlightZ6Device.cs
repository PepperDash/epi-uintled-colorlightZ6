using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using System;
using System.Linq;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugins
{
	public class ColorlightZ6Device : EssentialsBridgeableDevice, ICommunicationMonitor, IDisposable
	{
		private CTimer _heartbeatTimer;
		// per manufacturer documentation, heartbeat must be sent every 1-second
		private const long HeartbeatTime = 1000;
		private static readonly byte[] HeartbeatFrame = { 0x99, 0x99, 0x04, 0x00 };
		// matches the Essentials default AutoReconnectIntervalMs
		private const int ReconnectThrottleMs = 5000;
		private DateTime _lastConnectAttempt = DateTime.MinValue;
		private const long DefaultPollTimeMs = 30000;
		private const long DefaultWarningTimeMs = 120000;
		private const long DefaultErrorTimeMs = 300000;
		private readonly ColorlightFrameBuffer _frameBuffer = new ColorlightFrameBuffer();
		private readonly ColorlightStatusMonitor _statusMonitor;
		private readonly ushort _id;
		public IntFeedback InputNumberFeedback;
		public List<BoolFeedback> InputFeedback;
		private List<bool> _inputFeedback;
		private int _inputNumber;
		private bool _powerIsOn;
		private bool _powerIsOff;
		private BasicTriList _trilist;
		private ColorlightZ6BridgeJoinMap _joinMap;
        private readonly object _inputFeedbackLock = new object();
		private readonly ColorlightZ6PropertiesConfig _config;

		public IBasicCommunication Communications { get; private set; }
		public StatusMonitorBase CommunicationMonitor { get; private set; }


		public ColorlightZ6Device(string key, string name, IBasicCommunication comm, ColorlightZ6PropertiesConfig config)
			: base(key, name)
		{
			_config = config;

			Communications = comm;

			var socket = Communications as ISocketStatus;
			if (socket != null)
			{
				socket.ConnectionChange += SocketOnConnectionChange;
			}

			Communications.BytesReceived += CommunicationsOnBytesReceived;

			_id = config.Id;

			// Online status comes from replies to a status poll; timing can be overridden in config
			var monitorConfig = config.CommunicationMonitorProperties;
			var pollTime = monitorConfig != null && monitorConfig.PollInterval > 0 ? monitorConfig.PollInterval : DefaultPollTimeMs;
			var warningTime = monitorConfig != null && monitorConfig.TimeToWarning > 0 ? monitorConfig.TimeToWarning : DefaultWarningTimeMs;
			var errorTime = monitorConfig != null && monitorConfig.TimeToError > 0 ? monitorConfig.TimeToError : DefaultErrorTimeMs;

			_statusMonitor = new ColorlightStatusMonitor(this, pollTime, warningTime, errorTime, StatusSenderId, PollStatus);
			CommunicationMonitor = _statusMonitor;
			CommunicationMonitor.StatusChange += CommunicationMonitor_StatusChage;

			InputNumberFeedback = new IntFeedback(Key + "-InputNumberFeedback", () => InputNumber);

			this.LogInformation("Creating Colorlight Z6 controller with id: {Id}", _id);

			_inputFeedback = new List<bool>();
			InputFeedback = new List<BoolFeedback>();
		}

		protected override void Initialize()
		{
			this.LogInformation("Initialize: Colorlight Z6 with id: {Id}", _id);
			
			Communications.Connect();
			CommunicationMonitor.Start();

			// Per the protocol, the 1 s heartbeat is only needed (and only supported) over TCP
			if (_config.Control != null && _config.Control.Method == eControlMethod.Tcpip)
			{
				StartHeartbeatTimer();
			}

			base.Initialize();
		}	

		/// <summary>
		/// Starts the 1-second heartbeat timer if it is not already running.
		/// Used for all communication types, not just socket-based transports.
		/// </summary>
		private void StartHeartbeatTimer()
		{
			if (_heartbeatTimer != null)
				return;

			_heartbeatTimer = new CTimer(o => SendHeartbeat(), null, 0, HeartbeatTime);
		}

		/// <summary>
		/// Safely stops and disposes the heartbeat timer if it is running.
		/// </summary>
		private void StopHeartbeatTimer()
		{
			if (_heartbeatTimer == null)
				return;

			_heartbeatTimer.Stop();
			_heartbeatTimer.Dispose();
			_heartbeatTimer = null;
		}

		private void CommunicationsOnBytesReceived(object sender, GenericCommMethodReceiveBytesArgs genericCommMethodReceiveBytesArgs)
		{
			var bytes = genericCommMethodReceiveBytesArgs.Bytes;

			if (!IsHeartbeat(bytes))
			{
				this.LogVerbose("CommunicationsOnBytesReceived: {Bytes}", BitConverter.ToString(bytes));
			}

			foreach (var frame in _frameBuffer.Append(bytes))
			{
				if (frame.Type == ColorlightFrameType.StatusReply)
				{
					_statusMonitor.StatusReplyReceived();
				}
				else
				{
					_statusMonitor.DataReceived();
				}
			}
		}

		/// <summary>
		/// Sender the status poll is addressed to. A broadcast (65535) query would get colliding
		/// replies from every sender, so broadcast configs poll the first sender.
		/// </summary>
		private ushort StatusSenderId
		{
			get { return _id == 0xFFFF ? (ushort)0 : _id; }
		}

		/// <summary>
		/// Detecting Sender Parameters (0x01 ... 0x16); the reply drives online status.
		/// </summary>
		private void PollStatus()
		{
			var frame = BuildFrameFor(StatusSenderId, 0x01, 0x16);
			frame[14] = 0x01;
			SendBytes(frame);
		}

		private static bool IsHeartbeat(byte[] bytes)
		{
			return bytes != null && bytes.SequenceEqual(HeartbeatFrame);
		}

		private void SocketOnConnectionChange(object sender, GenericSocketStatusChageEventArgs genericSocketStatusChageEventArgs)
		{
			if (genericSocketStatusChageEventArgs.Client.IsConnected)
			{
				// For socket-based transports, maintain existing behavior on connect
				// (no-op for heartbeat, which now runs continuously once started).
				return;
			}

			// On socket disconnect, drop any partial frame and clear feedback. Heartbeat now runs
			// continuously and is only stopped during device disposal.
			_frameBuffer.Clear();
			ResetFakeFeedback();
		}

		private void CommunicationMonitor_StatusChage(object sender, MonitorStatusChangeEventArgs args)
		{
			// Keep the existing online feedback behavior.
			CommunicationMonitor.IsOnlineFeedback.FireUpdate();
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

		private ColorlightInputFriendlyName GetFriendlyConfigForButton(int inputNumber)
		{
			if (_config == null || _config.FriendlyNames == null)
				return null;

			return _config.FriendlyNames.FirstOrDefault(f => f.InputNumber == inputNumber);
		}

		private bool IsInputHidden(int inputNumber)
		{
			var friendly = GetFriendlyConfigForButton(inputNumber);
			return friendly != null && friendly.HideInput;
		}

		private string GetInputFriendlyName(int inputNumber, string defaultName)
		{
			var friendly = GetFriendlyConfigForButton(inputNumber);
			if (friendly == null || string.IsNullOrEmpty(friendly.Name) || friendly.HideInput)
				return friendly != null && friendly.HideInput ? string.Empty : defaultName;

			return friendly.Name;
		}

		private void ExecuteButtonAction(ushort buttonIndex)
		{
			// buttonIndex is 1-7 corresponding to joins 11-17
			var friendly = GetFriendlyConfigForButton(buttonIndex);
			var anyAction = false;

			// If we have a config entry, evaluate optional actions
			if (friendly != null)
			{
				// Optional input selection override
				if (friendly.InputSelect.HasValue)
				{
					var routeInput = friendly.InputSelect.Value;
					if (routeInput >= 1 && routeInput <= 7)
					{
						SelectInput((ushort)routeInput);
						anyAction = true;
					}
					else
					{
						this.LogWarning("ExecuteButtonAction: inputSelect {RouteInput} out of range (1-7) for button {ButtonIndex}", routeInput, buttonIndex);
					}
				}

				// Optional brightness
				if (friendly.Brightness.HasValue)
				{
					SetBrightness(friendly.Brightness.Value);
					anyAction = true;
				}

				// Optional preset recall
				if (friendly.Preset.HasValue)
				{
					if (friendly.Preset.Value > 0)
					{
						RecallPreset(friendly.Preset.Value);
						anyAction = true;
					}
					else
					{
						this.LogWarning("ExecuteButtonAction: preset value {Preset} is out of range (>0) for button {ButtonIndex}", friendly.Preset.Value, buttonIndex);
					}
				}
			}

			// Backward-compatible default: if no config or no explicit actions, treat as input select for this index
			if (!anyAction)
			{
				SelectInput(buttonIndex);
			}
		}

				/// <summary>
			/// Updates digital input-select feedback for SIMPL bridge.
			/// Ensures only the currently selected visible input join is high.
			/// Hidden inputs never drive feedback high.
			/// </summary>
			/// <param name="data">Currently routed source (1-7), or 0 for none.</param>
			private void UpdateBooleanFeedback(int data)
			{
				if (_trilist == null || _joinMap == null)
				{
					return;
				}

				lock (_inputFeedbackLock)
				{
					// Clear existing state
					_inputFeedback.Clear();

					// We support up to 7 inputs on digital joins 11-17
					const int maxInputs = 7;

					for (var i = 0; i < maxInputs; i++)
					{
						var inputNumber = i + 1;
						var isVisible = !IsInputHidden(inputNumber);
						var isActive = isVisible && (data - 1) == i && data >= 1 && data <= maxInputs;
						_inputFeedback.Add(isActive);

						var join = (uint)(_joinMap.InputSelectOffset.JoinNumber + i);
						_trilist.BooleanInput[join].BoolValue = isActive;
					}

					// Fire any external BoolFeedbacks that depend on this list
					foreach (var item in InputFeedback)
					{
						item.FireUpdate();
					}
				}
			}
		
		public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
		{
			var joinMap = new ColorlightZ6BridgeJoinMap(joinStart);
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

			this.LogInformation("Linking to bridge {TrilistId:X} at joinStart {JoinStart}", trilist.ID, joinStart);

			trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

			if (CommunicationMonitor != null)
			{
				CommunicationMonitor.IsOnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
			}

			trilist.SetSigTrueAction(joinMap.PowerOn.JoinNumber, PowerOn);
			trilist.SetSigTrueAction(joinMap.PowerOff.JoinNumber, PowerOff);
			trilist.SetUShortSigAction(joinMap.Preset.JoinNumber, RecallPreset); 
			trilist.SetUShortSigAction(joinMap.Brightness.JoinNumber, SetBrightness);

			// digital input-select buttons (11-17) mapped to logical buttons 1-7,
			// honoring any hideInput settings and executing configured actions
			const int maxInputs = 7;
			for (var i = 0; i < maxInputs; i++)
			{
				var buttonIndex = (ushort)(i + 1);
				var joinNumber = (uint)(joinMap.InputSelectOffset.JoinNumber + i);

				if (IsInputHidden(buttonIndex))
					continue;

				var localButtonIndex = buttonIndex; // avoid modified-closure issue
				trilist.SetSigTrueAction(joinNumber, () => ExecuteButtonAction(localButtonIndex));
			}

			// populate input names on InputNamesOffset serial joins,
			// applying friendlyNames and hideInput configuration
			var defaultInputNames = new[] { "HDMI", "DVI", "DVI-2", "DVI-3", "DVI-4", "SDI", "SDI-2" };
			for (var i = 0; i < defaultInputNames.Length; i++)
			{
				var inputNumber = i + 1;
				var defaultName = defaultInputNames[i];
				var friendlyName = GetInputFriendlyName(inputNumber, defaultName);

				trilist.SetString((uint)(_joinMap.InputNamesOffset.JoinNumber + i), friendlyName);
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

				UpdateBooleanFeedback(InputNumber);

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
			UpdateBooleanFeedback(0);
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

			// Setting Input Video Source (0x33): layer 0 (main), then the input code.
			// The switch only takes effect after a Batch Operation (0x6f) frame.
			SendBytes(BuildFrame(0x33, 0x00, inputCode));
			SendBytes(BuildFrame(0x6F));
		}

		/// <summary>
		/// Builds a Class A protocol frame: command byte, total frame length (little-endian),
		/// sender id (little-endian), 0xFF, zero padding to 16 bytes, then the payload.
		/// </summary>
		private byte[] BuildFrame(byte command, params byte[] payload)
		{
			return BuildFrameFor(_id, command, payload);
		}

		private static byte[] BuildFrameFor(ushort senderId, byte command, params byte[] payload)
		{
			var length = 16 + payload.Length;
			var frame = new byte[length];

			frame[0] = command;
			frame[2] = (byte)(length & 0xFF);
			frame[3] = (byte)(length >> 8);
			frame[6] = (byte)(senderId & 0xFF);
			frame[7] = (byte)(senderId >> 8);
			frame[8] = 0xFF;
			Array.Copy(payload, 0, frame, 16, payload.Length);

			return frame;
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
				TryReconnect();
			}

			if (!IsHeartbeat(command))
			{
				this.LogVerbose("SendBytes: {Command}", BitConverter.ToString(command));
			}

			Communications.SendBytes(command);
		}

		/// <summary>
		/// Attempts a reconnect at most once per ReconnectThrottleMs, and only when the
		/// client is not already reconnecting on its own (autoReconnect in config).
		/// </summary>
		private void TryReconnect()
		{
			var autoReconnect = Communications as IAutoReconnect;
			if (autoReconnect != null && autoReconnect.AutoReconnect)
				return;

			if ((DateTime.UtcNow - _lastConnectAttempt).TotalMilliseconds < ReconnectThrottleMs)
				return;

			_lastConnectAttempt = DateTime.UtcNow;
			this.LogVerbose("SendBytes: communications not connected, attempting connection...");
			Communications.Connect();
		}

		private void SendHeartbeat()
		{
			SendBytes(HeartbeatFrame);
		}

		public void SetBrightness(ushort brightness)
		{
			// Scale Crestron ushort 0-65535 to a float 0.00-1.00
			// Round to 2 decimal places so key values map as:
			//  65535 -> 1.00f, 49149 -> 0.75f, 32767 -> 0.50f, 16383 -> 0.25f
			var brightnessPercent = (float)Math.Round(brightness / 65535.0f, 2);

			this.LogVerbose("SetBrightness: Level {Level} Percent {Percent}", brightness, brightnessPercent * 100);

			var brightnessBytes = BitConverter.GetBytes(brightnessPercent);

			SendBytes(BuildFrame(0x21, brightnessBytes));
		}

		public void RecallPreset(ushort preset)
		{
			// 0 means "no preset" (SIMPL analogs start at and are often cleared to 0); ignore it silently
			if (preset == 0)
				return;

			// The protocol supports presets 1-16
			if (preset > 16)
			{
				this.LogWarning("RecallPreset: preset {Preset} is out of range (1-16), ignored", preset);
				return;
			}

			// per manufacturer documentation, preset value is 0-based, but SIMPL uses 1-based
			SendBytes(BuildFrame(0x74, (byte)(preset - 1)));
		}

		/// <summary>
		/// PowerOn command triggers `Show On` within manufacturers API
		/// </summary>
		public void PowerOn()
		{
			_powerIsOn = true;
			_powerIsOff = false;
			UpdatePowerFeedback();

			// Blackout (0x11): 0x01 disables blackout (show on)
			SendBytes(BuildFrame(0x11, 0x01));
		}

		/// <summary>
		/// PowerOff command triggers `Show Off` within manufacturers API
		/// </summary>
		public void PowerOff()
		{
			_powerIsOn = false;
			_powerIsOff = true;
			UpdatePowerFeedback();

			// Blackout (0x11): 0x00 enables blackout (show off)
			SendBytes(BuildFrame(0x11, 0x00));
		}

		/// <summary>
		/// Ensures heartbeat timer is stopped and disposed when the device
		/// itself is disposed (if the host calls this method).
		/// Once started, the heartbeat runs continuously for the lifetime
		/// of the device.
		/// </summary>
		public void Dispose()
		{
			StopHeartbeatTimer();
			_statusMonitor.Stop();
		}
	}
}

