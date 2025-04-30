using System;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;

namespace ColorlightZ6
{
	public class ColorlightZ6Controller : EssentialsBridgeableDevice, ICommunicationMonitor
	{
		private Thread _queueProcess;
		private readonly CrestronQueue<byte[]> _myQueue = new CrestronQueue<byte[]>(100);
		//private CTimer _heartbeatTimer;
		private const long HeartbeatTime = 30000;
		private readonly ushort _id;

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
			CommunicationMonitor = new GenericCommunicationMonitor(this, Communications, HeartbeatTime, 180000, 300000, SendHeartbeat);
			CommunicationMonitor.StatusChange += CommunicationMonitor_StatusChage;

			_id = config.Id;

			Debug.Console(0, this, "Creating Colorlight Z6 controller with id {0}", _id);
		}

		public override void Initialize()
		{
			Debug.Console(1, this, "Activating Colorlight Z6 {0}", _id);
			
			Communications.Connect();
			CommunicationMonitor.Start();

			base.Initialize();
		}	

		private void CommunicationsOnBytesReceived(object sender, GenericCommMethodReceiveBytesArgs genericCommMethodReceiveBytesArgs)
		{
			Debug.Console(0, this, "Device Response: {0}", BitConverter.ToString(genericCommMethodReceiveBytesArgs.Bytes));

			_myQueue.Enqueue(genericCommMethodReceiveBytesArgs.Bytes);

			if (_queueProcess == null || _queueProcess.ThreadState == Thread.eThreadStates.ThreadFinished) return;

			_queueProcess = new Thread(ProcessQueue, null);
		}

		private void SocketOnConnectionChange(object sender, GenericSocketStatusChageEventArgs genericSocketStatusChageEventArgs)
		{
			//if (genericSocketStatusChageEventArgs.Client.IsConnected)
			//{
			//    if (_heartbeatTimer == null)
			//    {
			//        _heartbeatTimer = new CTimer(o => SendHeartbeat(), null, 0, HeartbeatTime);
			//    }

			//    return;
			//}

			//_heartbeatTimer.Stop();
			//_heartbeatTimer.Dispose();
			//_heartbeatTimer = null;
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

				Debug.Console(2, this, "response: {0}", myResponse);
			}
			return null;
		}	
		
		public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
		{
			Debug.Console(0, this, "Connecting to SIMPL Bridge with joinStart {0}", joinStart);

			var joinMap = new ColorlightZ6JoinMap(joinStart);

			if (bridge != null)
			{
				bridge.AddJoinMap(Key, joinMap);
			}

			var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

			if (customJoins != null)
			{
				joinMap.SetCustomJoinData(customJoins);
			}

			Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
			Debug.Console(0, "Linking to Bridge Type {0}", GetType().Name);

			trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

			if (CommunicationMonitor != null)
			{
				CommunicationMonitor.IsOnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
			}

			trilist.SetSigTrueAction(joinMap.ShowOn.JoinNumber, SetShowOn);
			trilist.SetSigTrueAction(joinMap.ShowOff.JoinNumber, SetShowOff);
			trilist.SetUShortSigAction(joinMap.Preset.JoinNumber, RecallPreset); 
			trilist.SetUShortSigAction(joinMap.Brightness.JoinNumber, SetBrightness);

			trilist.OnlineStatusChange += (o, a) =>
			{
				if (!a.DeviceOnLine) return;

				trilist.SetString(joinMap.DeviceName.JoinNumber, Name);


			};
		}

		public void SendBytes(byte[] bytes)
		{
			if (bytes == null)
			{
				Debug.Console(2, this, "SendBytes: command bytes are null" );
				return;
			}

			if (!Communications.IsConnected)
			{
				Debug.Console(2, this, "SendBytes: communications not connected, attempting connection...");
				Communications.Connect();
			}

			Communications.SendBytes(bytes);
		}

		private void SendHeartbeat()
		{
			var command = new byte[] { 0x99, 0x99, 0x04, 0x00 };
			SendBytes(command);
		}

		public void SetBrightness(ushort brightness)
		{
			var brightnessPercent = (float)Math.Round(brightness / 65535.0f, 1);

			Debug.Console(1, this, "Brightness Level {0} Percent {1}", brightness, brightnessPercent * 100);

			var brightnessBytes = BitConverter.GetBytes(brightnessPercent);

			var commandBase = new byte[]
            {
                0x21, 0x00, 0x14, 0x00, 0x00, 0x00, (byte) (_id >> 8), (byte) (_id & 0xFF), 0xFF, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00
            };

			var command = commandBase.Concat(brightnessBytes).ToArray();

			Debug.Console(1, this, "Brightness Command {0}", BitConverter.ToString(command));

			SendBytes(command);
		}

		public void RecallPreset(ushort preset)
		{
			var command = new byte[]
            {
                0x74, 0x00, 0x11, 0x00, 0x00, 0x00, (byte) (_id >> 8), (byte) (_id & 0xFF), 0xFF, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, (byte) preset
            };

			Debug.Console(1, this, "Preset Command {0}", BitConverter.ToString(command));
			
			SendBytes(command);
		}

		public void SetShowOn()
		{
			var command = new byte[]
            {
                0x11, 0x00, 0x11, 0x00, 0x00, 0x00, (byte) (_id >> 8), (byte) (_id & 0xFF), 0xFF, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x01
            };

			SendBytes(command);
		}

		public void SetShowOff()
		{
			var command = new byte[]
            {
                0x11, 0x00, 0x11, 0x00, 0x00, 0x00, (byte) (_id >> 8), (byte) (_id & 0xFF), 0xFF, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00
            };

			SendBytes(command);
		}
	}
}

