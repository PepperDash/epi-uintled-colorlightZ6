![PepperDash Essentials Pluign Logo](/images/essentials-plugin-blue.png)
# Colorlight Sender (Z6) (c) 2026

## About the manufacturer and supported models

**Colorlight** is the manufacturer of the device this plugin controls: an LED *sender* (also called a sending card or video processor) that drives the receiving cards inside an LED wall. The **Z6** is one of many Colorlight senders that share the same control protocol, *Colorlight Device Control Protocol – Class A*.

**Unit LED** is an LED panel maker whose displays can be driven by Colorlight senders. The repository name `epi-uintled-colorlightZ6` contains a misspelling of "Unit LED" and is kept as-is because the plugin's assembly and package names are derived from it.

### Device types

| Type | Use for |
|------|---------|
| `colorlightz6` | Colorlight Z6. Kept for existing configurations. |
| `colorlightsender` | Any Colorlight Class A sender. Currently identical to `colorlightz6`. |

### Model support (Class A protocol)

The table below shows what the **Class A protocol** supports on each model, according to Colorlight's protocol document. It is not a list of models this plugin has been tested with. So far the plugin has only been implemented and tested against the **Z6 over TCP**. Other models may need changes to the plugin's behavior before they work fully: for example, the S6/S6F is controlled over UDP, has no preset recall, and numbers its inputs differently from the Z6.

| Model | TCP 9999 | UDP 9099 | Serial | Brightness | Show On/Off (blackout) | Preset recall |
|-------|:--------:|:--------:|:------:|:----------:|:----------------------:|:-------------:|
| Z6, X8/X8C, X16/X16B/X16C/X16D | ✓ | | RS-232 (PCB V5.1 only) | ✓ | ✓ | ✓ |
| Z4/S4R, Z4F | ✓ | | | ✓ | ✓ | ✓ |
| CM8 | ✓ | | | ✓ | ✓ | ✓ |
| Z6 PRO/Z6 PRO G2 | ✓ | | RS-232 | ✓ | ✓ | ✓ |
| X20(LAN), X16E/X8E | ✓ | | RS-232 | ✓ | ✓ | ✓ |
| S6/S6F | | ✓ | | ✓ | ✓ | |
| X20, X5/X6/X7, X12, S20/S20F, S5 PRO | | | RS-232 | ✓ | ✓ | ✓ |
| X1/X3, X2s/X4s | | | TTL | ✓ | ✓ | ✓ |
| S10 | | | RS-232 | ✓ | | |
| S10 PRO, S10 PRO-U | | | RS-232 | ✓ | | ✓ |
| S7 | | | RS-232 | ✓ | ✓ | |

- Serial runs at 115200 baud, 8N1. TTL-level ports need a level converter to connect to a Crestron RS-232 port.
- The **X2/X4** and **X4e** have no network control or serial port (USB only), so this plugin cannot control them.
- **Input select** uses the Z6's input codes. Other models number their inputs differently, so input select is only expected to work on the Z6.
- Colorlight senders that are not in the Class A document (for example X100 Pro, Z8t, VX10) are not supported.

## Requirements

- PepperDash Essentials **3.0.0** or later, on a Crestron **4-Series** processor. 3-Series is no longer supported.
- The plugin DLL is now `PepperDash.Essentials.Plugins.Uintled.ColorlightZ6.dll`. When upgrading, delete older builds of this plugin from the processor's `plugins` folder (`PepperDash.Essentials.Plugin.Colorlight.Z6.dll` or `epi-colorlight-z6.4Series.dll`) so the device types are not registered twice.
- **Check `properties.id` when upgrading.** The sender id is now sent in the byte order the protocol requires. Use **`0` for a single sender** or **`65535` for all senders** (for example, several cascaded senders that form one display); neither value is affected by this change. Any other value now addresses a different sender than before. See [Device ID](#device-id-propertiesid).

## License

Provided under MIT license

## Configuration

### Communication options

The plugin can use any of the following via the standard Essentials `properties.control` configuration block. Which ones a given sender supports is listed in [Model support](#model-support-class-a-protocol); the Z6 supports TCP, and RS-232 on PCB V5.1 units.

- **TCP/IP** – connect to the device IP on **port 9999**.
- **UDP** – send/receive datagrams to the device IP on **port 9099** (S6/S6F only).
- **Serial** – connect at **115200 baud, 8 data bits, 1 stop bit, no parity**.

Use the appropriate Essentials transport type (tcpIp, udp, com) in the `control` section of the device configuration to match how the sender is wired in your system.

### Device ID (`properties.id`)

The plugin uses an `id` value in the Essentials configuration to select which sender each command is for. In the Class A protocol this is the sender's **index**: `0` is the first sender, `1` the second, and so on when senders are cascaded. `65535` (0xFFFF) broadcasts to all senders.

**Use `0` for a single sender, or `65535` for all senders.** A command sent to `0` reaches only the first sender, so when several cascaded senders drive one display, use `65535` so they all change together.

- Config path: `properties.id`
- Type: unsigned 16-bit integer (0–65535), specified as a **decimal** value in JSON.
- Wire format: little-endian, as the protocol requires (low byte first, then high byte).

Common cases:

- `id = 0`
	- Wire bytes: `0x00 0x00`
	- The first (or only) sender. Use this for a single sender.

- `id = 1`
	- Wire bytes: `0x01 0x00`
	- The second sender in a cascade.

- `id = 65535`
	- Wire bytes: `0xFF 0xFF`
	- Broadcast to all senders.

> Note: JSON does **not** support hex literals like `0xFFFF`; always enter the decimal
> equivalent (`65535`) in the Essentials configuration.

### Online status

The plugin polls the sender for its status (Class A "Detecting Sender Parameters") and reports the device online only while valid replies come back. The poll goes to `properties.id`, or to sender `0` when `id` is `65535` (a broadcast query would get replies from every sender at once).

| Situation | Status | Logged once when it starts |
|---|---|---|
| Valid status replies | Online | — |
| Data arriving (e.g. TCP heartbeats) but no status reply | Warning | "Connected but no status reply from sender {id}. Check properties.id." |
| Nothing received | Warning, then offline | "No reply from sender {id}. Check network/IP and properties.id." |
| Replies resume | Online | "Sender {id} responding again." |

Default timing is a poll every 30 s, Warning after 2 minutes without a reply, and offline after 5 minutes. To change it, add the standard Essentials block to `properties` (values in ms):

```jsonc
"communicationMonitorProperties": {
	"pollInterval": 30000,
	"timeToWarning": 120000,
	"timeToError": 300000
}
```

`pollInterval` must be less than `timeToWarning`, which must not be more than `timeToError`; both times must be at least 5000.

Over TCP the plugin also sends the protocol's 1-second heartbeat to keep the connection open. It is not sent over UDP or serial, where the protocol doesn't use it.

### Input codes

Each input number (buttons 11–17, analog 11) is sent to the sender as a protocol input code. The defaults follow the Z6:

| Input | Button | Default name | Default code |
|---|---|---|---|
| 1 | 11 | HDMI | 16 (0x10) |
| 2 | 12 | DVI | 80 (0x50) |
| 3 | 13 | DVI-2 | 2 (0x02) |
| 4 | 14 | DVI-3 | 3 (0x03) |
| 5 | 15 | DVI-4 | 4 (0x04) |
| 6 | 16 | SDI | 32 (0x20) |
| 7 | 17 | SDI-2 | 33 (0x21) |

For other models, set `inputCode` (decimal) on that input's `friendlyNames` entry. Codes from the Class A protocol for models other than the Z6:

| Inputs | Codes |
|---|---|
| HDMI1–HDMI7 | 16–23 (0x10–0x17) |
| DP1–DP7 | 48–55 (0x30–0x37) |
| DVI1–DVI12 | 1–12 (0x01–0x0C) |
| DVI13–DVI16 | 144–147 (0x90–0x93) |
| SDI1–SDI16 | 32–47 (0x20–0x2F) |
| VGA, AV1, AV2 | 64, 65, 66 (0x40–0x42) |

```jsonc
"friendlyNames": [
	{ "inputNumber": 2, "name": "DVI 1", "inputCode": 1 }   // DVI1 on a non-Z6 sender
]
```

Input feedback comes from the sender's current input in the status poll, matched back to an input number through these codes. A code that matches no input number reports 0.

### Example Essentials configuration snippet

```jsonc
{
	"devices": [
		{
			"key": "colorlight-z6-1",
			"name": "ColorLight Z6",
			"type": "colorlightz6",
			"group": "displays",
			"properties": {
				"control": {
					// standard Essentials TCP/UDP control config here
				},
				"id": 0,       // first (or only) sender; sends 0x00 0x00 as the ID bytes
				"friendlyNames": [
					{
						"inputNumber": 1,       // button 11
						"name": "HDMI Processor",
						"hideInput": false,
						"inputSelect": 1,       // optional: route to input 1 (default behavior)
						"brightness": 32767,    // optional: 50% brightness
						"preset": 1             // optional: recall preset 1
					},
					{
						"inputNumber": 2,       // button 12
						"name": "Scene - Movie",
						"hideInput": false,
						"brightness": 49149,    // brightness-only scene (75%)
						"preset": 3             // optional: preset 3
					},
					{
						"inputNumber": 3,       // button 13
						"name": "Preset Only",
						"hideInput": false,
						"preset": 5             // preset-only scene
					},
					{
						"inputNumber": 7,       // button 17
						"name": "Spare",
						"hideInput": true        // hidden from SIMPL: no button action, no feedback, blank name
					}
				]
			}
		},
		{
			"key": "colorlight-z6-broadcast",
			"name": "ColorLight Z6 (Broadcast)",
			"type": "colorlightz6",
			"group": "displays",
			"properties": {
				"control": {
					"method": "com",
					"controlPortDevKey": "processor",
					"controlPortNumber": 1,
					"comParams": {
						"hardwareHandshake": "None",
						"parity": "None",
						"protocol": "RS232",
						"baudRate": 115200,
						"dataBits": 8,
						"softwareHandshake": "None",
						"stopBits": 1
					}
				},	
				"id": 65535   // sends 0xFF 0xFF as the ID bytes
			}
		}
	]
}
```

## SIMPL Bridge Join Map

The SIMPL bridge mapping is defined by the `ColorlightZ6BridgeJoinMap` class. The tables below show the join numbers, directions, types, and descriptions.

### Digital joins

| Join # | Name              | Direction   | Type     | Description                                                                                                                                     |
|--------|-------------------|------------|----------|-------------------------------------------------------------------------------------------------------------------------------------------------|
| 1      | PowerOff          | ToFromSIMPL| Digital  | Command: blackout on. Feedback: high while the sender is in blackout (from the status poll; set immediately when pressed). Clears when the connection drops. |
| 2      | PowerOn           | ToFromSIMPL| Digital  | Command: blackout off. Feedback: high while the sender is showing the image (from the status poll; set immediately when pressed). Clears when the connection drops. |
| 11–17  | InputSelectOffset | ToFromSIMPL| Digital  | One-hot input select and feedback array. 11–17 map to inputs 1–7 (see [Input codes](#input-codes)). Feedback follows the sender's actual input from the status poll. Honors `friendlyNames.hideInput`. |
| 50     | IsOnline          | ToSIMPL    | Digital  | High when the device is considered online by the communication monitor.                                                                         |

### Analog joins

| Join # | Name        | Direction   | Type    | Description                                                                                                                                  |
|--------|-------------|------------|---------|----------------------------------------------------------------------------------------------------------------------------------------------|
| 33     | Brightness  | ToFromSIMPL| Analog  | Brightness level, 0–65535, sent at full resolution as a float 0–1.0. Feedback: the sender's actual brightness from the status poll. |
| 21     | Preset      | FromSIMPL  | Analog  | Preset recall. SIMPL uses 1-based indexing; the EPI subtracts 1 and sends a 0-based preset index.                                            |
| 11     | InputSelect | ToFromSIMPL| Analog  | Input select command and feedback, 1–7 (see [Input codes](#input-codes)). Feedback follows the sender's actual input from the status poll; 0 when the input matches no input number or the connection drops. |

### Serial joins

| Join # | Name             | Direction | Description                                                                                   |
|--------|------------------|-----------|-----------------------------------------------------------------------------------------------|
| 1      | DeviceName       | ToSimpl   | Device name, taken from the Essentials device config (`name`) and pushed to SIMPL by the EPI. |
| 2      | Model            | ToSimpl   | Sender model from the status poll (e.g. Z6, S6F). Blank until the first status reply. |
| 11     | InputNamesOffset | ToSimpl   | Input names (HDMI, DVI, DVI-2, DVI-3, DVI-4, SDI, SDI-2, ...), starting at this offset.       |


