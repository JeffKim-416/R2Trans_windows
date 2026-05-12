# R2Trans

A Windows-only app for translating selected text with one global hotkey.

Select text in the app you are already using, press `Ctrl + Alt + T`, and R2Trans translates the selected text and replaces it in place.

[한국어 README](README.md)

## If You Only Want to Install

Regular users do not need the source code, .NET, Inno Setup, or development run commands.

You only need this file:

```text
R2TransSetup.exe
```

Install steps:

1. Run `R2TransSetup.exe`.
2. Follow the installer prompts.
3. Launch R2Trans after installation.
4. Enter your own OpenAI API key in Settings.
5. Check the translation direction, style, and hotkey, then save.

## First Use

1. Select text in another app.
2. Press the default hotkey: `Ctrl + Alt + T`.
3. R2Trans translates the selected text and pastes it back into the original location.

Enable `Confirm Before Replace` if you want to review the translation before replacing the selected text.

## Requirements

- Windows 10 or Windows 11
- Internet connection
- Your own OpenAI API key

The app does not include an OpenAI API key. Each user must enter their own key.

## Features

- Translates selected text with a Windows global hotkey.
- Supports directions such as `Korean -> English`, `English -> Korean`, `Japanese -> Korean`, and `Spanish -> English`.
- Supports auto-detect pairs for `Korean <-> English` and `Korean <-> Japanese`.
- Supports Natural, Formal, Polite, Overly Deferential, and Nyang style.
- Can show a confirmation window before replacing the selected text.
- Includes a Live Interpreter for microphone audio, system audio, or both.
- Provides a tray icon and launch-at-login option.

## Common Confusions

What is Inno Setup?

It is a developer tool for creating the installer. If you already have `R2TransSetup.exe`, you do not need it.

What does “run for development” mean?

It is for developers who are editing the source code and want to launch the app directly. Users who only install the app do not need it.

What if I do not have `R2TransSetup.exe`?

Ask the person distributing the app for `R2TransSetup.exe`. That is the installer intended for end users.

## Settings Storage

- API key: `%APPDATA%\R2Trans\openai-api-key.dat`
- General settings: `%APPDATA%\R2Trans\settings.json`

The API key is encrypted with Windows DPAPI and stored for the current Windows user account.

## Uninstall

Remove `R2Trans` from the Windows Settings app list.

## License

R2Trans is released under the MIT License. See [LICENSE](LICENSE).
