# Privacy

Ilha keeps runtime data in the `Dados` directory beside its launcher. This directory is excluded from source control and distribution packages.

- Windows media titles, artists, cover thumbnails and playback state are read locally through the Windows media-session API.
- Foreground window bounds, process names and selected game installation paths are checked locally to hide the island during games or full-screen use. A user-selected per-app hide list is stored locally.
- The mascot reads the cursor position to move its eyes. It does not record cursor history.
- Optional Claude hooks store the latest event, session identifier, tool name, project basename and affected file basename. Hook payloads containing prompts or file contents are not stored.
- Robo runs an embedded llama.cpp server on `127.0.0.1` (random port) and sends conversation text and a short focus-timer status only to it, after a user sends a message. The only network access is the first-use download of the engine (GitHub releases) and the chosen model (Hugging Face), verified by SHA-256. If the user selects Ollama instead, text goes to `127.0.0.1:11434`; whether that model is local or cloud-backed is up to the user.
- Chat history stays in memory for the session. There is no telemetry, advertising, screenshot capture or background upload in this implementation.

The manual certificate-signing utility contacts the selected timestamp provider when a maintainer explicitly signs a build. GitHub and SignPath receive build artifacts only as part of the separately configured publication/signing workflow.

This program will not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it.
