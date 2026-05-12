# R2Trans

A Windows-only desktop app for translating selected text with one global hotkey.

Select text in the app you are already using, press the hotkey, and R2Trans sends the text to the OpenAI Responses API, then replaces the selection with the translated result.

[한국어 README](README.md)

## Features

- Translates selected text with a Windows global hotkey.
- Supports directions such as `Korean -> English`, `English -> Korean`, `Japanese -> Korean`, and `Spanish -> English`.
- Supports auto-detect pairs for `Korean <-> English` and `Korean <-> Japanese`.
- Supports Natural, Formal, Polite, Overly Deferential, and Nyang style.
- Can show a confirmation window before replacing the selected text.
- Includes a Live Interpreter for microphone audio, system audio, or both.
- Provides a Windows tray icon, launch-at-login setting, and configurable global hotkey.

## Windows Port

- UI: WPF
- Global hotkey: Win32 `RegisterHotKey`
- Copy/paste automation: Win32 `SendInput`
- API key storage: Windows DPAPI, current-user scope
- Launch at login: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- System audio: WASAPI loopback through NAudio
- Installer: Inno Setup

This repository is Windows-only.

## Requirements

- Windows 10/11 x64
- .NET 8 SDK
- Inno Setup 6 when creating the installer

## Run for Development

```powershell
dotnet run --project Windows\R2Trans.Windows\R2Trans.Windows.csproj
```

The default hotkey is:

```text
control+alt+t
```

Hotkeys support `control`, `alt`, `shift`, `win`, letters, numbers, punctuation, and `space`.

## Build the Installer

Run from Windows PowerShell:

```powershell
Windows\build_installer.ps1
```

Build outputs:

```text
Windows\publish\win-x64
Windows\dist\R2TransSetup.exe
```

To publish the app without Inno Setup:

```powershell
Windows\build_installer.ps1 -SkipInstaller
```

## First-Time Setup

1. Launch `R2Trans.exe`.
2. Enter your own OpenAI API key in Settings.
3. Choose work mode, translation direction, auto-detect behavior, translation style, model, and hotkey.
4. Enable the `Launch at Login` checkbox if desired.
5. Select text in another app and press `control+alt+t`.

## Privacy and Security

- OpenAI API keys are entered by each user.
- API keys are encrypted with Windows DPAPI and stored at `%APPDATA%\R2Trans\openai-api-key.dat`.
- General settings are stored at `%APPDATA%\R2Trans\settings.json`.
- Selected text and Live Interpreter audio are sent to OpenAI to perform the requested translation.
- Do not commit API keys, local publish outputs, installers, or logs.

## License

R2Trans is released under the MIT License. See [LICENSE](LICENSE).
