# Uintled ColorLight Z6 (c) 2025

## License

Provided under MIT license

## Configuration

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
						"inputNumber": 1,
						"name": "HDMI Processor",
						"hideInput": false
					},
					{
						"inputNumber": 2,
						"name": "DVI Wall PC",
						"hideInput": false
					},
					{
						"inputNumber": 7,
						"name": "Spare",
						"hideInput": true   // hidden from SIMPL: no button action, no feedback, blank name
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
					// standard Essentials TCP/UDP control config here
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


