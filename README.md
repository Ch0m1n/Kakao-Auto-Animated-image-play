# Kakao Auto Animated image play

카카오톡 Windows 채팅창에서 GIF와 움직이는 WebP를 자동 다운로드하고 반복재생하는 개인용 실험 도우미.

## 기능

- 새 원본 자동 다운로드와 기존 GIF/WebP 보충
- 최대 4건 동시 다운로드 요청, 여러 움짤 재생
- 화면에 보이는 이미지 중심 프레임 처리
- 카톡/채팅창 재열기 감지, 로그인 자동 시작 설정
- 단일 EXE 패키징, 실행 상태 UI, 로컬 진단 ZIP
- V8 및 CModule 픽셀 연산, 자체 갱신 간격 제한, 채팅 영역 갱신 최적화

## 현재 지원 작업 범위

설치된 기존 32비트 `KakaoTalk.exe`와 Qt 64비트 `KakaoTalkUI.exe`가 대상이다. ZIP으로 제공된 구형 구조의 x64는 현재 작업 대상에서 제외했다.

Qt 64비트는 최대 4개 원본 다운로드 슬롯을 사용하며 같은 원본은 중복 요청하지 않는다. 한 요청이 늦어져도 완료된 슬롯을 다음 원본에 넘긴다. 일시 오류/60초 초과 요청은 간격을 두고 최대 3회 시도하며 만료 응답은 재시도하지 않는다. 화면 밖 원본은 요청을 중지한다. 이 재시도는 다운로드에만 적용되며 준비된 움짤 반복재생에는 시간 제한이 없다.

## 개발 환경

### 카카오톡 호환성

| 카톡 종류 | 원본 자동 다운로드 | 개별·묶음 재생 검증 |
| --- | --- | --- |
| 기존 32비트 `KakaoTalk.exe` | 검증된 내부 구조에서 지원 | 기존 재생 지원. 잘림 영역 매핑 검사 통과, 실제 묶음 전송 검증은 별도 필요 |
| 기존 64비트 `KakaoTalk.exe` | 미지원 | 자체 GDI 시험 프로그램에서 GIF/WebP 66건 통과, 실제 카톡 검증 필요 |
| Qt 64비트 `KakaoTalkUI.exe` 26.10.0.492 | 카톡의 캐시 다운로드 사용 | 실제 개별 GIF 2개, 새 WebP, 묶음 WebP 2개 동시 반복재생 확인 |

Qt 엔진은 개별 사진의 `details`와 묶음 사진의 `modelData`를 구분하며, 확장자가 비어 있어도 `isWebp`로 판별한다. 최대 8개를 연결하고 화면 밖의 재생 객체는 정리한다. 사진 파일 메시지·다른 카톡 버전·모든 묶음 배치가 검증됐다는 뜻은 아니다. 트레이, 종료 옵션, 자동 시작, 업데이트, 진단 ZIP은 공통 런처를 사용한다.

Qt 재생 객체는 도우미 종료 시 정리되며 비정상 종료 시 마지막 heartbeat 이후 6초 뒤 GUI가 처리 가능한 시점에 제거된다. 진단에는 프레임·반복·다운로드 상태 수치만 추가하고 원본 경로·대화 내용은 내보내지 않는다. 설치된 카톡 파일을 수정하지 않는다.

기존 x64 다운로드는 별도 구현이 필요하며 x86 함수 주소를 재사용하지 않는다. Windows/도우미 EXE의 비트 수와 카톡 프로세스의 비트 수는 별개다.

친구가 제공한 `KakaoAutoPlay-v8-4K-performance-changes.md`의 V8 렌더러, CModule 픽셀 추출·비교·해시, WeakMap 버퍼 재사용, 최소 34ms 자체 갱신 간격, 성능 진단 필드는 현재 소스에 통합되어 있다. 추가로 대화 영역만 갱신하는 처리가 포함되어 있다. EXE를 다시 빌드하면 이 소스가 번들에 포함되며, 오래된 v8 번들에 직접 적용하던 패치를 새 버전에 덮어쓸 필요는 없다. 4K 최적화와 x64 대응은 별도 변경이며, 실제 4K PC에서의 끊김 해소를 보장하지 않는다.

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
python tools/test-kakao-performance-native.py --x64
python tools/test-kakao-resolution-native.py --x64
python tools/test-kakao-refresh-native.py --x64
```

## 실행 및 진단

이전 도우미를 중지한 뒤 새 EXE 하나만 실행한다. 문제가 생기면 UI의 진단 ZIP 저장 기능을 사용한다. 진단 파일을 이 저장소에 커밋하지 않는다.

### 보낸 사람별 사진 칸 축소

**옵션**에서 채팅방과 보낸 사람을 지정하면 사진의 가로·세로와 채팅 칸을 함께 최대 64픽셀로 줄인다. 사진 비율을 유지하며 정사각형 사진은 64×64로 표시한다. 여러 사람은 쉼표로 구분하고 닉네임의 괄호 별칭은 생략할 수 있다. 마우스를 올릴 때만 사진을 보이게 하는 추가 옵션도 제공한다.

32비트 카카오톡 26.10.0.5364의 일반 사진 메시지가 대상이다. 발신자 이름이 화면에 한 번 표시된 사진을 추적한다. 이름이 화면 밖에 있는 상태에서 처음 표시된 사진은 적용이 지연될 수 있다. 묶음 사진 및 카카오 이모티콘 상품의 모든 형태는 별도 검증이 필요하다. 설정을 해제하거나 도우미를 정상 종료하면 살아 있는 사진 객체의 크기를 복원한다. 규칙은 로컬 설정에 저장한다.

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


### v11.1: 트레이와 종료 옵션

X 버튼은 기본으로 트레이에 숨기며 재생을 유지합니다. 트레이 아이콘을 더블클릭하거나 메뉴의 **열기**를 선택하면 돌아옵니다. **완전 종료**는 재생 작업도 종료하지만 로그인 자동 시작 등록은 유지합니다. 기존 **중지** 버튼은 자동 시작도 해제합니다.

상단 **옵션**에서 X 버튼을 트레이 숨김 또는 즉시 종료로 선택할 수 있습니다. 설정은 `%LOCALAPPDATA%/KakaoAutoPlay/settings.json`의 `closeToTray`에 저장되어 업데이트 후에도 유지됩니다. Windows 로그아웃/종료는 트레이로 숨기지 않습니다.

기존 KakaoAutoPlay 파이썬/번들 자동 시작 항목을 인식하면 새 EXE의 `--tray --resume`으로 이전합니다. 등록하지 않았던 자동 시작을 임의로 켜지는 않습니다. 알 수 없는 등록은 변경하지 않고 해당 명령을 표시합니다. 이전 명령은 번들의 `artifacts/kakao-internals/startup-migration.json`에 보관합니다. EXE를 옮기면 새 위치에서 다시 실행해 주세요.
