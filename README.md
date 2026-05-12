# R2Trans

선택한 문장을 단축키 한 번으로 번역해 주는 Windows 전용 프로그램입니다.

메일, 메모장, 브라우저, 문서 편집기처럼 평소에 쓰던 앱에서 텍스트를 선택하고 `Ctrl + Alt + T`를 누르면, R2Trans가 선택한 문장을 번역한 뒤 그 자리에 바꿔 넣습니다.

[English README](README_en.md)

## 설치만 하려면

일반 사용자는 소스코드, .NET, Inno Setup, 개발 실행 명령을 알 필요가 없습니다.

필요한 파일은 하나입니다.

```text
R2TransSetup.exe
```

설치 방법:

1. `R2TransSetup.exe`를 실행합니다.
2. 설치 마법사에서 안내대로 진행합니다.
3. 설치가 끝나면 R2Trans를 실행합니다.
4. 설정 창에 본인의 OpenAI API 키를 입력합니다.
5. 사용할 번역 방향, 스타일, 단축키를 확인하고 저장합니다.

## 처음 사용하기

1. 번역하고 싶은 문장을 다른 앱에서 선택합니다.
2. 기본 단축키 `Ctrl + Alt + T`를 누릅니다.
3. R2Trans가 선택한 문장을 번역해서 원래 위치에 붙여넣습니다.

`Confirm Before Replace`를 켜두면 바꾸기 전에 번역 결과를 먼저 확인할 수 있습니다.

## 필요한 것

- Windows 10 또는 Windows 11
- 인터넷 연결
- 본인의 OpenAI API 키

OpenAI API 키는 앱에 포함되어 있지 않습니다. 사용자가 직접 입력해야 합니다.

## 주요 기능

- 선택한 텍스트를 Windows 전역 단축키로 바로 번역합니다.
- `한국어 -> 영어`, `영어 -> 한국어`, `일본어 -> 한국어`, `스페인어 -> 영어` 같은 번역 방향을 고를 수 있습니다.
- `한국어 <-> 영어`, `한국어 <-> 일본어` 자동 언어 감지를 지원합니다.
- Natural, Formal, Polite, Overly Deferential, Nyang style 번역 스타일을 지원합니다.
- 바꾸기 전에 번역 결과 확인 창을 띄울 수 있습니다.
- Live Interpreter로 마이크 오디오, 시스템 오디오, 또는 둘 다를 실시간 번역할 수 있습니다.
- 트레이 아이콘과 로그인 시 실행 옵션을 제공합니다.

## 자주 헷갈리는 것

`Inno Setup`은 뭔가요?

설치 파일을 만드는 개발자용 도구입니다. 이미 `R2TransSetup.exe`를 받았다면 필요 없습니다.

`개발 실행`은 뭔가요?

소스코드를 수정하는 개발자가 앱을 바로 실행해보는 방법입니다. 설치만 하는 사용자는 필요 없습니다.

`R2TransSetup.exe`가 없으면 어떻게 하나요?

설치 파일을 만든 사람에게 `R2TransSetup.exe`를 받아야 합니다. 이 파일이 최종 사용자용 설치 파일입니다.

## 설정 저장 위치

- API 키: `%APPDATA%\R2Trans\openai-api-key.dat`
- 일반 설정: `%APPDATA%\R2Trans\settings.json`

API 키는 Windows DPAPI로 암호화되어 현재 Windows 사용자 계정 기준으로 저장됩니다.

## 제거 방법

Windows 설정의 앱 목록에서 `R2Trans`를 제거하면 됩니다.

## 라이선스

R2Trans는 MIT License로 배포됩니다. 자세한 내용은 [LICENSE](LICENSE)를 확인해 주세요.
