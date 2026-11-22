# Kakao Auto Animated image play

카카오톡 Windows 채팅창에서 GIF와 움직이는 WebP를 자동 다운로드하고 반복재생하는 개인용 실험 도우미.

## 기능

- 새 원본 자동 다운로드와 기존 GIF/WebP 보충
- 최대 4건 동시 다운로드 요청, 여러 움짤 재생
- 화면에 보이는 이미지 중심 프레임 처리
- 카톡/채팅창 재열기 감지, 로그인 자동 시작 설정
- 단일 EXE 패키징, 실행 상태 UI, 로컬 진단 ZIP
- V8 및 CModule 픽셀 연산, 자체 갱신 간격 제한, 채팅 영역 갱신 최적화

## 개발 환경

Windows x64, Python 3.11 x64, PowerShell 7. JavaScript 단위 검사는 Node.js를 사용한다. 런처 빌드는 Windows .NET Framework C# 컴파일러를 사용한다.

```powershell
pwsh -NoProfile -File tools/prepare-dev.ps1 -PythonPath "C:\Path\To\Python311\python.exe"
pwsh -NoProfile -File tools/build-kakao-exe.ps1 -Name KakaoAutoPlay -PythonPath "C:\Path\To\Python311\python.exe"
```

출력: `artifacts/KakaoAutoPlay.exe`. 실행 환경은 EXE에 포함되므로 사용자 PC에 Python/PowerShell을 별도로 설치할 필요가 없다. 처음 실행하면 `%LOCALAPPDATA%/KakaoAutoPlay/bundles/`에 런타임을 푼다.

## 테스트

```powershell
python tools/test-kakao-diagnostics.py
python tools/test-kakao-autoplay-service.py
python tools/test-kakao-gif-discovery.py
python tools/test-kakao-webp-frames.py
node tools/test-kakao-resolution.cjs
node tools/test-kakao-coarse-pixels.cjs
node tools/test-kakao-download-pump.cjs
```

네이티브 테스트는 `prepare-dev.ps1`로 만든 자체 x86 시험 프로그램을 사용한다. 실제 설치된 카톡을 시험 대상으로 사용하지 않는다.

```powershell
python tools/test-kakao-performance-native.py
python tools/test-kakao-resolution-native.py
python tools/test-kakao-refresh-native.py
```

## 실행 및 진단

이전 도우미를 중지한 뒤 새 EXE 하나만 실행한다. 문제가 생기면 UI의 진단 ZIP 저장 기능을 사용한다. 진단 파일을 이 저장소에 커밋하지 않는다.

## 프로그램 업데이트

v11.0.0부터 `업데이트 확인` 버튼으로 이 저장소의 최신 정식 GitHub Release를 확인한다. 새 버전이 있으면 버튼이 설치 버튼으로 바뀐다. 사용자가 설치를 선택하면 다운로드 크기와 GitHub 제공 SHA-256을 검증하고 기존 재생 작업을 정상 종료한 뒤 새 EXE를 실행한다. 기존 실행 파일은 덮어쓰지 않는다. 자동 시작 등록과 실행 중이던 재생은 새 번들로 이어받는다.

다운로드는 `%LOCALAPPDATA%/KakaoAutoPlay/updates/<버전>/KakaoAutoPlay.exe`에 저장된다. 네트워크 장애·검증 실패·정리 시간 초과 시 새 버전으로 전환하지 않는다. 새 버전 실행 후의 모든 장애를 자동 롤백하는 기능은 아직 없다. 구버전은 수동으로 다시 실행할 수 있다.

이전 v10 이하에는 업데이트 버튼이 없으므로 v11.0.0을 한 번 직접 받아 실행해야 한다. 소스 커밋만으로 업데이트가 배포되지는 않는다.

### 새 버전 배포하기

1. `version.json`의 버전을 올리고 변경 사항을 커밋·푸시한다.
2. 같은 버전 태그(예: `v11.1.0`)를 생성하여 푸시한다.
3. GitHub Actions의 **Build and publish release**를 실행하고 태그를 입력한다.

워크플로는 해당 태그를 체크아웃해 테스트·빌드하고 `KakaoAutoPlay.exe`와 `SHA256SUMS.txt`를 업로드한 뒤 정식 Release를 공개한다. 초안/사전 출시 버전은 프로그램의 업데이트 대상으로 사용하지 않는다. 실행 파일 이름은 반드시 `KakaoAutoPlay.exe`여야 한다.

## 제한과 저장소 범위

카카오 공식 클라이언트나 공식 기능이 아니다. 내부 함수/구조에 의존하므로 카카오톡 업데이트에 따라 깨질 수 있으며 모든 버전·DPI 환경에서의 동작을 보장하지 않는다. 상세 사항은 [구현 및 제한](tools/KAKAO-GIF-EXPERIMENT.md)을 참고한다.

이 저장소는 소스와 테스트만 관리한다. 받은 움짤, 대화 데이터, 로그, 실행 상태, 원본 조사 자료, Python 런타임 및 타사 패키지 바이너리는 제외한다. 빌드 준비 시 설치되는 Frida/Pillow/Python의 라이선스는 각 배포물에 포함된 것을 따른다.
