# VisualNovel — 승주 공략 연애 시뮬레이션

친구들끼리 가지고 놀 용도의 Unity 2D 연애 시뮬레이션. 출시 계획 없음.
사용자는 한국어로 소통하고, 코드보다 스토리/그림 작업에 관심이 많음. 설명은 쉬운 한국어로.

## 환경

- **Unity 2022.3.62f3** (다른 버전으로 열지 말 것)
- 저장소: https://github.com/sumin311/VisualNovel (공개). 사용자는 GitHub Desktop으로 commit/push/pull 함
- Git이 PATH에 없을 수 있음. GitHub Desktop 내장 git: `%LOCALAPPDATA%\GitHubDesktop\app-*\resources\app\git\cmd\git.exe`
- 패키지: uGUI(Legacy `UnityEngine.UI.Text`), TextMeshPro 미사용. 한글 폰트는 런타임에 OS 폰트(맑은 고딕) 사용, `VN` 오브젝트의 `Custom Font`로 교체 가능

## 게임 설정 (사용자가 정한 것 — 바꾸지 말 것)

- 장르: 플레이어가 여주인공 **승주** 한 명을 공략하는 연애 시뮬레이션
- 등장인물: 모두 같은 반 친구 "6인방"
  - **나**: 플레이어. 이름은 '나' 고정. 성별은 게임 시작 시 선택(변수 `성별`: 1=남, 2=여)
  - **승주**: 여주인공. 말수는 적지만 마음이 따뜻함. 짧고 조용한 말투
  - **승현, 서연, 기주, 수민**: 전부 활발한 친구들 (모두 여자로 설정됨)
- 승주를 좋아하게 된 계기: 지난 겨울 소나기 날 승주가 우산을 주고 자기는 비를 맞고 감 (프롤로그)
- 이름표 색: 승주 #E0566B, 승현 #FFA45C, 서연 #7FA8FF, 기주 #B887FF, 수민 #6FD6B0, 나 #FFE08F

## 구조

```
Assets/Resources/Story/      스토리 .txt (사용자가 직접 수정함)
  start.txt  성별 선택 + 프롤로그 → day1 → day2 → day3 → ending
Assets/Resources/Backgrounds/  배경 (파일명 = @bg 이름)
Assets/Resources/Characters/   스탠딩 "이름_표정.png" (표정: 기본/웃음/화남/부끄럼)
Assets/Resources/CG/           CG (승주_고백.png = 해피엔딩)
Assets/Resources/Audio/BGM, SE  (아직 비어 있음 → 검사 시 경고 4개는 정상)
Assets/VN/Scripts/Runtime/     엔진 (VNEngine, VNScript 파서, VNStage, VNAffectionHUD 등)
Assets/VN/Scripts/Editor/      씬 생성기(VNSceneBuilder), 스토리 검사기, 텍스처 자동 임포트
Assets/Scenes/Main.unity       유일한 씬. 전체 UI가 VNSceneBuilder로 생성됨
스토리_작성_가이드.md           사용자용 스토리 문법 설명서 (명령어 추가 시 함께 갱신)
캐릭터_프롬프트.md              그림 AI용 캐릭터 프롬프트 (설정 바뀌면 함께 갱신)
```

- 이미지는 현재 전부 코드로 그린 **임시 이미지**. 사용자가 AI로 뽑은 그림으로 같은 파일명에 덮어씀
- 호감도: 변수 `승주호감도`가 왼쪽 위 HUD 게이지에 표시됨. 엔딩 기준 해피 ≥60 / 노멀 ≥35 / 배드
- 대사 `{남자일때|여자일때}` → 성별에 맞게 치환. `{변수}` → 값

## 작업 규칙

- 스토리 문법/명령어를 바꾸면 `VNScript.KnownCommands`, `VNStoryValidator`, `스토리_작성_가이드.md`를 같이 고칠 것
- **`VN > 메인 씬 다시 생성`(VNSceneBuilder.Build)은 Main.unity를 덮어씀.** 사용자가 씬을 직접 수정했을 수 있으니, 실행 전 git diff/수정 여부를 확인하고 필요하면 물어볼 것. 새 UI가 필요하면 VNSceneBuilder에도 추가해서 재생성 가능하게 유지
- 테스트용 코드는 프로젝트에 남기지 말 것 (아래처럼 임시 복사본에서)

## 검증 방법

컴파일 + 스토리 검사 (Unity 에디터가 이 프로젝트를 열고 있으면 배치 모드 실행 불가):

```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -quit -projectPath "<프로젝트 경로>" -executeMethod VN.EditorTools.VNStoryValidator.ValidateQuiet -logFile "$env:TEMP\vn.log"
```

로그에서 `error CS` / `[VN` 줄 확인. 씬 재생성은 `-executeMethod VN.EditorTools.VNSceneBuilder.BuildBatch`.

화면 확인이 필요하면: 프로젝트를 `%TEMP%` 아래 짧은 경로로 복사(Library 제외) → manifest에 `com.unity.modules.screencapture` 추가 → 임시 MonoBehaviour(`-vnshot <폴더>` 인자로 자동 클릭·`ScreenCapture`)와 `BuildPipeline.BuildPlayer` 에디터 스크립트를 넣어 빌드 → 실행해 스크린샷 확인 → 복사본 삭제.
경로가 길면(260자 초과) 패키지 임포트가 실패하니 짧은 경로를 쓸 것.
