#!/usr/bin/env python3
"""Colorlight sender emulator (Class A protocol) for bench-testing the plugin without hardware.

Emulates one sender: answers status polls (Detecting Sender Parameters) for its own id, tracks
blackout, brightness, input and preset, and sends the 1-second heartbeat to TCP clients.
Standard library only; Python 3.8+.

Examples:
    python colorlight_emulator.py                      # Z6, sender id 0, TCP 9999
    python colorlight_emulator.py --model s6f          # S6F, UDP 9099
    python colorlight_emulator.py --id 1               # answer polls for sender 1 only
"""

import argparse
import asyncio
import struct
from datetime import datetime

HEARTBEAT = bytes([0x99, 0x99, 0x04, 0x00])
COMMAND_RESPONSE = bytes([0xEF, 0x00, 0x04, 0x00])
BROADCAST_ID = 0xFFFF
HEADER_LENGTH = 16
MAX_FRAME_LENGTH = 4096
STATUS_REPLY_LENGTH = 260

# model name -> (model code, default transport, supports presets, default input code)
MODELS = {
    "z6": (0x11, "tcp", True, 0x10),
    "s6f": (0x15, "udp", False, 0x10),
    "s6": (0x16, "udp", False, 0x10),
}


def log(message):
    print("{} {}".format(datetime.now().strftime("%H:%M:%S.%f")[:-3], message), flush=True)


def hex_bytes(data):
    return " ".join("{:02x}".format(b) for b in data)


class Sender:
    """Device state and command handling, shared by the TCP and UDP front ends."""

    def __init__(self, model, sender_id, auto_input, verbose):
        self.model = model
        self.model_code, _, self.supports_presets, self.input_code = MODELS[model]
        self.sender_id = sender_id
        self.auto_input = auto_input
        self.verbose = verbose
        self.blackout_off = True
        self.brightness = 0.5
        self.preset = None
        self.pending_input = None

    def handle(self, frame):
        """Returns the bytes to send back for one received frame (may be empty)."""
        if frame == HEARTBEAT:
            if self.verbose:
                log("<- heartbeat")
            return b""

        if len(frame) < HEADER_LENGTH:
            log("<- short frame ignored: {}".format(hex_bytes(frame)))
            return b""

        command = frame[0]
        target = frame[6] | (frame[7] << 8)
        payload = frame[HEADER_LENGTH:]

        if target != self.sender_id and target != BROADCAST_ID:
            log("<- 0x{:02x} for sender {} ignored (this is sender {})".format(command, target, self.sender_id))
            return b""

        reply = b""

        if command == 0x01 and frame[14] == 0x01 and payload[:1] == b"\x16":
            if self.verbose:
                log("<- status poll")
            reply += self.status_reply()
        elif command == 0x21 and len(payload) >= 4:
            self.brightness = struct.unpack("<f", payload[:4])[0]
            log("<- brightness {:.4f} ({:.2f}%)".format(self.brightness, self.brightness * 100))
        elif command == 0x11 and len(payload) >= 1:
            self.blackout_off = payload[0] == 0x01
            log("<- blackout {}".format("off (PowerOn)" if self.blackout_off else "on (PowerOff)"))
        elif command == 0x74 and len(payload) >= 1:
            if self.supports_presets:
                self.preset = payload[0]
                log("<- preset {} (0-based)".format(self.preset))
            else:
                log("<- preset {} IGNORED: {} has no presets".format(payload[0], self.model.upper()))
        elif command == 0x33 and len(payload) >= 2:
            self.pending_input = payload[1]
            log("<- input select layer {} code 0x{:02x} (applies on batch 0x6f)".format(payload[0], payload[1]))
        elif command == 0x6F:
            if self.pending_input is not None:
                self.input_code = self.pending_input
                self.pending_input = None
                log("<- batch operation: input now 0x{:02x}".format(self.input_code))
            else:
                log("<- batch operation (nothing pending)")
        else:
            log("<- unhandled 0x{:02x}: {}".format(command, hex_bytes(frame)))

        # Command Response: byte 9 bit 2 asks for a short acknowledgement
        if frame[9] & 0x04:
            reply += COMMAND_RESPONSE

        return reply

    def status_reply(self):
        """Response Frame for Detecting Sender Parameters (f1 00 ... ab ab ab ab 16 01)."""
        frame = bytearray(STATUS_REPLY_LENGTH)
        frame[0] = 0xF1
        frame[2] = STATUS_REPLY_LENGTH & 0xFF
        frame[3] = STATUS_REPLY_LENGTH >> 8
        frame[4:8] = b"\xab\xab\xab\xab"
        frame[8] = 0x16
        frame[9] = 0x01
        frame[12] = self.model_code
        frame[13] = 0x00  # minor version
        frame[14] = 0x01  # major version
        frame[15] = 0x01 if self.blackout_off else 0x00  # DWORD0 bit0: 1 = blackout disabled
        frame[19] = 0x00 if self.auto_input else 0x01  # DWORD1 bit0 (S6/S6F): 1 = manual input switching
        frame[23:27] = struct.pack("<f", self.brightness)
        frame[88] = self.input_code
        frame[192] = self.input_code
        frame[193] = 0x01  # input connected
        return bytes(frame)


class FrameReader:
    """Splits a TCP byte stream into heartbeats and length-prefixed frames."""

    def __init__(self):
        self.buffer = bytearray()

    def feed(self, data):
        self.buffer += data
        frames = []

        while len(self.buffer) >= 4:
            if self.buffer[:4] == HEARTBEAT:
                frames.append(bytes(self.buffer[:4]))
                del self.buffer[:4]
                continue

            length = self.buffer[2] | (self.buffer[3] << 8)
            if length < HEADER_LENGTH or length > MAX_FRAME_LENGTH:
                del self.buffer[0]
                continue

            if len(self.buffer) < length:
                break

            frames.append(bytes(self.buffer[:length]))
            del self.buffer[:length]

        return frames


async def run_tcp(sender, host, port):
    async def client(reader, writer):
        peer = writer.get_extra_info("peername")
        log("TCP client connected: {}".format(peer))
        frames = FrameReader()

        async def heartbeat():
            # The sender sends a heartbeat to the host every second over TCP
            while True:
                writer.write(HEARTBEAT)
                await writer.drain()
                await asyncio.sleep(1)

        beat = asyncio.ensure_future(heartbeat())
        try:
            while True:
                data = await reader.read(4096)
                if not data:
                    break
                for frame in frames.feed(data):
                    reply = sender.handle(frame)
                    if reply:
                        writer.write(reply)
                        await writer.drain()
        except (ConnectionError, OSError):
            pass
        finally:
            beat.cancel()
            writer.close()
            log("TCP client disconnected: {}".format(peer))

    server = await asyncio.start_server(client, host, port)
    log("TCP listening on {}:{}".format(host, port))
    async with server:
        await server.serve_forever()


class UdpProtocol(asyncio.DatagramProtocol):
    def __init__(self, sender):
        self.sender = sender
        self.transport = None

    def connection_made(self, transport):
        self.transport = transport

    def datagram_received(self, data, addr):
        # Each datagram is one frame; no heartbeat over UDP
        reply = self.sender.handle(bytes(data))
        if reply:
            self.transport.sendto(reply, addr)


async def run_udp(sender, host, port):
    loop = asyncio.get_running_loop()
    await loop.create_datagram_endpoint(lambda: UdpProtocol(sender), local_addr=(host, port))
    log("UDP listening on {}:{}".format(host, port))
    await asyncio.Event().wait()


async def main():
    parser = argparse.ArgumentParser(description="Colorlight sender emulator (Class A protocol)")
    parser.add_argument("--model", choices=sorted(MODELS), default="z6", help="model to report (default z6)")
    parser.add_argument("--id", type=int, default=0, help="sender id to answer as (default 0, the first sender)")
    parser.add_argument("--transport", choices=["tcp", "udp", "both"], help="default: tcp for z6, udp for s6/s6f")
    parser.add_argument("--host", default="0.0.0.0", help="address to listen on (default all)")
    parser.add_argument("--tcp-port", type=int, default=9999)
    parser.add_argument("--udp-port", type=int, default=9099)
    parser.add_argument("--auto-input", action="store_true", help="report automatic input switching (S6/S6F flag)")
    parser.add_argument("-v", "--verbose", action="store_true", help="also log heartbeats and status polls")
    args = parser.parse_args()

    sender = Sender(args.model, args.id, args.auto_input, args.verbose)
    transport = args.transport or MODELS[args.model][1]
    log("Emulating {} as sender {}".format(args.model.upper(), args.id))

    tasks = []
    if transport in ("tcp", "both"):
        tasks.append(run_tcp(sender, args.host, args.tcp_port))
    if transport in ("udp", "both"):
        tasks.append(run_udp(sender, args.host, args.udp_port))

    await asyncio.gather(*tasks)


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        pass
