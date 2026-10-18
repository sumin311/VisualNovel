# VisualNovel — 승주 공략 연애 시뮬레이션

친구들끼리 가지고 놀 용도의 Unity 2D 연애 시뮬레이션. 출시 계획 없음.
사용자는 한국어로 소통하고, 코드보다 스토리/그림 작업에 관심이 많음. 설명은 쉬운 한국어로.

## 환경

- **Unity 2022.3.62f3** (다른 버전으로 열지 말 것)
- 저장소: https://github.com/sumin311/VisualNovel (공개). 사용자는 GitHub Desktop으로 commit/push/pull 함
- Git이 PATH에 없을 수 있음. GitHub Desktop 내장 git: `%LOCALAPPDATA%\GitHubDesktop\app-*\resources\app\git\cmd\git.exe`
- 패키지: uGUI(Legacy `UnityEngine.UI.Text`), TextMeshPro 미사용. 폰트: `Assets/Fonts/` — UI는 Griun_Yeeun-Rg.ttf (`VNEngine.uiFont`), 대사·로그는 NanumBarunpenB.ttf (`VNEngine.dialogueFont`, `VNDialogueFont` 표시 붙은 Text). 비면 OS 폰트로 대체. 두 폰트 모두 저장소에 포함 (그리운 예은체: 사용 범위 제한 없음, 폰트 파일 개작·수정·판매만 금지 — 파일을 수정하지 말 것)
- 플랫폼: PC(윈도우) + **웹(WebGL) — 친구들이 아이폰이라 웹 링크로 배포**. **main에 Push하면 GitHub Actions(`.github/workflows/webgl.yml`, game-ci/unity-builder, `VNBuildTools.BuildWebGLBatch`)가 자동 빌드 → `Builds/WebGL`을 GitHub Pages(Source: GitHub Actions)에 배포** https://sumin311.github.io/VisualNovel/ . 시크릿 UNITY_LICENSE/UNITY_EMAIL/UNITY_PASSWORD 필요(사용자가 직접 등록). `docs/`는 예전 수동 방식(`VN > 웹 버전 빌드`가 복사)의 잔재로, 자동 배포가 확인되면 지워도 됨 (템플릿 `Assets/WebGLTemplates/VN`, Gzip+decompressionFallback). 웹 세이브는 PlayerPrefs. 폰 대응: `VNPlatformAdjust`(폰 확대/숨김), `VNSafeArea`, 퀵메뉴 "숨기기" 버튼. **세로 화면 지원 (사용자 요청)**: 화면이 세로로 길면 `VNOrientation`(실행 중 VNEngine.Start 에서 붙음)이 세로 배치로 바꾸고 가로면 원래대로 되돌림. 씬은 가로(1920x1080) 배치 그대로, 세로 기준은 가로 900·세로 1600 이상(CanvasScaler Expand). 세로에선 퀵메뉴 맨 위 한 줄, 날짜/스탯 그 아래, 대사창 아래쪽, 캐릭터 크게, 메뉴 창 좁고 길게 + 탭은 창 아래 한 줄. 배경은 비율 유지로 꽉 채움(잘림), CG 는 잘리지 않게 가운데 띠 + 뒤에 같은 그림을 흐리게(`VNStage.SetBackdrop`). 예전 "가로로 돌려 주세요" 안내(VNRotateHint)는 삭제됨. 갤러리 CG 목록은 스크롤됨(`VNGallery.MakeScrollable`)

## GitHub · 웹 배포 메모 (PC 여러 대에서 이어 작업할 때 읽기)

**GitHub 관련 설정·절차가 바뀌면 이 섹션을 바로 고치고, 업데이트 기록에 한 줄 추가할 것** (사용자 요청). 메모리 폴더는 PC마다 따로라서 이 파일에 적어야 다른 PC에서도 보임. 시각은 서울 기준(KST)으로 적기.

- 사용자 PC 2대(집/다른 곳)에서 번갈아 작업. 둘 다 프로젝트 경로는 `C:\UnityProjects\VisualNovel`
- **작업 순서**: 시작할 때 GitHub Desktop에서 `Fetch origin` → `Pull origin` (Unity 열기 전에) → 작업 → Commit → `Push origin`
- **Claude는 commit까지만** 하고, push는 사용자가 GitHub Desktop에서 직접 누름 (Claude 쪽 git push는 인증이 없어 실패하고, 저장된 자격 증명을 건드리면 안 됨). commit 후엔 "GitHub Desktop에서 Push origin 눌러 주세요"라고 안내
- **자동 빌드**: main에 push → GitHub Actions "웹 버전 자동 빌드"(`.github/workflows/webgl.yml`) 약 10분 → GitHub Pages 배포. `docs/**`, `*.md`, `Tools/**`만 바뀐 push는 빌드 안 함 (Actions 탭에서 `Run workflow`로 수동 실행 가능)
  - 진행 확인(로그인 불필요): `https://api.github.com/repos/sumin311/VisualNovel/actions/runs` (status/conclusion), 실패 시 `.../actions/runs/{id}/jobs`에서 실패한 단계 확인
  - 웹이 안 바뀐 것 같으면 Ctrl+F5 (폰은 브라우저 재시작)
- **저장소 설정 (완료됨)**: 공개 저장소 / Settings → Pages → Source = **GitHub Actions** / Settings → Secrets → Actions에 `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` 등록됨 (사용자가 직접 입력. Claude는 비밀번호·라이선스 내용을 어디에도 입력하지 말 것)
  - `UNITY_LICENSE` = 개인 라이선스 파일 `C:\ProgramData\Unity\Unity_lic.ulf` 전체 내용 (ProgramData는 숨김 폴더). Unity 라이선스가 갱신·만료되면 빌드가 라이선스 오류로 실패 → 새 .ulf 내용으로 시크릿 교체
  - 새 PC에서 배치 모드 검사를 하려면 그 PC의 Unity Hub에서도 개인 라이선스를 받아야 함 (없으면 exit 199)
- `.gitignore`: Library/Temp/Builds 등 제외. 폰트 2개는 저장소에 포함

### 업데이트 기록 (KST)

- 2026-10-12 저장소 생성, 공개 전환, 모드 2개·7일 뼈대
- 2026-10-13 01:02 웹 버전(docs/ 수동 방식) 추가 / 13:08 그리운 예은체 폰트 저장소에 추가
- 2026-10-13 14:58 GitHub Actions 자동 빌드 도입 (첫 빌드 성공 15:10) / 15:11 빠른 저장·불러오기 버튼 제거
- 2026-10-13 15:53~16:06 등장인물 성별 선택, 남자 스탠딩·CG, 공주님/왕자님 자동 변환 → 16:16 웹 반영
- 2026-10-14 폰 세로 화면 지원(VNOrientation), 서연(포니테일·당근 인형)·기주(귀티 나는 아가씨) 설정 변경 + 임시 그림 다시 그림
- 2026-10-18 기주 스탠딩 4장을 친구가 그린 진짜 그림으로 교체 (기본/윙크→웃음/홍조→부끄럼/화남, 1200x2700 투명 PNG). 남자 기주도 같은 그림, 기주 설정을 그림 기준(갈색 똥머리·분홍 이어폰·팔짱)으로 변경
- 2026-10-18 기주 CG(엔딩·음악실·하렘 엔딩 속 기주)도 친구 그림으로 교체 (임시 배경 위에 합성)

## 게임 설정 (사용자가 정한 것 — 바꾸지 말 것)

- 장르: 여주인공 **승주**를 중심으로 한 연애 시뮬레이션. 임시 제목 **모두의 승주**
- 모드 2개 (타이틀 "새로 시작" → 모드 선택), 각각 7일 (축제 D-6 → 축제 당일)
  - **승주를 꼬시기**: 플레이어 '나'가 승주 한 명을 공략 (`start.txt`)
  - **승주가 되기**(하렘): 플레이어가 승주. 승현·서연·기주·수민 넷이 승주를 쟁탈하고, 승주가 누구를 받아줄지 고름 (`harem_start.txt`). 이 모드에 '나'는 등장 안 함. 승주 그림은 화면에 안 띄움
- 등장인물: 모두 같은 반 친구 "6인방". 눈은 전부 검은색, 옷은 기본 교복 바탕. 머리핀·리본·땋은 머리 없음
  - **나**: 플레이어. 이름은 '나' 고정. 성별은 게임 시작 시 선택(변수 `성별`: 1=남, 2=여)
  - **승주**: 여주인공. **시크하고 도도한 공주님**. 차갑게 말하지만 행동은 다정함. 흑발 앞머리 없는 장단발~장발 생머리, 교복 위 블레이저(마이)
  - **승현**: 활발한 분위기 메이커. 흑발 장단발 아주 약한 펌, 앞머리
  - **서연**: 겉보기엔 도도한데 알고 보면 웃긴 반전 매력. 금발 포니테일(살짝 웨이브), 앞머리, **당근 인형**을 늘 들고 다니고 짙은 남색 담요를 두름
  - **기주**: 부잣집 아가씨처럼 귀티 나는 외모, 성격은 장난꾸러기, 음악 얘기엔 진지. **친구가 그린 진짜 그림 기준 (2026-10-18)**: 짙은 갈색 머리를 뒤로 낮게 묶은 똥머리 + 잔머리, 옆으로 흐르는 앞머리, 눈 밑 점, 분홍 줄 이어폰(한쪽 귀), 남색 블레이저 + 더블 조끼 + 줄무늬 넥타이, 이름표, 팔짱. 남자 기주도 같은 그림
  - **수민**: 엉뚱한 4차원. 흑발 풀뱅 포니테일 장발, 회색 후드집업, 핸드폰을 들고 다님 (사진 좋아하는 설정은 폐기)
  - 친구 넷은 모두 여자
- 승주를 좋아하게 된 계기: 지난 겨울 소나기 날 승주가 우산을 주고 자기는 비를 맞고 감 (프롤로그, CG `승주_우산`)
- 이름표 색: 승주 #E0566B, 승현 #FFA45C, 서연 #7FA8FF, 기주 #B887FF, 수민 #6FD6B0, 나 #FFE08F
- 스탠딩 그림은 전부 **왼쪽을 보는 비스듬한 각도**로 그림 (게임에서 주변 인물이 좌우반전되기 때문)

## 구조

```
Assets/Resources/Story/      스토리 .txt (사용자가 직접 수정함)
  start.txt  [꼬시기] 성별 선택 + 프롤로그 → day1 … day7(축제) → ending
  harem_start.txt  [승주가 되기] → harem_day1 … harem_day7(축제) → harem_ending
Assets/Resources/Backgrounds/  배경 (파일명 = @bg 이름)
Assets/Resources/Characters/   스탠딩 "이름_표정.png" (표정: 기본/웃음/화남/부끄럼)
Assets/Resources/CG/           CG 11장 (승주_우산, 승주_고백, 승현/서연/기주/수민/하렘_엔딩, 친구별 특별 장면: 승현_농구·서연_도서관·기주_음악실·수민_편의점 — harem_day1 방과후)
Assets/Resources/Audio/BGM, SE  (아직 비어 있음 → 검사 시 BGM/SE 없음 경고는 정상)
Assets/VN/Scripts/Runtime/     엔진 (VNEngine, VNScript 파서, VNStage, VNAffectionHUD, VNGallery 등)
Assets/VN/Scripts/Editor/      씬 생성기(VNSceneBuilder), 스토리 검사기, 텍스처 자동 임포트
Assets/Scenes/Main.unity       유일한 씬. 전체 UI가 VNSceneBuilder로 생성됨
스토리_작성_가이드.md           사용자용 스토리 문법 설명서 (명령어 추가 시 함께 갱신)
캐릭터_프롬프트.md              그림 AI용 캐릭터/CG 프롬프트 (설정 바뀌면 함께 갱신)
```

- 이미지는 현재 전부 코드로 그린 **임시 이미지**. 사용자가 AI로 뽑은 그림으로 같은 파일명에 덮어씀
- **진짜 그림 (덮어쓰기 절대 금지)**: `Characters/기주_기본·웃음·부끄럼·화남.png`, `CG/기주_엔딩.png`(밤 옥상 + 기주_부끄럼), `CG/기주_음악실.png`(음악실 + 기주_웃음=윙크), `CG/하렘_엔딩(_남).png` 속 기주(기주_웃음) — 모두 임시 배경 위에 PlaceholderArt `DrawRealArt`로 합성. 기주 단독 CG는 남자 버전 없음 = 남자 기주도 같은 CG (친구가 그린 그림, 2026-10-18). 정면 보기·가슴에 "기주" 이름표가 그려져 있어 좌우반전 시 글씨가 뒤집히지만 **사용자가 괜찮다고 함**. 남자 기주도 이 그림을 그대로 씀(`기주_남_*` 삭제됨, PlaceholderArt 의 `RealStanding` 에 등록해 다시 안 만듦). 기주 설정·프롬프트·스토리 메모는 이 그림 기준으로 맞춤
- 임시 스탠딩·CG 생성기: `Tools/PlaceholderArt/` (C# 5, System.Drawing, `run.ps1 [이름 일부]`로 실행). **같은 이름 파일을 덮어쓰니 사용자가 진짜 그림을 넣은 뒤에는 실행 금지.** .cs는 UTF-8 BOM 필수. 캐릭터는 800x1400 왼쪽 보기, 머리는 (400,612) 기준 0.86배
- **스토리는 현재 뼈대 상태** (사용자 선택). 선택지·호감도·분기는 완성, 대사는 `[작성]` 표시 줄로 비워 둠 → 사용자가 직접 채움. 검사기가 `[작성]` 개수를 셈. 사용자가 요청하지 않으면 대사를 대신 채우지 말 것
- 모드 목록은 `VNEngine.modes` (이름/설명/첫 파일). 세이브에 모드 이름·첫 파일·게이지 목록·화자도 저장됨
- 스탠딩 배치 (사용자 요청): 말하는 사람 = 오른쪽에 크게, 나머지 = 왼쪽에 좌우반전 + 어둡게 (`VNStage.Arrange`). 나레이션/무대 밖 화자는 배치 유지. 아무도 안 말했으면 `@show` 위치
- 퀵 메뉴 버튼은 화면 왼쪽 위 (사용자 요청). 오른쪽 위는 날짜/스탯창
- **UI 디자인 (사용자가 레퍼런스 이미지로 지정, 2차)**: 파스텔 미연시 풍. **색 차이가 큰 그라데이션 절대 금지** (단색 위주)
  - 팔레트: 크림 창 #FFF9F5, 안쪽 영역 #F5EAE3, 갈색 글자 #4A2C2A / #8C6F68, 분홍 #F27BA3 / #E0628F, 연분홍 #FFE6EF, 라벤더 #B9A7E8 / #7A64B4
  - 버튼: 흰 둥근 사각 + 옅은 그림자 + 갈색 굵은 글씨 ("Make" 버튼 스타일). 마우스 올리면 분홍 채움 + 흰 글씨, 타이틀/선택지는 양옆 » « 표시
  - 메뉴 창(저장/불러오기/로그/갤러리/설정): 크림색 둥근 창, 왼쪽 위 "| 제목" + 설명, 오른쪽 위 갈색 원형 X 닫기, 창 왼쪽 아이콘 탭(`VNMenuTabs`, 선택된 탭은 갈색 바탕). 탭/퀵메뉴는 `VNEngine.OpenMenu` 로 열기. 타이틀 화면에선 저장·로그 탭 숨김
  - 타이틀: 연분홍 바탕(타이틀.png, 벚꽃 가지·꽃잎) + 왼쪽 캐릭터 3명(`VNTitleMenu.titleCharacters`) + 오른쪽 분홍 큰 제목(흰 외곽선) "모두의 승주", 부제 "~ 폭풍을 부르는 승주 ~", 아래 흰 버튼 5개
  - 대사창: 크림색 반투명 + 안쪽 분홍 얇은 테두리, 이름표는 캐릭터 @color 알약 + 흰 테두리
  - 도형/아이콘 스프라이트(`Assets/VN/UI/*.png`: pill, pill_line_thin, arrow, heart, sparkle, xmark, icon_*)는 VNSceneBuilder가 없으면 거리함수로 자동 생성. 모서리는 `Image.pixelsPerUnitMultiplier = 63 / 반지름`
  - 부품: `VNPillButton`(마우스 올림), `VNSwitch`(스위치), `VNTintFollower`(탭 글자색)
  - 임시 배경(교실/복도/옥상/하교길)은 System.Drawing으로 그린 평면 일러스트
- 호감도 UI (사용자 요청): 오른쪽 위. 날짜 칩은 항상, 스탯창은 퀵메뉴 "스탯"/S 키로 토글(PlayerPrefs 기억) + 호감도 변화 시 잠깐 표시
- 호감도: `@gauge 이름…`으로 표시할 게이지를 정함. 게이지 이름+`호감도` 변수 (기본은 `승주호감도` 하나)
  - 꼬시기 모드: 판정은 day7.txt 끝. 해피 ≥80 / 노멀 ≥50 / 배드
  - 하렘 모드: `@gauge 승현 서연 기주 수민`. 넷 다 ≥55 하렘 / 1등 ≥80 그 친구 엔딩 / 솔로
- 갤러리 (타이틀 "갤러리"): `@cg` 로 본 CG, `@ending 이름` 으로 본 엔딩이 해금 (`VNUnlocks`, PlayerPrefs). CG 목록은 CG 폴더 전체, 엔딩 목록은 스토리 속 `@ending` 전체를 자동 수집
- 대사 `{남자일때|여자일때}` → '나' 성별(하렘 모드에선 승주 성별), `{승주:남|여}` → 해당 인물 성별. `{변수}` → 값
- **모든 등장인물 성별 선택 (사용자 요청)**: 새로 시작 → 모드 선택 → 성별 선택 창(`VNGenderSelect`, 실행 중 모드 선택 창을 복제해 생성 — 씬 재생성 불필요). 변수: 나=`성별`, 나머지=`이름성별` (1=남, 2=여; 미설정이면 등장인물은 여, 나는 남). PlayerPrefs에 모드별로 기억. 모드의 등장인물 목록은 `VNGameMode.cast`(비면 자동: harem은 '나' 제외)
  - 남자 그림: 스탠딩 `이름_남_표정.png`(없으면 여자 그림), CG `이름_xx_남.png`(`VNEngine.ResolveCG`, CG 이름 앞부분이 등장인물이면 그 사람 성별로, 하렘_엔딩은 친구 넷 중 셋 이상 남자면 `_남`). 이름은 성별과 상관없이 그대로
  - start.txt 의 예전 성별 @choice 는 제거됨

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
