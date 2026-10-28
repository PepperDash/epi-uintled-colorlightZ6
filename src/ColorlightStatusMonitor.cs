using System;
using System.Timers;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugins
{
    /// <summary>
    /// Online status driven by valid status replies from the sender rather than by any received
    /// bytes, so a wrong properties.id shows up as a problem instead of a healthy connection.
    /// </summary>
    public class ColorlightStatusMonitor : StatusMonitorBase
    {
        private enum Condition
        {
            Starting,
            Ok,
            NoStatusReply,
            NoData
        }

        private readonly Action _poll;
        private readonly long _pollTime;
        private readonly long _warningTime;
        private readonly long _errorTime;
        private readonly ushort _senderId;
        private readonly object _lock = new object();

        private Timer _pollTimer;
        private DateTime _lastStatusReply;
        private DateTime _lastData;
        private bool _replyReceived;
        private Condition _condition = Condition.Starting;

        public ColorlightStatusMonitor(IKeyed parent, long pollTime, long warningTime, long errorTime, ushort senderId, Action poll)
            : base(parent, warningTime, errorTime)
        {
            if (pollTime >= warningTime)
                throw new ArgumentException("pollTime must be less than warningTime");

            _pollTime = pollTime;
            _warningTime = warningTime;
            _errorTime = errorTime;
            _senderId = senderId;
            _poll = poll;
        }

        public override void Start()
        {
            lock (_lock)
            {
                if (_pollTimer != null)
                    return;

                // Time the warning/error windows from start-up rather than from DateTime.MinValue
                _lastStatusReply = DateTime.UtcNow;
                _lastData = DateTime.MinValue;
                _replyReceived = false;
                _condition = Condition.Starting;

                _pollTimer = new Timer(_pollTime) { AutoReset = true };
                _pollTimer.Elapsed += (s, e) => OnPollTimer();
                _pollTimer.Start();
            }

            _poll();
        }

        public override void Stop()
        {
            lock (_lock)
            {
                if (_pollTimer == null)
                    return;

                _pollTimer.Stop();
                _pollTimer.Dispose();
                _pollTimer = null;
            }
        }

        /// <summary>
        /// Call for every valid status reply.
        /// </summary>
        public void StatusReplyReceived()
        {
            lock (_lock)
            {
                _lastStatusReply = DateTime.UtcNow;
                _lastData = _lastStatusReply;
                _replyReceived = true;
            }

            Evaluate();
        }

        /// <summary>
        /// Call for any other frame from the sender (heartbeats, command responses).
        /// </summary>
        public void DataReceived()
        {
            lock (_lock)
            {
                _lastData = DateTime.UtcNow;
            }
        }

        private void OnPollTimer()
        {
            Evaluate();
            _poll();
        }

        private void Evaluate()
        {
            Condition condition;
            MonitorStatus status;
            var now = DateTime.UtcNow;

            lock (_lock)
            {
                var sinceReply = (now - _lastStatusReply).TotalMilliseconds;
                var sinceData = (now - _lastData).TotalMilliseconds;

                if (sinceReply < _warningTime)
                {
                    // Inside the window since the last reply, or since start-up before the first one
                    condition = _replyReceived ? Condition.Ok : Condition.Starting;
                    status = _replyReceived ? MonitorStatus.IsOk : MonitorStatus.StatusUnknown;
                }
                else if (sinceData < _warningTime)
                {
                    // Data is arriving (e.g. TCP heartbeats) but this sender isn't answering status polls
                    condition = Condition.NoStatusReply;
                    status = MonitorStatus.InWarning;
                }
                else
                {
                    condition = Condition.NoData;
                    status = sinceReply < _errorTime ? MonitorStatus.InWarning : MonitorStatus.InError;
                }

                if (condition == _condition && status == Status)
                    return;

                var previous = _condition;
                _condition = condition;

                if (condition != previous)
                    LogConditionChange(previous, condition);
            }

            Status = status;
            Message = MessageFor(condition);
        }

        private void LogConditionChange(Condition previous, Condition condition)
        {
            switch (condition)
            {
                case Condition.NoStatusReply:
                    Parent.LogWarning("Connected but no status reply from sender {SenderId}. Check properties.id.", _senderId);
                    break;
                case Condition.NoData:
                    Parent.LogWarning("No reply from sender {SenderId}. Check network/IP and properties.id.", _senderId);
                    break;
                case Condition.Ok:
                    if (previous == Condition.NoStatusReply || previous == Condition.NoData)
                        Parent.LogInformation("Sender {SenderId} responding again.", _senderId);
                    break;
            }
        }

        private string MessageFor(Condition condition)
        {
            switch (condition)
            {
                case Condition.NoStatusReply:
                    return string.Format("Connected but no status reply from sender {0}. Check properties.id.", _senderId);
                case Condition.NoData:
                    return string.Format("No reply from sender {0}. Check network/IP and properties.id.", _senderId);
                default:
                    return string.Empty;
            }
        }
    }
}
