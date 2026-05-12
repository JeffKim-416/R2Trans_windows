# R2Trans

R2Trans is a Windows-only app that translates selected text with a global hotkey and replaces it in place.

[Korean README](README.md)

## Installer

Download the latest setup file from [GitHub Releases](https://github.com/JeffKim-416/R2Trans_windows/releases/latest).

```text
R2TransSetup-0.1.0-win-x64.exe
```

Install steps:

1. Run `R2TransSetup-0.1.0-win-x64.exe`.
2. Follow the installer prompts.
3. Launch R2Trans after installation.
4. Enter your own OpenAI API key in Settings.
5. Check the translation direction, style, and hotkey, then save.

## How To Use

1. Select text in another app, such as Notepad, a browser, or a document editor.
2. Press the default hotkey: `Ctrl + Alt + T`.
3. A translation progress popup appears.
4. When translation finishes, the selected text is replaced with the translated text.

Enable `Confirm Before Replace` if you want to review the translation before replacing the selected text.

## Features

- Translates selected text with a Windows global hotkey.
- Supports Korean, English, Japanese, Spanish, and Chinese translation.
- Supports auto-detect pairs for Korean/English and Korean/Japanese.
- Supports Natural, Formal, Polite, Overly Deferential, and Nyang style.
- Can replace selected text after confirmation or copy the translation.
- Includes a Live Interpreter for microphone audio, system audio, or both.
- Provides a tray icon and launch-at-login option.

## Uninstall

Remove `R2Trans` from the Windows Settings app list.

## License

R2Trans is released under the MIT License. See [LICENSE](LICENSE).
