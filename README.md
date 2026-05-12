# R2Trans

선택한 문장을 단축키 한 번으로 번역해 주는 Windows 전용 데스크톱 앱입니다.

메일, 메모장, 브라우저, 문서 편집기처럼 평소에 쓰던 앱에서 텍스트를 선택하고 단축키를 누르면, R2Trans가 선택한 문장을 OpenAI Responses API로 번역한 뒤 그 자리에 바꿔 넣습니다.

[English README](README_en.md)

## 기능

- 선택한 텍스트를 Windows 전역 단축키로 번역합니다.
- `한국어 -> 영어`, `영어 -> 한국어`, `일본어 -> 한국어`, `스페인어 -> 영어` 같은 번역 방향을 고를 수 있습니다.
- `한국어 <-> 영어`, `한국어 <-> 일본어` 자동 언어 감지를 지원합니다.
- Natural, Formal, Polite, Overly Deferential, Nyang style 번역 스타일을 지원합니다.
- 바꾸기 전에 번역 결과 확인 창을 띄울 수 있습니다.
- Live Interpreter로 마이크 오디오, 시스템 오디오, 또는 둘 다를 실시간 번역할 수 있습니다.
- 트레이 아이콘, 로그인 시 실행, 전역 단축키를 Windows 방식으로 제공합니다.

## Windows 이식 방식

- UI: WPF
- 전역 단축키: Win32 `RegisterHotKey`
- 복사/붙여넣기 자동화: Win32 `SendInput`
- API 키 저장: Windows DPAPI, 현재 사용자 범위
- 로그인 시 실행: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- 시스템 오디오: WASAPI loopback, NAudio
- 설치 파일: Inno Setup

이 저장소는 Windows 전용 구조입니다.

## 요구 사항

- Windows 10/11 x64
- .NET 8 SDK
- 설치 파일 생성 시 Inno Setup 6

## 개발 실행

```powershell
dotnet run --project Windows\R2Trans.Windows\R2Trans.Windows.csproj
```

기본 단축키는 다음과 같습니다.

```text
control+alt+t
```

단축키에는 `control`, `alt`, `shift`, `win`, 문자, 숫자, 구두점, `space`를 사용할 수 있습니다.

## 설치 파일 만들기

Windows PowerShell에서 실행합니다.

```powershell
Windows\build_installer.ps1
```

빌드 결과:

```text
Windows\publish\win-x64
Windows\dist\R2TransSetup.exe
```

Inno Setup 없이 앱 publish만 만들려면:

```powershell
Windows\build_installer.ps1 -SkipInstaller
```

## 처음 설정하기

1. `R2Trans.exe`를 실행합니다.
2. 설정 창에서 본인의 OpenAI API 키를 입력합니다.
3. 작업 모드, 번역 방향, 자동 감지, 번역 스타일, 모델, 단축키를 고릅니다.
4. 필요하면 `Launch at Login` 체크박스를 켭니다.
5. 다른 앱에서 텍스트를 선택한 뒤 `control+alt+t`를 누릅니다.

## 개인정보와 보안

- OpenAI API 키는 사용자가 직접 입력합니다.
- API 키는 Windows DPAPI로 암호화되어 `%APPDATA%\R2Trans\openai-api-key.dat`에 저장됩니다.
- 일반 설정은 `%APPDATA%\R2Trans\settings.json`에 저장됩니다.
- 번역할 텍스트와 Live Interpreter 오디오는 요청한 번역을 수행하기 위해 OpenAI로 전송됩니다.
- API 키, 로컬 publish 결과물, 설치 파일, 로그는 커밋하지 않는 것을 권장합니다.

## 라이선스

R2Trans는 MIT License로 배포됩니다. 자세한 내용은 [LICENSE](LICENSE)를 확인해 주세요.
