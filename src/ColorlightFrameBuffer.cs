using System.Collections.Generic;

namespace PepperDash.Essentials.Plugins
{
    public enum ColorlightFrameType
    {
        Heartbeat,
        CommandResponse,
        StatusReply,
        Other
    }

    public class ColorlightFrame
    {
        public ColorlightFrameType Type { get; private set; }
        public byte[] Bytes { get; private set; }

        public ColorlightFrame(ColorlightFrameType type, byte[] bytes)
        {
            Type = type;
            Bytes = bytes;
        }
    }

    /// <summary>
    /// Collects received bytes and splits them into Class A protocol frames. Data can arrive
    /// split across packets (TCP) or with several frames in one packet.
    /// </summary>
    public class ColorlightFrameBuffer
    {
        // Status replies are 260 bytes; anything this large without a complete frame is junk
        private const int MaxBufferLength = 4096;
        private const int MinStatusFrameLength = 10;

        private readonly List<byte> _buffer = new List<byte>();
        private readonly object _lock = new object();

        /// <summary>
        /// Adds received bytes and returns every complete frame now available.
        /// Bytes that don't start a known frame are discarded one at a time to resync.
        /// </summary>
        public List<ColorlightFrame> Append(byte[] data)
        {
            var frames = new List<ColorlightFrame>();

            lock (_lock)
            {
                _buffer.AddRange(data);

                if (_buffer.Count > MaxBufferLength)
                    _buffer.Clear();

                while (_buffer.Count > 0)
                {
                    var first = _buffer[0];

                    // Heartbeat: 99 99 04 00
                    if (first == 0x99)
                    {
                        if (_buffer.Count < 4) break;

                        if (_buffer[1] == 0x99 && _buffer[2] == 0x04 && _buffer[3] == 0x00)
                        {
                            frames.Add(new ColorlightFrame(ColorlightFrameType.Heartbeat, Take(4)));
                            continue;
                        }

                        _buffer.RemoveAt(0);
                        continue;
                    }

                    // Short command response: ef 00 04 00
                    if (first == 0xEF)
                    {
                        if (_buffer.Count < 4) break;

                        if (_buffer[1] == 0x00 && _buffer[2] == 0x04 && _buffer[3] == 0x00)
                        {
                            frames.Add(new ColorlightFrame(ColorlightFrameType.CommandResponse, Take(4)));
                            continue;
                        }

                        _buffer.RemoveAt(0);
                        continue;
                    }

                    // Response frames: f1 00 <length, little-endian> ...
                    if (first == 0xF1)
                    {
                        if (_buffer.Count < 4) break;

                        var length = _buffer[2] | (_buffer[3] << 8);

                        if (_buffer[1] != 0x00 || length < MinStatusFrameLength || length > MaxBufferLength)
                        {
                            _buffer.RemoveAt(0);
                            continue;
                        }

                        if (_buffer.Count < length) break;

                        var frame = Take(length);
                        var type = IsStatusReply(frame) ? ColorlightFrameType.StatusReply : ColorlightFrameType.Other;
                        frames.Add(new ColorlightFrame(type, frame));
                        continue;
                    }

                    _buffer.RemoveAt(0);
                }
            }

            return frames;
        }

        public void Clear()
        {
            lock (_lock)
            {
                _buffer.Clear();
            }
        }

        // Response Frame for Detecting Sender Parameters: f1 00 <len> ab ab ab ab 16 01 ...
        private static bool IsStatusReply(byte[] frame)
        {
            return frame[4] == 0xAB && frame[5] == 0xAB && frame[6] == 0xAB && frame[7] == 0xAB
                && frame[8] == 0x16 && frame[9] == 0x01;
        }

        private byte[] Take(int count)
        {
            var bytes = _buffer.GetRange(0, count).ToArray();
            _buffer.RemoveRange(0, count);
            return bytes;
        }
    }
}
