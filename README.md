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

| Model | TCP 9999 | UDP 9099 | Brightness | Show On/Off (blackout) | Preset recall |
|-------|:--------:|:--------:|:----------:|:----------------------:|:-------------:|
| Z6, X8/X8C, X16/X16B/X16C/X16D | ✓ | | ✓ | ✓ | ✓ |
| Z4/S4R, Z4F | ✓ | | ✓ | ✓ | ✓ |
| CM8 | ✓ | | ✓ | ✓ | ✓ |
| Z6 PRO/Z6 PRO G2 | ✓ | | ✓ | ✓ | ✓ |
| X20(LAN), X16E/X8E | ✓ | | ✓ | ✓ | ✓ |
| S6/S6F | | ✓ | ✓ | ✓ | |
| X1/X3, X2/X4, X2s/X4s, X4e, X5/X6/X7, X12, X20, S20/S20F, S5 PRO | | | ✓ | ✓ | ✓ |
| S10 | | | ✓ | | |
| S10 PRO, S10 PRO-U | | | ✓ | | ✓ |
| S7 | | | ✓ | ✓ | |

- Models with no network control port are controlled over serial (115200 baud, 8N1). Most use RS-232; the Z4F and X4e use TTL-level serial and need a level converter.
- **Input select** uses the Z6's input codes. Other models number their inputs differently, so input select is only expected to work on the Z6.
- Colorlight senders that are not in the Class A document (for example X100 Pro, Z8t, VX10) are not supported.

## Requirements

- PepperDash Essentials **3.0.0** or later, on a Crestron **4-Series** processor. 3-Series is no longer supported.
- The plugin DLL is now `PepperDash.Essentials.Plugins.Uintled.ColorlightZ6.dll`. When upgrading, delete older builds of this plugin from the processor's `plugins` folder (`PepperDash.Essentials.Plugin.Colorlight.Z6.dll` or `epi-colorlight-z6.4Series.dll`) so the device types are not registered twice.

## License

Provided under MIT license

## Configuration

### Communication options

The plugin can use any of the following via the standard Essentials `properties.control` configuration block. Which ones a given sender supports is listed in [Model support](#model-support-class-a-protocol); the Z6 supports TCP and serial.

- **TCP/IP** – connect to the device IP on **port 9999**.
- **UDP** – send/receive datagrams to the device IP on **port 9099** (S6/S6F only).
- **Serial** – connect at **115200 baud, 8 data bits, 1 stop bit, no parity**.

Use the appropriate Essentials transport type (tcpIp, udp, com) in the `control` section of the device configuration to match how the sender is wired in your system.

### Device ID (`properties.id`)

The ColorLight Z6 EPI uses an `id` value in the Essentials configuration to determine which device ID to embed in all commands.

- Config path: `properties.id`
- Type: unsigned 16-bit integer (0–65535), specified as a **decimal** value in JSON.
- Wire format: the value is split into high/low bytes and sent as:
	- High byte: `(byte)(id >> 8)`
	- Low byte: `(byte)(id & 0xFF)`

Common cases:

- `id = 1`
	- Wire bytes: `0x00 0x01`
	- Example usage: target a specific device with ID 1.

- `id = 65535`
	- Wire bytes: `0xFF 0xFF`
	- Example usage: broadcast / "unknown ID" mode as allowed by the manufacturer.
		This is how to send `\xFF\xFF` on the wire from config.

> Note: JSON does **not** support hex literals like `0xFFFF`; always enter the decimal
> equivalent (`65535`) in the Essentials configuration.

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
				"id": 1,       // sends 0x00 0x01 as the ID bytes
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
| 1      | PowerOff          | ToFromSIMPL| Digital  | Command + fake feedback: off state. Clears when device goes offline.                                                                            |
| 2      | PowerOn           | ToFromSIMPL| Digital  | Command + fake feedback: on state. Clears when device goes offline.                                                                             |
| 11–17  | InputSelectOffset | ToFromSIMPL| Digital  | One-hot input select and feedback array. 11–17 map to inputs 1–7 (HDMI, DVI, DVI-2, DVI-3, DVI-4, SDI, SDI-2). Honors `friendlyNames.hideInput`. |
| 50     | IsOnline          | ToSIMPL    | Digital  | High when the device is considered online by the communication monitor.                                                                         |

### Analog joins

| Join # | Name        | Direction   | Type    | Description                                                                                                                                  |
|--------|-------------|------------|---------|----------------------------------------------------------------------------------------------------------------------------------------------|
| 33     | Brightness  | FromSIMPL  | Analog  | Brightness level, 0–65535. The EPI maps this to a float 0.00–1.00 and sends it as a 4-byte IEEE 754 float.                                   |
| 21     | Preset      | FromSIMPL  | Analog  | Preset recall. SIMPL uses 1-based indexing; the EPI subtracts 1 and sends a 0-based preset index.                                            |
| 11     | InputSelect | ToFromSIMPL| Analog  | Input select command and fake feedback. 1–7 map to HDMI, DVI, DVI-2, DVI-3, DVI-4, SDI, SDI-2. Resets to 0 when the device goes offline.    |

### Serial joins

| Join # | Name             | Direction | Description                                                                                   |
|--------|------------------|-----------|-----------------------------------------------------------------------------------------------|
| 1      | DeviceName       | ToSimpl   | Device name, taken from the Essentials device config (`name`) and pushed to SIMPL by the EPI. |
| 11     | InputNamesOffset | ToSimpl   | Input names (HDMI, DVI, DVI-2, DVI-3, DVI-4, SDI, SDI-2, ...), starting at this offset.       |


