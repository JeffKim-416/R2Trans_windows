# Security

R2Trans is a Windows-only desktop app. The app does not include an OpenAI API key; each user enters their own key in Settings.

## API Key Storage

The OpenAI API key is encrypted with Windows DPAPI using the current-user scope and stored at:

```text
%APPDATA%\R2Trans\openai-api-key.dat
```

General non-secret settings are stored at:

```text
%APPDATA%\R2Trans\settings.json
```

Do not commit API keys, publish outputs, installers, logs, or local environment files.

## Data Sent to OpenAI

R2Trans sends the following user-selected data to OpenAI only when the user invokes the related feature:

- Selected text for translation or rewriting.
- Microphone audio for Live Interpreter when microphone input is enabled.
- System audio for Live Interpreter when system audio input is enabled.

## Windows Permissions

R2Trans uses normal current-user Windows APIs:

- Global hotkey registration through Win32 `RegisterHotKey`.
- Keyboard input simulation through Win32 `SendInput`.
- Clipboard access through the Windows clipboard.
- Microphone capture through Windows audio APIs.
- System audio capture through WASAPI loopback.
- Login startup through `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

## Release Checklist

Before publishing a build, run a local secret scan and inspect ignored artifacts:

```powershell
rg -n "OPENAI_API_KEY|sk-[A-Za-z0-9_-]+|BEGIN .*PRIVATE KEY" .
git status --short --ignored
```

Expected generated outputs are under:

```text
Windows\publish\
Windows\dist\
```
