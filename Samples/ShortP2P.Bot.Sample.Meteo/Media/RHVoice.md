# RHVoice for the Meteo weather bot

The bot synthesizes weather scripts through Windows **SAPI5** (`System.Speech`).
RHVoice installs as ordinary SAPI voices, so no native library or API key is required.

## Install (Windows)

1. Open [RHVoice installation](https://rhvoice.org/installation/) → **For other Windows programs** (SAPI 5).
2. Install at least one **Russian** voice (recommended: Elena, Irina, or Aleksandr).
   Each voice installer includes the RHVoice core.
3. Optional: install English (or other language) RHVoice voices the same way if you want them for non-Russian prompts.
4. Restart the bot process so `SpeechSynthesizer` reloads the voice list.

Official SAPI installer list: [RHVoice Binaries](https://github.com/RHVoice/RHVoice/blob/master/doc/en/Binaries.md) (section **SAPI 5**).

There is no Chocolatey/winget package used by this project; use the official `.exe` / `.msi` installers.

### Typical install locations (after Elena SAPI setup)

| Piece | Path |
| --- | --- |
| Engine (64-bit) | `C:\Program Files\rhvoice.org\RHVoice\` |
| Engine (32-bit) | `C:\Program Files (x86)\rhvoice.org\RHVoice\` |
| Voice / language data | `C:\ProgramData\rhvoice.org\RHVoice\data\` |
| Optional config | `C:\ProgramData\Contributors to the RHVoice project\RHVoice\config\RHVoice.ini` |

SAPI often exposes the voice as the short name **`Elena`** (culture `ru-RU`), not `RHVoice Elena`.

## Enable in appsettings

```json
"Voice": {
  "Format": "ogg",
  "BitRate": 64000,
  "Engine": "Auto",
  "PreferredVoice": "Elena"
}
```

| Setting | Values | Behavior |
| --- | --- | --- |
| `Engine` | `Auto` (default) | Prefer an RHVoice SAPI voice for the reply language when installed; otherwise Microsoft/System.Speech. |
| `Engine` | `RHVoice` | Prefer RHVoice; still fall back to System.Speech if no matching RHVoice voice is found. |
| `Engine` | `SystemSpeech` | Skip RHVoice; use Microsoft/other SAPI voices. |
| `PreferredVoice` | optional name | Exact or substring match (e.g. `Elena`) after language filtering. |

If RHVoice is not installed, synthesis still works with the built-in Windows voices (often English-only on a minimal install).

## How Russian selection works

For language `ru`, the bot looks for enabled SAPI voices whose culture is Russian.
With `Engine` = `Auto` or `RHVoice`, known RHVoice voices (name contains `RHVoice`, or a known short name such as `Elena`) rank first.
`PreferredVoice` wins over the default RHVoice pick when it matches.
Otherwise the first matching Microsoft/System.Speech Russian voice is used (or phonetic fallback text if none exist).
