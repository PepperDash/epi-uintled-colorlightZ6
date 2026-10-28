# Colorlight sender emulator

A small Python program that acts like one Colorlight sender, so the plugin can be tested without hardware. It isn't part of the plugin build or the `.cplz`.

It needs Python 3.8 or later and nothing else.

## What it does

- Answers status polls (Detecting Sender Parameters) only for its own sender id, with the model, blackout state, brightness and current input.
- Accepts brightness (`0x21`), blackout (`0x11`), input select (`0x33`, applied on Batch Operation `0x6f`) and preset (`0x74`), and logs each one.
- Ignores presets on the S6/S6F, as the real senders do.
- Over TCP, sends the protocol's heartbeat (`99 99 04 00`) every second. Nothing is sent unasked over UDP.
- Sends the short Command Response (`ef 00 04 00`) when a frame asks for it.

## Run it

```shell
python colorlight_emulator.py                  # Z6, sender id 0, TCP 9999
python colorlight_emulator.py --model s6f      # S6F, UDP 9099
python colorlight_emulator.py --id 1           # answer as sender 1
python colorlight_emulator.py -v               # also log heartbeats and status polls
```

| Option | Default | Meaning |
|---|---|---|
| `--model` | `z6` | `z6`, `s6f` or `s6` |
| `--id` | `0` | Sender id it answers as |
| `--transport` | `tcp` for Z6, `udp` for S6/S6F | `tcp`, `udp` or `both` |
| `--tcp-port` / `--udp-port` | `9999` / `9099` | Listening ports |
| `--auto-input` | off | Report automatic input switching (S6/S6F flag) |
| `-v` | off | Also log heartbeats and status polls |

Stop it with Ctrl+C.

## Point the plugin at it

Set the device's `control` to the IP of the PC running the emulator, on the matching port and method:

```jsonc
"control": {
	"method": "tcpip",          // "udp" for --model s6f / s6
	"tcpSshProperties": {
		"address": "192.168.1.50",  // the PC running the emulator
		"port": 9999,               // 9099 for UDP
		"autoReconnect": true
	}
}
```

The PC's firewall must allow the port in.

## Things to try

| Test | How | Expect |
|---|---|---|
| Normal | Same `id` in config and emulator | Online; Model join shows "Z6"; brightness, power and input feedback follow commands |
| Wrong id over TCP | Run with `--id 1`, config `id` 0 | Warning: "Connected but no status reply from sender 0. Check properties.id." |
| No sender | Stop the emulator | Warning: "No reply from sender 0 …" and IsOnline low after 2 minutes; Error after 5 minutes |
| Back again | Restart it | Information: "Sender 0 responding again." |
| S6F | `--model s6f`, UDP config | Online over UDP; preset recall logs a Warning in the plugin and nothing is sent |
| Power-on brightness | Set `powerOnBrightness`, press PowerOn | Emulator logs blackout off, then the brightness |
