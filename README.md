# Ilha for Windows

A Windows desktop companion that rests as a small black capsule with two eyes and expands into a translucent dashboard on hover.

## Project status

**In development.** The goal is a complete reproduction of the interaction and functionality requested from the visual references. That goal has not yet been achieved. This is a newly written implementation with original source code and an original icon.

Visual references: [glass island](https://www.instagram.com/reel/DfmGp5ZSFKK/) and [night-owl desktop](https://www.instagram.com/reel/DfxcJk9mUEU/).

Implemented in source:

- A 128 × 34 black resting capsule with blinking eyes and gaze movement.
- Hover expansion and automatic collapse after the pointer leaves.
- A dashboard containing Claude, music and focus cards.
- Windows media-session adapter, full-bleed cover-based music background, interpolated progress and local audio playback.
- Synced lyrics loaded from a user-selected `.lrc` file through the + menu. Lyrics stay associated with the current track.
- Focus countdown with start, pause, reset and minute selection.
- Claude Code event hooks enabled per session.
- A Robo chat interface with an optional local Ollama connection.
- A tray menu with restart/update and shutdown controls.
- A tool-window style that excludes the island from Alt+Tab.
- Suppression during foreground full-screen applications, known game window classes and supported game installations, with an additional per-app hide list.

Still requiring completion or broader verification: fidelity across all reference screens, full-screen/game behavior across more applications, media-session compatibility, live Claude integration and end-to-end AI conversations. The latest executable could not be tested on the development PC because Smart App Control rejected its unsigned build.

## Run

Requires Windows with .NET Framework 4.8. Native desktop acrylic requires Windows 11 build 22621 or later; other systems use an opaque fallback.

Extract the entire distribution ZIP into a normal folder, then open **Ilha.exe**. Running `Iniciar.cmd` from inside the ZIP does not extract its dependencies.

The + menu provides local music, synced lyrics, focus and new-chat actions. The capsule appears at the top of the primary screen. Hover to expand; move away to collapse. Use the tray icon to restart/update or shut down. A pinned shortcut to `Ilha.exe` can stay on the taskbar while the floating window remains outside Alt+Tab.

Unsigned downloads can be blocked by Smart App Control. Trusted signing is pending; see the [Code signing policy](CODE_SIGNING_POLICY.md).

## Build

On Windows, using the .NET Framework compiler already installed with the framework:

```powershell
powershell.exe -NoProfile -File .\Compilar.ps1
powershell.exe -NoProfile -File .\scripts\Test.ps1
powershell.exe -NoProfile -File .\scripts\Package.ps1 -SkipBuild
```

Builds are stored under `Versions/`. `Ilha.exe` is a stable launcher that reads `current-version.txt`, enabling version replacement without overwriting a running runtime executable.

## Connections

**Windows media:** reads the currently selected system media session. Features depend on what the player exposes. The app displays connection failures instead of sample track data.

**Claude Code:** use the Claude panel to copy a command that starts a Claude session with generated hooks. Existing global settings and approval decisions are preserved. The hook stores event names, tool names and file basenames locally.

**Robo:** start Ollama on the same PC, then use Settings → Detectar modelos. The application connects only to `127.0.0.1:11434`. Any further network behavior depends on the model/server configured by the user.

## Privacy and uninstall

See [Privacy](PRIVACY.md). To uninstall, shut down the tray application, remove any pinned shortcut and delete the extracted folder. A per-session Claude connection stops when that Claude session ends. This app does not install an auto-start entry.

## Code signing policy

See [CODE_SIGNING_POLICY.md](CODE_SIGNING_POLICY.md). The SignPath Foundation application was submitted on 2026-11-15 and is awaiting review; no acceptance, certificate or signed release is claimed.

## License

[MIT](LICENSE).

## Visual checks

The GitHub workflow uploads an `ilha-visual-previews` artifact containing offscreen WPF renders. Files named `preview-reference-*` use explicit synthetic playback/chat fixtures for visual review; they are not live service results. Native desktop acrylic is composited by Windows and is not included in these offscreen images.
