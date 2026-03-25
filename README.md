![PepperDash Essentials Pluign Logo](/images/essentials-plugin-blue.png)
# Uintled ColorLight Z6 (c) 2025

## License

Provided under MIT license

## Configuration

### Communication options

The ColorLight Z6 exposes three control integration methods. The EPI can use any of these via the standard Essentials `properties.control` configuration block:

- **TCP/IP** – connect to the device IP on **port 9999**.
- **UDP** – send/receive datagrams to the device IP on **port 9099**.
- **Serial / RS‑232** – connect at **115200 baud, 8 data bits, 1 stop bit, no parity** 

Use the appropriate Essentials transport type (tcpIp, udp, com) in the `control` section of the device configuration to match how the Z6 is wired in your system.

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

The SIMPL bridge mapping is defined by the `ColorlightZ6JoinMap` class. The tables below show the join numbers, directions, types, and descriptions.

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


<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 2.24.2
<!-- END Minimum Essentials Framework Versions -->
<!-- START Supported Types -->

<!-- END Supported Types -->
<!-- START Join Maps -->

<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- ICommunicationMonitor
- IDisposable
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- EssentialsBridgeableDevice
- JoinMapBaseAdvanced
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void SendBytes(byte[] command)
- public void SetBrightness(ushort brightness)
- public void RecallPreset(ushort preset)
- public void PowerOn()
- public void PowerOff()
- public void Dispose()
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->

<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->
### Int Feedbacks

- InputNumberFeedback
<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->

<!-- END String Feedbacks -->
