using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VN.EditorTools
{
    /// <summary>
    /// VN > 메인 씬 다시 생성 : 게임에 필요한 UI 전체를 Assets/Scenes/Main.unity 로 만들어 줍니다.
    /// 만들어진 뒤에는 씬에서 위치/색/크기를 자유롭게 수정해도 됩니다.
    /// 디자인: 파스텔 미연시 풍 — 크림색 창, 흰 둥근 버튼(마우스 올리면 분홍), 왼쪽 아이콘 탭, 둥근 X 닫기
    /// </summary>
    [InitializeOnLoad]
    public static class VNSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";
        const string UIDir = "Assets/VN/UI/";

        // ───────── 팔레트 (큰 그라데이션 없이 단색만) ─────────
        static readonly Color Cream = Hex("FFF9F5");
        static readonly Color Inner = Hex("F5EAE3");
        static readonly Color CardWhite = Color.white;
        static readonly Color Brown = Hex("4A2C2A");
        static readonly Color BrownSoft = Hex("8C6F68");
        static readonly Color Muted = Hex("B8A39B");
        static readonly Color Pink = Hex("F27BA3");
        static readonly Color PinkDeep = Hex("E0628F");
        static readonly Color PinkSoft = Hex("FFE6EF");
        static readonly Color Lavender = Hex("B9A7E8");
        static readonly Color LavenderDeep = Hex("7A64B4");
        static readonly Color Track = Hex("EEDDD5");
        static readonly Color TitleBg = Hex("FBE7EC");
        static readonly Color DimColor = new Color(0.22f, 0.12f, 0.14f, 0.45f);
        static readonly Color ShadowColor = new Color(0.35f, 0.18f, 0.2f, 0.16f);

        static Font font;
        static Font uiFontAsset, dialogueFontAsset;
        const string UIFontPath = "Assets/Fonts/Griun_Yeeun-Rg.ttf";
        const string DialogueFontPath = "Assets/Fonts/NanumBarunpenB.ttf";
        static Sprite pill, pillLine, arrow, heart, sparkle, xmark;
        static Sprite iconSettings, iconImage, iconFolder, iconLog;

        static VNSceneBuilder()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (File.Exists(ScenePath) || SessionState.GetBool("VN.SceneAsked", false)) return;
                SessionState.SetBool("VN.SceneAsked", true);
                if (EditorUtility.DisplayDialog("VN", "메인 씬(Assets/Scenes/Main.unity)이 없습니다.\n지금 자동으로 생성할까요?", "생성", "나중에"))
                    Build();
            };
        }

        [MenuItem("VN/메인 씬 다시 생성", priority = 0)]
        static void BuildMenu()
        {
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("VN",
                    "Main 씬을 새로 만들어 덮어씁니다.\n씬에서 직접 수정한 UI 변경사항은 사라집니다. 계속할까요?", "생성", "취소"))
                return;
            Build();
        }

        /// <summary>배치 모드용: Unity.exe -batchmode -executeMethod VN.EditorTools.VNSceneBuilder.BuildBatch</summary>
        public static void BuildBatch()
        {
            Build();
            var (errors, warnings) = VNStoryValidator.ValidateQuiet();
            Debug.Log($"[VN] BuildBatch 완료 (스토리 검사: 오류 {errors}, 경고 {warnings})");
        }

        public static void Build()
        {
            LoadResources();
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 카메라 & 이벤트 시스템
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0, 0, -10);
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // 엔진
            var vnGo = new GameObject("VN");
            var engine = vnGo.AddComponent<VNEngine>();
            engine.uiFont = uiFontAsset;
            engine.dialogueFont = dialogueFontAsset;
            var audio = vnGo.AddComponent<VNAudio>();
            audio.bgmSource = new GameObject("BGM").AddComponent<AudioSource>();
            audio.bgmSource.transform.SetParent(vnGo.transform);
            audio.bgmSource.playOnAwake = false;
            audio.bgmSource.loop = true;
            audio.seSource = new GameObject("SE").AddComponent<AudioSource>();
            audio.seSource.transform.SetParent(vnGo.transform);
            audio.seSource.playOnAwake = false;

            // 캔버스
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.transform;

            engine.stage = BuildStage(root);
            engine.audioPlayer = audio;
            BuildGameUI(root, engine);
            engine.titleMenu = BuildTitle(root);
            engine.saveLoadMenu = BuildSaveLoad(root);
            engine.settingsMenu = BuildSettings(root);
            engine.backlog = BuildBacklog(root);
            engine.gallery = BuildGallery(root);
            engine.confirmDialog = BuildConfirm(root);
            // 세로 화면 배치는 실행 중에 VNOrientation 이 맡음 (씬은 가로 배치로 만듦)

            engine.saveLoadMenu.gameObject.SetActive(false);
            engine.settingsMenu.gameObject.SetActive(false);
            engine.backlog.gameObject.SetActive(false);
            engine.gallery.gameObject.SetActive(false);
            engine.confirmDialog.gameObject.SetActive(false);
            engine.gameUI.SetActive(false);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            // 폰 앱으로 만들 때도 가로·세로 둘 다 (세로면 VNOrientation 이 세로 배치로 바꿈)
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            AssetDatabase.SaveAssets();
            Debug.Log("[VN] 메인 씬 생성 완료: " + ScenePath);
        }

        // ───────────────────────── 화면별 구성 ─────────────────────────

        static VNStage BuildStage(Transform root)
        {
            var rt = Child("Stage", root);
            Stretch(rt);
            var stage = rt.gameObject.AddComponent<VNStage>();

            Stretch(Img(Child("BlackBase", rt), Color.black).rectTransform);
            stage.backgroundBack = Img(Child("BackgroundBack", rt), Color.white);
            stage.backgroundFront = Img(Child("BackgroundFront", rt), Color.white);
            stage.characterLayer = Child("Characters", rt);
            stage.cgImage = Img(Child("CG", rt), Color.white);

            Stretch(stage.backgroundBack.rectTransform);
            Stretch(stage.backgroundFront.rectTransform);
            Stretch(stage.characterLayer);
            Stretch(stage.cgImage.rectTransform);
            stage.backgroundBack.enabled = stage.backgroundFront.enabled = stage.cgImage.enabled = false;
            return stage;
        }

        static void BuildGameUI(Transform root, VNEngine engine)
        {
            var gameUI = Child("GameUI", root);
            Stretch(gameUI);
            engine.gameUI = gameUI.gameObject;
            gameUI.gameObject.AddComponent<VNSafeArea>();

            // 화면 클릭 = 다음 대사
            var catcherImg = Img(Child("ClickCatcher", gameUI), new Color(0, 0, 0, 0));
            Stretch(catcherImg.rectTransform);
            catcherImg.raycastTarget = true;
            var catcher = catcherImg.gameObject.AddComponent<Button>();
            catcher.transition = Selectable.Transition.None;
            catcher.navigation = new Navigation { mode = Navigation.Mode.None };
            engine.clickCatcher = catcher;

            // 대사창 — 크림색 둥근 창 + 안쪽 분홍 테두리
            var boxRt = Child("DialogueBox", gameUI);
            boxRt.anchorMin = new Vector2(0, 0);
            boxRt.anchorMax = new Vector2(1, 0);
            boxRt.pivot = new Vector2(0.5f, 0);
            boxRt.offsetMin = new Vector2(90, 34);
            boxRt.offsetMax = new Vector2(-90, 300);
            var box = boxRt.gameObject.AddComponent<VNDialogueBox>();
            box.canvasGroup = boxRt.gameObject.AddComponent<CanvasGroup>();
            boxRt.gameObject.AddComponent<VNPlatformAdjust>().mobileTextScale = 1.12f;
            box.toggleOnColor = PinkDeep;
            box.toggleOffColor = Brown;
            box.darkText = Brown;
            box.defaultPlateColor = BrownSoft;
            engine.dialogue = box;

            var panel = Rounded(Child("Panel", boxRt), new Color(1f, 0.98f, 0.97f, 0.95f), 30);
            Stretch(panel.rectTransform);
            AddDropShadow(panel);
            var frame = Ring(Child("Frame", boxRt), new Color(Pink.r, Pink.g, Pink.b, 0.45f), 22);
            Stretch(frame.rectTransform, 10, 10, 10, 10);

            // 이름표 = 캐릭터 색 알약 + 흰 테두리 (글자 길이에 맞게 늘어남)
            var plate = Child("NamePlate", boxRt);
            plate.anchorMin = plate.anchorMax = new Vector2(0, 1);
            plate.pivot = new Vector2(0, 0.5f);
            plate.anchoredPosition = new Vector2(48, 0);
            plate.sizeDelta = new Vector2(220, 62);
            box.namePlateImage = Pill(plate, Pink, 62);
            AddDropShadow(box.namePlateImage);
            var plateRing = Pill(Child("Ring", plate), Color.white, 62, true);
            Stretch(plateRing.rectTransform);
            plateRing.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var hl = plate.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(46, 46, 4, 4);
            hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childControlWidth = hl.childControlHeight = true;
            hl.childForceExpandWidth = hl.childForceExpandHeight = true;
            var csf = plate.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            box.namePlate = plate.gameObject;
            box.nameText = Txt(plate, "Name", "이름", 30, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
            box.nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            box.nameText.gameObject.AddComponent<LayoutElement>().minWidth = 100;

            // 본문
            box.bodyText = Txt(boxRt, "Body", "여기에 대사가 표시됩니다.", 34, TextAnchor.UpperLeft, Brown);
            Stretch(box.bodyText.rectTransform, 84, 44, 84, 60);
            box.bodyText.lineSpacing = 1.3f;
            DialogueFont(box.bodyText);

            // 다음 대사 표시 = 분홍 동그라미 + 아래 화살표
            var next = Child("NextIndicator", boxRt);
            Place(next, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 32), new Vector2(38, 38));
            var nextImg = Pill(next, Pink, 38);
            var nextArrow = Img(Child("Arrow", next), Color.white, arrow);
            nextArrow.type = Image.Type.Simple;
            Stretch(nextArrow.rectTransform, 9, 9, 9, 9);
            nextArrow.rectTransform.localEulerAngles = new Vector3(0, 0, -90);
            box.nextIndicator = nextImg;

            // 퀵 메뉴 (화면 왼쪽 위 — 오른쪽 위는 날짜/스탯창 자리)
            var qm = Child("QuickMenu", gameUI);
            Place(qm, new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -28), new Vector2(1260, 46));
            box.quickMenu = qm.gameObject;
            var qhl = qm.gameObject.AddComponent<HorizontalLayoutGroup>();
            qhl.spacing = 8;
            qhl.childAlignment = TextAnchor.MiddleLeft;
            qhl.childControlWidth = qhl.childControlHeight = true;
            qhl.childForceExpandWidth = qhl.childForceExpandHeight = false;
            // 빠른 저장/불러오기 버튼은 사용자 요청으로 뺌 (상단 버튼이 너무 많음)
            box.saveButton = QuickButton(qm, "저장");
            box.loadButton = QuickButton(qm, "불러오기");
            box.logButton = QuickButton(qm, "로그");
            box.autoButton = QuickButton(qm, "오토");
            box.skipButton = QuickButton(qm, "스킵");
            box.statButton = QuickButton(qm, "스탯");
            box.configButton = QuickButton(qm, "설정");
            box.titleButton = QuickButton(qm, "타이틀");
            box.hideButton = QuickButton(qm, "숨기기");
            qm.gameObject.AddComponent<VNPlatformAdjust>().mobileScale = 1.2f;

            engine.hud = BuildHUD(gameUI);

            // 알림 문구 (저장 완료 등) — 흰 알약
            var noticeRt = Child("Notice", gameUI);
            Place(noticeRt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(540, 58));
            AddDropShadow(Pill(noticeRt, Color.white, 58));
            var notice = Txt(noticeRt, "Text", "", 26, TextAnchor.MiddleCenter, Brown, FontStyle.Bold);
            Stretch(notice.rectTransform);
            noticeRt.gameObject.SetActive(false);
            box.noticeText = notice;
            box.noticeRoot = noticeRt.gameObject;

            // 선택지 — 흰 둥근 버튼, 마우스 올리면 분홍 + 양옆 화살표
            var choiceRt = Child("ChoiceMenu", gameUI);
            Stretch(choiceRt, 0, 320, 0, 0);
            Img(choiceRt, new Color(0.22f, 0.12f, 0.14f, 0.25f)).raycastTarget = true;
            var choice = choiceRt.gameObject.AddComponent<VNChoiceMenu>();
            var options = Child("Options", choiceRt);
            Place(options, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(980, 640));
            var vl = options.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 16;
            vl.childAlignment = TextAnchor.MiddleCenter;
            vl.childControlWidth = vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            var template = WhiteButton(options, "OptionTemplate", "선택지", 30, 78, true);
            template.gameObject.AddComponent<LayoutElement>().preferredHeight = 78;
            template.gameObject.SetActive(false);
            choice.container = options;
            choice.optionTemplate = template;
            choiceRt.gameObject.SetActive(false);
            engine.choiceMenu = choice;
            qm.SetAsLastSibling(); // 선택지가 떠 있어도 저장 등을 누를 수 있게
        }

        static VNAffectionHUD BuildHUD(Transform parent)
        {
            var root = Child("AffectionHUD", parent);
            Stretch(root);
            var hud = root.gameObject.AddComponent<VNAffectionHUD>();
            var topRight = new Vector2(1, 1);

            // 날짜 (오른쪽 위 흰 알약, 항상 표시)
            var chip = Child("DayChip", root);
            Place(chip, topRight, topRight, new Vector2(-34, -28), new Vector2(460, 46));
            AddDropShadow(Pill(chip, new Color(1, 1, 1, 0.95f), 46));
            hud.dayChip = chip.gameObject;
            var dot = Img(Child("Heart", chip), Pink, heart);
            dot.type = Image.Type.Simple;
            Place(dot.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(24, 24));
            hud.dayText = Txt(chip, "Day", "1일차", 22, TextAnchor.MiddleRight, Brown, FontStyle.Bold);
            Stretch(hud.dayText.rectTransform, 50, 0, 24, 0);

            // 스탯창 — 게이지 1개
            var panel = Child("Panel", root);
            Place(panel, topRight, topRight, new Vector2(-34, -86), new Vector2(460, 104));
            AddDropShadow(Rounded(panel, new Color(1f, 0.98f, 0.97f, 0.96f), 22));
            hud.panel = panel.gameObject;

            hud.nameText = Txt(panel, "Name", "♥ 승주", 26, TextAnchor.MiddleLeft, PinkDeep, FontStyle.Bold);
            Place(hud.nameText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(26, -12), new Vector2(220, 38));

            hud.stageText = Txt(panel, "Stage", "그냥 친구", 20, TextAnchor.MiddleRight, BrownSoft);
            Place(hud.stageText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-26, -12), new Vector2(240, 38));

            var gaugeBg = Pill(Child("Gauge", panel), Track, 20);
            Place(gaugeBg.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(26, 22), new Vector2(290, 20));
            hud.gaugeFill = Pill(Child("Fill", gaugeBg.rectTransform), Pink, 20);
            var fillRt = hud.gaugeFill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0.2f, 1);
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

            hud.valueText = Txt(panel, "Value", "0 / 100", 20, TextAnchor.MiddleRight, Brown, FontStyle.Bold);
            Place(hud.valueText.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-26, 14), new Vector2(120, 34));

            // +10 알림은 스탯창 왼쪽에
            hud.deltaText = Txt(panel, "Delta", "♥ +10", 30, TextAnchor.MiddleRight, PinkDeep, FontStyle.Bold);
            Place(hud.deltaText.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(-18, 0), new Vector2(220, 50));
            AddOutline(hud.deltaText, Color.white, 2);
            hud.deltaText.gameObject.SetActive(false);

            panel.gameObject.SetActive(false);

            // 스탯창 — 게이지 여러 개 (@gauge 승현 서연 기주 수민). 줄 수에 맞게 패널이 늘어남
            var multi = Child("MultiPanel", root);
            multi.anchorMin = multi.anchorMax = multi.pivot = topRight;
            multi.anchoredPosition = new Vector2(-34, -86);
            multi.sizeDelta = new Vector2(460, 0);
            AddDropShadow(Rounded(multi, new Color(1f, 0.98f, 0.97f, 0.96f), 22));
            var mvl = multi.gameObject.AddComponent<VerticalLayoutGroup>();
            mvl.padding = new RectOffset(26, 26, 14, 14);
            mvl.spacing = 6;
            mvl.childControlWidth = mvl.childControlHeight = true;
            mvl.childForceExpandWidth = true;
            mvl.childForceExpandHeight = false;
            multi.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            hud.multiPanel = multi.gameObject;

            var row = Child("RowTemplate", multi);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;
            var rowName = Txt(row, "Name", "♥ 이름", 22, TextAnchor.MiddleLeft, PinkDeep, FontStyle.Bold);
            Place(rowName.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(118, 36));
            var rowGauge = Pill(Child("Gauge", row), Track, 16);
            Place(rowGauge.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, 0), new Vector2(232, 16));
            var rowFill = Pill(Child("Fill", rowGauge.rectTransform), Pink, 16);
            rowFill.rectTransform.anchorMin = Vector2.zero;
            rowFill.rectTransform.anchorMax = new Vector2(0.2f, 1);
            rowFill.rectTransform.offsetMin = rowFill.rectTransform.offsetMax = Vector2.zero;
            var rowValue = Txt(row, "Value", "0", 20, TextAnchor.MiddleRight, Brown, FontStyle.Bold);
            Place(rowValue.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(60, 36));
            var rowDelta = Txt(row, "Delta", "♥ +10", 24, TextAnchor.MiddleRight, PinkDeep, FontStyle.Bold);
            Place(rowDelta.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(-44, 0), new Vector2(200, 40));
            AddOutline(rowDelta, Color.white, 2);
            rowDelta.gameObject.SetActive(false);
            row.gameObject.SetActive(false);
            hud.rowTemplate = row.gameObject;

            multi.gameObject.SetActive(false);
            chip.gameObject.SetActive(false);
            return hud;
        }

        static VNTitleMenu BuildTitle(Transform root)
        {
            var rt = Child("TitleMenu", root);
            Stretch(rt);
            var title = rt.gameObject.AddComponent<VNTitleMenu>();
            Stretch(Img(Child("BaseColor", rt), TitleBg).rectTransform);
            title.background = Img(Child("Background", rt), Color.white);
            Stretch(title.background.rectTransform);

            // 왼쪽: 캐릭터 3명 (뒤 2명 → 가운데 앞 1명 순서로 그려야 겹침이 맞음)
            // 노치를 피하는 안전 영역 (바탕은 화면 전체, 글자/버튼은 안전 영역 안)
            var safe = Child("Safe", rt);
            Stretch(safe);
            safe.gameObject.AddComponent<VNSafeArea>();

            var chars = Child("Characters", safe);
            Stretch(chars);
            var slots = new Image[3];
            float[] xs = { 330, 600, 870 };
            float[] hs = { 900, 1000, 900 };
            foreach (int i in new[] { 0, 2, 1 })
            {
                var img = Img(Child("Character" + i, chars), Color.white);
                img.preserveAspect = true;
                Place(img.rectTransform, new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(xs[i], -60), new Vector2(hs[i] * 0.62f, hs[i]));
                if (i != 1) img.color = new Color(0.94f, 0.92f, 0.94f); // 뒤쪽은 살짝 톤 다운
                slots[i] = img;
            }
            title.characterSlots = slots;

            // 오른쪽: 제목 + 버튼 묶음
            var right = Child("Right", safe);
            Place(right, new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520, 0), new Vector2(820, 1000));

            // 장식: 하트, 반짝이
            Deco(right, heart, Pink, new Vector2(-300, 310), 46, -14);
            Deco(right, heart, Lavender, new Vector2(330, 360), 34, 12);
            Deco(right, heart, PinkSoft, new Vector2(345, 150), 54, 18);
            Deco(right, sparkle, Color.white, new Vector2(-250, 380), 44, 0);
            Deco(right, sparkle, Lavender, new Vector2(280, 250), 30, 0);
            Deco(right, sparkle, Color.white, new Vector2(-330, 170), 26, 0);

            title.titleText = Txt(right, "Title", "타이틀", 128, TextAnchor.MiddleCenter, PinkDeep, FontStyle.Bold);
            Place(title.titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 270), new Vector2(820, 170));
            title.titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            AddOutline(title.titleText, Color.white, 6);
            var ts = title.titleText.gameObject.AddComponent<Shadow>();
            ts.effectColor = new Color(PinkDeep.r, PinkDeep.g, PinkDeep.b, 0.3f);
            ts.effectDistance = new Vector2(0, -10);

            title.subtitleText = Txt(right, "Subtitle", "부제", 36, TextAnchor.MiddleCenter, LavenderDeep, FontStyle.Bold);
            Place(title.subtitleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(820, 56));
            AddOutline(title.subtitleText, Color.white, 3);

            var buttons = Child("Buttons", right);
            Place(buttons, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1), new Vector2(0, 70), new Vector2(420, 420));
            var vl = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 16;
            vl.childAlignment = TextAnchor.UpperCenter;
            vl.childControlWidth = vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            title.newGameButton = TitleButton(buttons, "새로 시작");
            title.loadButton = TitleButton(buttons, "불러오기");
            title.galleryButton = TitleButton(buttons, "갤러리");
            title.settingsButton = TitleButton(buttons, "환경 설정");
            title.quitButton = TitleButton(buttons, "종료");
            var quitAdj = title.quitButton.gameObject.AddComponent<VNPlatformAdjust>();
            quitAdj.hideOnMobile = quitAdj.hideOnWeb = true; // 폰·웹에서는 종료가 안 되므로 숨김

            var ver = Txt(safe, "Version", "v0.5", 22, TextAnchor.LowerRight, BrownSoft);
            Place(ver.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 20), new Vector2(300, 40));

            // 모드 선택 창 (새로 시작 → 모드 고르기). 카드는 VNEngine.modes 로 자동 생성
            var modeRt = Menu(rt, "ModeSelect", new Vector2(1100, 640), "모드 선택", "누구의 이야기로 시작할까요?",
                false, VNMenuKind.Load, out var content, out var close, out _, out _, out _);
            var list = Child("Modes", content);
            Stretch(list, 30, 30, 30, 30);
            var mvl = list.gameObject.AddComponent<VerticalLayoutGroup>();
            mvl.spacing = 22;
            mvl.childAlignment = TextAnchor.MiddleCenter;
            mvl.childControlWidth = mvl.childControlHeight = true;
            mvl.childForceExpandWidth = true;
            mvl.childForceExpandHeight = false;
            var modeBtn = CardButton(list, "ModeTemplate", "모드", 38, 24, Brown);
            var accent = Rounded(Child("Accent", modeBtn.transform), Pink, 7);
            accent.rectTransform.anchorMin = new Vector2(0, 0);
            accent.rectTransform.anchorMax = new Vector2(0, 1);
            accent.rectTransform.pivot = new Vector2(0, 0.5f);
            accent.rectTransform.offsetMin = new Vector2(24, 24);
            accent.rectTransform.offsetMax = new Vector2(38, -24);
            var modeLabel = modeBtn.GetComponentInChildren<Text>();
            modeLabel.alignment = TextAnchor.MiddleLeft;
            modeLabel.lineSpacing = 1.25f;
            Stretch(modeLabel.rectTransform, 66, 10, 40, 10);
            modeBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 140;
            modeBtn.gameObject.SetActive(false);
            title.modePanel = modeRt.gameObject;
            title.modeContainer = list;
            title.modeTemplate = modeBtn;
            title.modeColors = new[] { Pink, Lavender, Hex("8FCFB8"), Hex("F5C26B") };
            title.modeBackButton = close;
            modeRt.gameObject.SetActive(false);
            return title;
        }

        static VNSaveLoadMenu BuildSaveLoad(Transform root)
        {
            var rt = Menu(root, "SaveLoadMenu", new Vector2(1380, 800), "저장하기", "지금까지의 이야기를 저장합니다.",
                true, VNMenuKind.Save, out var content, out var close, out var header, out var desc, out var tabs);
            var menu = rt.gameObject.AddComponent<VNSaveLoadMenu>();
            menu.headerText = header;
            menu.descText = desc;
            menu.tabs = tabs;

            var grid = Child("Slots", content);
            Stretch(grid, 24, 24, 24, 24);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(610, 124);
            g.spacing = new Vector2(20, 16);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 2;
            g.childAlignment = TextAnchor.UpperCenter;

            var slot = CardButton(grid, "SlotTemplate", "No.1", 26, 20, Brown);
            var label = slot.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            Stretch(label.rectTransform, 34, 14, 30, 14);
            label.lineSpacing = 1.25f;
            slot.gameObject.SetActive(false);
            menu.slotTemplate = slot;
            menu.slotContainer = grid;

            menu.closeButton = close;
            return menu;
        }

        static VNSettingsMenu BuildSettings(Transform root)
        {
            var rt = Menu(root, "SettingsMenu", new Vector2(1380, 800), "환경 설정", "게임 환경을 설정할 수 있습니다.",
                true, VNMenuKind.Settings, out var content, out var close, out _, out _, out _);
            var menu = rt.gameObject.AddComponent<VNSettingsMenu>();

            menu.textSpeedSlider = SliderRow(content, "텍스트 속도", -50, 10f, 150f, false, out menu.textSpeedValue);
            menu.autoDelaySlider = SliderRow(content, "오토 대기 시간", -150, 0.5f, 5f, false, out menu.autoDelayValue);
            menu.bgmSlider = SliderRow(content, "배경음 볼륨", -250, 0f, 1f, false, out menu.bgmValue);
            menu.seSlider = SliderRow(content, "효과음 볼륨", -350, 0f, 1f, false, out menu.seValue);
            menu.fullscreenToggle = ToggleRow(content, "전체 화면", -450);
            menu.fullscreenToggle.gameObject.AddComponent<VNPlatformAdjust>().hideOnMobile = true;

            menu.closeButton = close;
            return menu;
        }

        static VNBacklog BuildBacklog(Transform root)
        {
            var rt = Menu(root, "Backlog", new Vector2(1380, 800), "대사 로그", "지나간 대사를 다시 볼 수 있습니다.",
                true, VNMenuKind.Log, out var content, out var close, out _, out _, out _);
            var log = rt.gameObject.AddComponent<VNBacklog>();

            // 스크롤 영역
            var sv = Child("Scroll", content);
            Stretch(sv, 10, 10, 10, 10);
            var srImg = Img(sv, new Color(0, 0, 0, 0));
            srImg.raycastTarget = true;
            var sr = sv.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 40;

            var viewport = Child("Viewport", sv);
            Stretch(viewport, 10, 10, 34, 10);
            viewport.gameObject.AddComponent<RectMask2D>();
            sr.viewport = viewport;

            var contentRt = Child("Content", viewport);
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.offsetMin = contentRt.offsetMax = Vector2.zero;
            var vl = contentRt.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(26, 26, 18, 18);
            vl.spacing = 26;
            vl.childControlWidth = vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            contentRt.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = contentRt;

            // 스크롤바
            var bar = Child("Scrollbar", sv);
            bar.anchorMin = new Vector2(1, 0);
            bar.anchorMax = new Vector2(1, 1);
            bar.pivot = new Vector2(1, 0.5f);
            bar.offsetMin = new Vector2(-20, 14);
            bar.offsetMax = new Vector2(-8, -14);
            Pill(bar, Track, 12).raycastTarget = true;
            var sb = bar.gameObject.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;
            var area = Child("Sliding Area", bar);
            Stretch(area);
            var handle = Pill(Child("Handle", area), Pink, 12);
            handle.raycastTarget = true;
            handle.rectTransform.offsetMin = handle.rectTransform.offsetMax = Vector2.zero;
            sb.handleRect = handle.rectTransform;
            sb.targetGraphic = handle;
            sr.verticalScrollbar = sb;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            var entry = Txt(contentRt, "EntryTemplate", "대사", 28, TextAnchor.UpperLeft, Brown);
            entry.lineSpacing = 1.3f;
            DialogueFont(entry);
            entry.gameObject.SetActive(false);
            log.entryTemplate = entry;
            log.content = contentRt;
            log.scrollRect = sr;

            log.closeButton = close;
            return log;
        }

        static VNGallery BuildGallery(Transform root)
        {
            var rt = Menu(root, "Gallery", new Vector2(1380, 800), "갤러리", "지금까지 모은 CG와 엔딩을 볼 수 있습니다.",
                true, VNMenuKind.Gallery, out var content, out var close, out _, out _, out _);
            var gal = rt.gameObject.AddComponent<VNGallery>();

            // 탭 (CG / 엔딩) + 개수
            var tabs = Child("Tabs", content);
            Place(tabs, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -20), new Vector2(300, 50));
            var thl = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            thl.spacing = 12;
            thl.childControlWidth = thl.childControlHeight = true;
            thl.childForceExpandWidth = thl.childForceExpandHeight = true;
            gal.cgTabButton = SmallButton(tabs, "CGTab", "CG", 24, 50);
            gal.endingTabButton = SmallButton(tabs, "EndingTab", "엔딩", 24, 50);

            gal.countText = Txt(content, "Count", "0 / 0", 24, TextAnchor.MiddleRight, BrownSoft, FontStyle.Bold);
            Place(gal.countText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -20), new Vector2(300, 50));

            // CG 페이지 — 4칸씩 썸네일
            var cgPage = Child("CGPage", content);
            Stretch(cgPage, 24, 24, 24, 90);
            gal.cgPage = cgPage.gameObject;
            var grid = cgPage.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(286, 190);
            grid.spacing = new Vector2(20, 20);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperCenter;
            gal.cgContainer = cgPage;

            var slot = CardButton(cgPage, "SlotTemplate", "?", 22, 18, BrownSoft);
            var slotLabel = slot.GetComponentInChildren<Text>();
            slotLabel.gameObject.name = "Label";
            var thumb = Img(Child("Thumb", slot.transform), Color.white);
            Stretch(thumb.rectTransform, 10, 40, 10, 10);
            thumb.preserveAspect = true;
            thumb.transform.SetSiblingIndex(1); // 배경 위, 글자 아래
            slotLabel.alignment = TextAnchor.LowerCenter;
            Stretch(slotLabel.rectTransform, 8, 8, 8, 6);
            slot.gameObject.SetActive(false);
            gal.cgSlotTemplate = slot;

            // 엔딩 페이지 — 목록
            var endPage = Child("EndingPage", content);
            Stretch(endPage, 40, 24, 40, 90);
            gal.endingPage = endPage.gameObject;
            var vl = endPage.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 8;
            vl.childAlignment = TextAnchor.UpperLeft;
            vl.childControlWidth = vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            gal.endingContainer = endPage;
            var row = Txt(endPage, "RowTemplate", "♥ 엔딩", 28, TextAnchor.MiddleLeft, Brown);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 46;
            row.gameObject.SetActive(false);
            gal.endingRowTemplate = row;
            endPage.gameObject.SetActive(false);

            gal.closeButton = close;

            // CG 크게 보기 (클릭하면 닫힘)
            var viewer = Child("Viewer", rt);
            Stretch(viewer);
            var vbg = Img(viewer, Color.black);
            vbg.raycastTarget = true;
            var vbtn = viewer.gameObject.AddComponent<Button>();
            vbtn.transition = Selectable.Transition.None;
            vbtn.navigation = new Navigation { mode = Navigation.Mode.None };
            gal.viewerImage = Img(Child("Image", viewer), Color.white);
            Stretch(gal.viewerImage.rectTransform);
            gal.viewerImage.preserveAspect = true;
            viewer.gameObject.SetActive(false);
            gal.viewer = viewer.gameObject;
            return gal;
        }

        static VNConfirmDialog BuildConfirm(Transform root)
        {
            var rt = Child("ConfirmDialog", root);
            Stretch(rt);
            Img(rt, DimColor).raycastTarget = true;
            var win = Child("Window", rt);
            Place(win, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 340));
            var wImg = Rounded(win, Cream, 30);
            wImg.raycastTarget = true;
            AddDropShadow(wImg);
            var bar = Rounded(Child("Accent", win), Pink, 4);
            Place(bar.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(64, 8));

            var dlg = rt.gameObject.AddComponent<VNConfirmDialog>();
            dlg.messageText = Txt(win, "Message", "확인 메시지", 30, TextAnchor.MiddleCenter, Brown, FontStyle.Bold);
            Stretch(dlg.messageText.rectTransform, 40, 120, 40, 50);
            dlg.messageText.lineSpacing = 1.3f;

            dlg.yesButton = WhiteButton(win, "Yes", "예", 28, 64, false, Pink, Color.white);
            Place((RectTransform)dlg.yesButton.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-125, 40), new Vector2(220, 64));
            dlg.noButton = WhiteButton(win, "No", "아니오", 28, 64);
            Place((RectTransform)dlg.noButton.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(125, 40), new Vector2(220, 64));
            return dlg;
        }

        // ───────────────────────── 메뉴 창 틀 ─────────────────────────

        /// <summary>
        /// 크림색 메뉴 창: 제목(분홍 막대 + 굵은 글씨) + 설명 + 오른쪽 위 둥근 X + 안쪽 베이지 영역
        /// withTabs 면 창 왼쪽에 아이콘 탭(저장/불러오기/로그/갤러리/설정)이 붙음
        /// </summary>
        static RectTransform Menu(Transform root, string name, Vector2 size, string title, string desc, bool withTabs,
            VNMenuKind kind, out RectTransform content, out Button close, out Text header, out Text descText, out VNMenuTabs tabs)
        {
            var rt = Child(name, root);
            Stretch(rt);
            Img(rt, DimColor).raycastTarget = true;

            var win = Child("Window", rt);
            Place(win, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(withTabs ? 56 : 0, 0), size);
            var wImg = Rounded(win, Cream, 32);
            wImg.raycastTarget = true;
            AddDropShadow(wImg);

            var bar = Rounded(Child("TitleBar", win), Pink, 4);
            Place(bar.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(46, -38), new Vector2(8, 44));
            header = Txt(win, "Header", title, 40, TextAnchor.MiddleLeft, Brown, FontStyle.Bold);
            Place(header.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(68, -30), new Vector2(800, 60));
            descText = Txt(win, "Desc", desc, 22, TextAnchor.MiddleLeft, BrownSoft);
            Place(descText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(48, -90), new Vector2(900, 34));

            close = CloseButton(win);

            content = Child("Content", win);
            Stretch(content, 40, 40, 40, 138);
            Rounded(content, Inner, 22);

            tabs = null;
            if (withTabs)
            {
                var side = Child("Tabs", win);
                Place(side, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -40), new Vector2(120, 600));
                var svl = side.gameObject.AddComponent<VerticalLayoutGroup>();
                svl.spacing = 12;
                svl.childAlignment = TextAnchor.UpperRight;
                svl.childControlWidth = svl.childControlHeight = true;
                svl.childForceExpandWidth = true;
                svl.childForceExpandHeight = false;
                side.SetAsFirstSibling(); // 창 뒤로 살짝 숨도록
                tabs = win.gameObject.AddComponent<VNMenuTabs>();
                tabs.current = kind;
                tabs.normalBg = Color.white;
                tabs.selectedBg = Brown;
                tabs.normalFg = Brown;
                tabs.selectedFg = Color.white;
                tabs.tabs = new[]
                {
                    MenuTab(side, VNMenuKind.Save, "저장", iconFolder, "S"),
                    MenuTab(side, VNMenuKind.Load, "불러오기", iconFolder, "L"),
                    MenuTab(side, VNMenuKind.Log, "로그", iconLog, null),
                    MenuTab(side, VNMenuKind.Gallery, "갤러리", iconImage, null),
                    MenuTab(side, VNMenuKind.Settings, "설정", iconSettings, null),
                };
            }
            return rt;
        }

        static VNMenuTabs.Tab MenuTab(Transform parent, VNMenuKind kind, string label, Sprite icon, string letter)
        {
            var rt = Child("Tab_" + label, parent);
            var bg = Rounded(rt, Color.white, 22);
            bg.raycastTarget = true;
            AddDropShadow(bg);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.colors = TintBlock();
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 104;

            var ic = Img(Child("Icon", rt), Brown, icon);
            ic.type = Image.Type.Simple;
            Place(ic.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(6, 14), new Vector2(54, 54));
            if (letter != null)
            {
                var lt = Txt(ic.rectTransform, "Letter", letter, 22, TextAnchor.MiddleCenter, Brown, FontStyle.Bold);
                Place(lt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -4), new Vector2(40, 30));
                // 아이콘 색을 따라가도록 VNMenuTabs 가 icon 만 칠하므로 글자는 Image 하위로 두고 같이 칠함
                lt.gameObject.AddComponent<VNTintFollower>().source = ic;
            }
            var t = Txt(rt, "Label", label, 18, TextAnchor.MiddleCenter, Brown, FontStyle.Bold);
            Place(t.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(6, 10), new Vector2(110, 26));

            var pb = rt.gameObject.AddComponent<VNPillButton>();
            pb.hoverScale = 1.05f;
            return new VNMenuTabs.Tab { kind = kind, button = b, background = bg, icon = ic, label = t };
        }

        static Button CloseButton(RectTransform win)
        {
            var rt = Child("Close", win);
            Place(rt, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-62, -62), new Vector2(60, 60));
            var bg = Pill(rt, Brown, 60);
            bg.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.colors = TintBlock();
            var x = Img(Child("X", rt), Color.white, xmark);
            x.type = Image.Type.Simple;
            Stretch(x.rectTransform, 15, 15, 15, 15);
            var pb = rt.gameObject.AddComponent<VNPillButton>();
            pb.hoverScale = 1.1f;
            return b;
        }

        // ───────────────────────── 버튼 부품 ─────────────────────────

        /// <summary>
        /// 흰 둥근 버튼 (그림자 살짝). 마우스를 올리면 분홍으로 채워지고 글자가 흰색으로.
        /// marks = 양옆에 » « 표시
        /// </summary>
        static Button WhiteButton(Transform parent, string name, string label, int fontSize, float height,
            bool marks = false, Color? baseColor = null, Color? labelColor = null)
        {
            var rt = Child(name, parent);
            float radius = height * 0.38f;
            var img = Rounded(rt, baseColor ?? CardWhite, radius);
            img.raycastTarget = true;
            AddDropShadow(img);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.colors = TintBlock();

            var fill = Rounded(Child("HoverFill", rt), baseColor.HasValue ? PinkDeep : Pink, radius);
            Stretch(fill.rectTransform);
            fill.enabled = false;

            Color normal = labelColor ?? Brown;
            var t = Txt(rt, "Text", label, fontSize, TextAnchor.MiddleCenter, normal, FontStyle.Bold);
            Stretch(t.rectTransform, 24, 4, 24, 4);

            var pb = rt.gameObject.AddComponent<VNPillButton>();
            pb.hoverFill = fill;
            pb.label = t;
            pb.labelNormal = normal;
            pb.labelHover = Color.white;
            pb.hoverScale = 1.03f;

            if (marks)
            {
                var m = Child("Marks", rt);
                Stretch(m);
                var l = Txt(m, "Left", "»", fontSize + 6, TextAnchor.MiddleRight, LavenderDeep, FontStyle.Bold);
                Place(l.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 2), new Vector2(60, height));
                var r = Txt(m, "Right", "«", fontSize + 6, TextAnchor.MiddleLeft, LavenderDeep, FontStyle.Bold);
                Place(r.rectTransform, new Vector2(1, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 2), new Vector2(60, height));
                m.gameObject.SetActive(false);
                pb.hoverMarks = m.gameObject;
            }
            return b;
        }

        static Button TitleButton(Transform parent, string label)
        {
            var b = WhiteButton(parent, label, label, 30, 66, true);
            b.gameObject.AddComponent<LayoutElement>().preferredHeight = 66;
            return b;
        }

        /// <summary>작은 흰 버튼 (탭 등). 글자색은 그대로 두고 마우스 올리면 연분홍</summary>
        static Button SmallButton(Transform parent, string name, string label, int fontSize, float height)
        {
            var rt = Child(name, parent);
            float radius = height * 0.38f;
            var img = Rounded(rt, CardWhite, radius);
            img.raycastTarget = true;
            AddDropShadow(img);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.colors = TintBlock();
            var fill = Rounded(Child("HoverFill", rt), PinkSoft, radius);
            Stretch(fill.rectTransform);
            fill.enabled = false;
            var t = Txt(rt, "Text", label, fontSize, TextAnchor.MiddleCenter, Brown, FontStyle.Bold);
            Stretch(t.rectTransform, 10, 2, 10, 2);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var pb = rt.gameObject.AddComponent<VNPillButton>();
            pb.hoverFill = fill;
            pb.hoverScale = 1.04f;
            return b;
        }

        static Button QuickButton(Transform parent, string label)
        {
            var b = SmallButton(parent, label, label, 20, 44);
            b.GetComponent<Image>().color = new Color(1, 1, 1, 0.94f);
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 44 + label.Length * 20;
            le.preferredHeight = 44;
            return b;
        }

        /// <summary>흰 둥근 카드 (세이브 슬롯, 갤러리, 모드 선택). 마우스 올리면 분홍 테두리</summary>
        static Button CardButton(Transform parent, string name, string label, int fontSize, float radius, Color labelColor)
        {
            var rt = Child(name, parent);
            var img = Rounded(rt, CardWhite, radius);
            img.raycastTarget = true;
            AddDropShadow(img);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.colors = TintBlock();

            var ring = Ring(Child("HoverRing", rt), Pink, radius);
            Stretch(ring.rectTransform);
            ring.enabled = false;

            var t = Txt(rt, "Text", label, fontSize, TextAnchor.MiddleCenter, labelColor);
            Stretch(t.rectTransform, 20, 6, 20, 6);

            var pb = rt.gameObject.AddComponent<VNPillButton>();
            pb.hoverFill = ring;
            pb.hoverScale = 1.02f;
            return b;
        }

        static ColorBlock TintBlock()
        {
            var cb = ColorBlock.defaultColorBlock;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.selectedColor = Color.white;
            cb.pressedColor = new Color(0.9f, 0.86f, 0.86f, 1f);
            cb.disabledColor = new Color(1f, 1f, 1f, 0.5f);
            cb.fadeDuration = 0.08f;
            return cb;
        }

        static void Deco(RectTransform parent, Sprite sp, Color color, Vector2 pos, float size, float angle)
        {
            var img = Img(Child("Deco", parent), color, sp);
            img.type = Image.Type.Simple;
            Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            img.rectTransform.localEulerAngles = new Vector3(0, 0, angle);
        }

        static Slider SliderRow(RectTransform parent, string label, float y, float min, float max, bool whole, out Text valueText)
        {
            var row = Child(label, parent);
            row.anchorMin = new Vector2(0, 1);
            row.anchorMax = new Vector2(1, 1);
            row.pivot = new Vector2(0.5f, 1);
            row.offsetMin = new Vector2(60, y - 70);
            row.offsetMax = new Vector2(-60, y);
            AddDropShadow(Rounded(row, CardWhite, 20));

            var l = Txt(row, "Label", label, 26, TextAnchor.MiddleLeft, Brown, FontStyle.Bold);
            Place(l.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(36, 0), new Vector2(280, 60));

            var sRt = Child("Slider", row);
            Place(sRt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(340, 0), new Vector2(560, 18));
            var bg = Pill(Child("Background", sRt), Track, 18);
            bg.raycastTarget = true;
            Stretch(bg.rectTransform);
            var fillArea = Child("Fill Area", sRt);
            Stretch(fillArea, 0, 0, 0, 0);
            var fill = Pill(Child("Fill", fillArea), Pink, 18);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0, 1);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var handleArea = Child("Handle Slide Area", sRt);
            Stretch(handleArea, 16, 0, 16, 0);
            var handle = Pill(Child("Handle", handleArea), Color.white, 32);
            handle.raycastTarget = true;
            handle.rectTransform.anchorMin = new Vector2(0, 0.5f);
            handle.rectTransform.anchorMax = new Vector2(0, 0.5f);
            handle.rectTransform.sizeDelta = new Vector2(32, 32);
            AddDropShadow(handle);
            var hring = Pill(Child("Ring", handle.rectTransform), Pink, 32, true);
            Stretch(hring.rectTransform);

            var slider = sRt.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = whole;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };

            valueText = Txt(row, "Value", "", 24, TextAnchor.MiddleRight, PinkDeep, FontStyle.Bold);
            Place(valueText.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-36, 0), new Vector2(180, 60));
            return slider;
        }

        static Toggle ToggleRow(RectTransform parent, string label, float y)
        {
            var row = Child(label, parent);
            row.anchorMin = new Vector2(0, 1);
            row.anchorMax = new Vector2(1, 1);
            row.pivot = new Vector2(0.5f, 1);
            row.offsetMin = new Vector2(60, y - 70);
            row.offsetMax = new Vector2(-60, y);
            AddDropShadow(Rounded(row, CardWhite, 20));

            var l = Txt(row, "Label", label, 26, TextAnchor.MiddleLeft, Brown, FontStyle.Bold);
            Place(l.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(36, 0), new Vector2(280, 60));

            // 알약 스위치: 트랙 + 켜지면 분홍 채움 + 흰 동그라미
            var box = Pill(Child("Box", row), Track, 40);
            box.raycastTarget = true;
            Place(box.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(340, 0), new Vector2(78, 40));
            var mark = Pill(Child("On", box.rectTransform), Pink, 40);
            Stretch(mark.rectTransform);
            var knob = Pill(Child("Knob", box.rectTransform), Color.white, 30);
            Place(knob.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(5, 0), new Vector2(30, 30));

            var toggle = row.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = null;
            toggle.navigation = new Navigation { mode = Navigation.Mode.None };
            var sw = row.gameObject.AddComponent<VNSwitch>();
            sw.onFill = mark;
            sw.knob = knob.rectTransform;
            sw.offX = 5;
            sw.onX = 43;
            return toggle;
        }

        // ───────────────────────── 도형 스프라이트 ─────────────────────────

        static void LoadResources()
        {
            uiFontAsset = AssetDatabase.LoadAssetAtPath<Font>(UIFontPath);
            dialogueFontAsset = AssetDatabase.LoadAssetAtPath<Font>(DialogueFontPath);
            font = uiFontAsset != null ? uiFontAsset : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Directory.CreateDirectory(UIDir);
            var c = new Vector2(64, 64);
            var b63 = new Vector4(63, 63, 63, 63);

            pill = MakeSprite("pill", 128, 128, (x, y) => Vector2.Distance(new Vector2(x, y), c) - 63f, b63);
            pillLine = MakeSprite("pill_line_thin", 128, 128, (x, y) =>
                Mathf.Abs(Vector2.Distance(new Vector2(x, y), c) - 60.5f) - 2.5f, b63);
            arrow = MakeSprite("arrow", 128, 128, (x, y) =>
            {
                var p = new Vector2(x, y);
                float d = Seg(p, new Vector2(22, 64), new Vector2(106, 64));
                d = Mathf.Min(d, Seg(p, new Vector2(106, 64), new Vector2(74, 32)));
                d = Mathf.Min(d, Seg(p, new Vector2(106, 64), new Vector2(74, 96)));
                return d - 4.5f;
            }, Vector4.zero);
            heart = MakeSprite("heart", 128, 128, (x, y) =>
            {
                var p = new Vector2(x, y);
                float d = Mathf.Min(Vector2.Distance(p, new Vector2(43, 80)) - 27f, Vector2.Distance(p, new Vector2(85, 80)) - 27f);
                return Mathf.Min(d, Tri(p, new Vector2(19, 70), new Vector2(109, 70), new Vector2(64, 16)));
            }, Vector4.zero);
            sparkle = MakeSprite("sparkle", 128, 128, (x, y) =>
            {
                float dx = Mathf.Abs(x - 64), dy = Mathf.Abs(y - 64);
                float d1 = (dx * 9 + dy * 60 - 60 * 9) / Mathf.Sqrt(60 * 60 + 9 * 9);
                float d2 = (dx * 60 + dy * 9 - 60 * 9) / Mathf.Sqrt(60 * 60 + 9 * 9);
                return Mathf.Min(d1, d2);
            }, Vector4.zero);
            xmark = MakeSprite("xmark", 128, 128, (x, y) =>
            {
                var p = new Vector2(x, y);
                return Mathf.Min(Seg(p, new Vector2(30, 30), new Vector2(98, 98)), Seg(p, new Vector2(30, 98), new Vector2(98, 30))) - 7f;
            }, Vector4.zero);

            // 탭 아이콘 (선 두께 8)
            iconSettings = MakeSprite("icon_settings", 128, 128, (x, y) =>
            {
                var p = new Vector2(x, y);
                float[] ly = { 96, 64, 32 };
                float[] kx = { 82, 44, 76 };
                float d = 1e9f;
                for (int i = 0; i < 3; i++) d = Mathf.Min(d, Seg(p, new Vector2(16, ly[i]), new Vector2(112, ly[i])) - 4f);
                for (int i = 0; i < 3; i++)
                {
                    float cd = Vector2.Distance(p, new Vector2(kx[i], ly[i]));
                    d = Mathf.Max(d, 9.5f - cd);                       // 동그라미 안쪽은 비움
                    d = Mathf.Min(d, Mathf.Abs(cd - 13.5f) - 4f);      // 동그라미 테두리
                }
                return d;
            }, Vector4.zero);
            iconImage = MakeSprite("icon_image", 128, 128, (x, y) =>
            {
                var p = new Vector2(x, y);
                float box = Mathf.Abs(RoundBox(p, c, new Vector2(50, 42), 12)) - 4f;
                float m = Mathf.Min(Seg(p, new Vector2(24, 34), new Vector2(52, 66)), Seg(p, new Vector2(52, 66), new Vector2(68, 50)));
                m = Mathf.Min(m, Seg(p, new Vector2(68, 50), new Vector2(82, 62)));
                m = Mathf.Min(m, Seg(p, new Vector2(82, 62), new Vector2(104, 38))) - 4f;
                float sun = Vector2.Distance(p, new Vector2(86, 86)) - 9f;
                return Mathf.Min(box, Mathf.Min(m, sun));
            }, Vector4.zero);
            iconFolder = MakeSprite("icon_folder", 128, 128, (x, y) =>
            {
                var p = new Vector2(x, y);
                float body = Mathf.Abs(RoundBox(p, new Vector2(64, 56), new Vector2(50, 36), 10)) - 4f;
                float tab = Mathf.Abs(RoundBox(p, new Vector2(38, 94), new Vector2(24, 8), 6)) - 4f;
                return Mathf.Min(body, Mathf.Max(tab, 90 - y));
            }, Vector4.zero);
            iconLog = MakeSprite("icon_log", 128, 128, (x, y) =>
            {
                var p = new Vector2(x, y);
                float bubble = Mathf.Abs(RoundBox(p, new Vector2(64, 72), new Vector2(50, 36), 16)) - 4f;
                float tail = Mathf.Min(Seg(p, new Vector2(42, 36), new Vector2(34, 18)), Seg(p, new Vector2(34, 18), new Vector2(60, 36))) - 4f;
                float lines = Mathf.Min(Seg(p, new Vector2(40, 82), new Vector2(88, 82)), Seg(p, new Vector2(40, 62), new Vector2(74, 62))) - 4f;
                return Mathf.Min(bubble, Mathf.Min(tail, lines));
            }, Vector4.zero);
        }

        static float Seg(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        static float RoundBox(Vector2 p, Vector2 center, Vector2 half, float r)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) - half + new Vector2(r, r);
            return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - r;
        }

        /// <summary>삼각형 거리 함수 (안쪽 음수)</summary>
        static float Tri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d = Mathf.Min(Seg(p, a, b), Mathf.Min(Seg(p, b, c), Seg(p, c, a)));
            float s1 = Cross(b - a, p - a), s2 = Cross(c - b, p - b), s3 = Cross(a - c, p - c);
            bool inside = (s1 >= 0 && s2 >= 0 && s3 >= 0) || (s1 <= 0 && s2 <= 0 && s3 <= 0);
            return inside ? -d : d;
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        /// <summary>거리 함수로 흰색 도형 PNG를 만들어 Assets/VN/UI/ 에 저장 (없을 때만)</summary>
        static Sprite MakeSprite(string name, int w, int h, System.Func<float, float, float> sdf, Vector4 border)
        {
            string path = UIDir + name + ".png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float a = Mathf.Clamp01(0.5f - sdf(x + 0.5f, y + 0.5f));
                        px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255));
                    }
                tex.SetPixels32(px);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }

            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            if (ti.textureType != TextureImporterType.Sprite || ti.spriteBorder != border || ti.mipmapEnabled ||
                ti.textureCompression != TextureImporterCompression.Uncompressed)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spriteBorder = border;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.filterMode = FilterMode.Bilinear;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>알약 (높이의 절반이 모서리 반지름). 정사각형이면 원</summary>
        static Image Pill(RectTransform rt, Color color, float height, bool outline = false)
        {
            var img = Img(rt, color, outline ? pillLine : pill);
            img.pixelsPerUnitMultiplier = 63f / (height * 0.5f);
            return img;
        }

        /// <summary>모서리가 둥근 사각형</summary>
        static Image Rounded(RectTransform rt, Color color, float radius)
        {
            var img = Img(rt, color, pill);
            img.pixelsPerUnitMultiplier = 63f / radius;
            return img;
        }

        /// <summary>모서리가 둥근 사각 테두리</summary>
        static Image Ring(RectTransform rt, Color color, float radius)
        {
            var img = Img(rt, color, pillLine);
            img.pixelsPerUnitMultiplier = 63f / radius;
            return img;
        }

        /// <summary>대사용 폰트로 표시 (VNEngine.dialogueFont)</summary>
        static void DialogueFont(Text t)
        {
            t.gameObject.AddComponent<VNDialogueFont>();
            if (dialogueFontAsset != null) t.font = dialogueFontAsset;
        }

        static void AddDropShadow(Graphic g)
        {
            var s = g.gameObject.AddComponent<Shadow>();
            s.effectColor = ShadowColor;
            s.effectDistance = new Vector2(0, -4);
        }

        static void AddOutline(Graphic g, Color color, float dist)
        {
            var o = g.gameObject.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(dist, -dist);
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        static RectTransform Child(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static Image Img(RectTransform rt, Color color, Sprite sprite = null)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        static Text Txt(Transform parent, string name, string content, int size, TextAnchor align, Color color,
            FontStyle style = FontStyle.Normal)
        {
            var rt = Child(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = content;
            t.fontSize = size;
            // UI 폰트(그리운 예은체)는 원래 굵어서, 작은 글씨에 굵게까지 넣으면 뭉개짐 → 큰 제목만 굵게
            if (uiFontAsset != null && style == FontStyle.Bold && size < 60) style = FontStyle.Normal;
            t.fontStyle = style;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
    }
}
