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
    /// </summary>
    [InitializeOnLoad]
    public static class VNSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";
        const string RoundSpritePath = "Assets/VN/UI/round.png";

        // 테마 색상
        static readonly Color PanelColor = new Color(0.06f, 0.06f, 0.11f, 0.86f);
        static readonly Color WindowColor = new Color(0.09f, 0.09f, 0.15f, 0.97f);
        static readonly Color Accent = new Color(1f, 0.56f, 0.72f, 1f);
        static readonly Color AccentDark = new Color(0.82f, 0.38f, 0.55f, 1f);
        static readonly Color ButtonColor = new Color(0.16f, 0.16f, 0.25f, 0.95f);
        static readonly Color SlotColor = new Color(0.14f, 0.14f, 0.22f, 1f);
        static readonly Color TextColor = new Color(0.96f, 0.96f, 0.98f, 1f);
        static readonly Color DimColor = new Color(0f, 0f, 0f, 0.65f);

        static Font font;
        static Sprite round, knob, check;

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
            engine.confirmDialog = BuildConfirm(root);

            engine.saveLoadMenu.gameObject.SetActive(false);
            engine.settingsMenu.gameObject.SetActive(false);
            engine.backlog.gameObject.SetActive(false);
            engine.confirmDialog.gameObject.SetActive(false);
            engine.gameUI.SetActive(false);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;

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

            // 화면 클릭 = 다음 대사
            var catcherImg = Img(Child("ClickCatcher", gameUI), new Color(0, 0, 0, 0));
            Stretch(catcherImg.rectTransform);
            catcherImg.raycastTarget = true;
            var catcher = catcherImg.gameObject.AddComponent<Button>();
            catcher.transition = Selectable.Transition.None;
            catcher.navigation = new Navigation { mode = Navigation.Mode.None };
            engine.clickCatcher = catcher;

            // 대사창
            var boxRt = Child("DialogueBox", gameUI);
            boxRt.anchorMin = new Vector2(0, 0);
            boxRt.anchorMax = new Vector2(1, 0);
            boxRt.pivot = new Vector2(0.5f, 0);
            boxRt.offsetMin = new Vector2(80, 36);
            boxRt.offsetMax = new Vector2(-80, 316);
            var box = boxRt.gameObject.AddComponent<VNDialogueBox>();
            box.canvasGroup = boxRt.gameObject.AddComponent<CanvasGroup>();
            engine.dialogue = box;

            var panel = Img(Child("Panel", boxRt), PanelColor, round);
            Stretch(panel.rectTransform);
            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Accent.r, Accent.g, Accent.b, 0.35f);
            outline.effectDistance = new Vector2(2, -2);

            // 이름표 (글자 길이에 맞게 늘어남)
            var plate = Child("NamePlate", boxRt);
            plate.anchorMin = plate.anchorMax = new Vector2(0, 1);
            plate.pivot = new Vector2(0, 0.5f);
            plate.anchoredPosition = new Vector2(48, 4);
            plate.sizeDelta = new Vector2(260, 64);
            Img(plate, new Color(0.13f, 0.12f, 0.2f, 1f), round);
            var plateOutline = plate.gameObject.AddComponent<Outline>();
            plateOutline.effectColor = Accent;
            plateOutline.effectDistance = new Vector2(2, -2);
            var hl = plate.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(40, 40, 4, 4);
            hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childControlWidth = hl.childControlHeight = true;
            hl.childForceExpandWidth = hl.childForceExpandHeight = true;
            var csf = plate.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            box.namePlate = plate.gameObject;
            box.nameText = Txt(plate, "Name", "이름", 34, TextAnchor.MiddleCenter, TextColor, FontStyle.Bold);
            box.nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            box.nameText.gameObject.AddComponent<LayoutElement>().minWidth = 140;

            // 본문
            box.bodyText = Txt(boxRt, "Body", "여기에 대사가 표시됩니다.", 36, TextAnchor.UpperLeft, TextColor);
            Stretch(box.bodyText.rectTransform, 80, 40, 80, 64);
            box.bodyText.lineSpacing = 1.2f;
            AddShadow(box.bodyText);

            var next = Txt(boxRt, "NextIndicator", "▼", 28, TextAnchor.MiddleCenter, Accent);
            Place(next.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 24), new Vector2(40, 40));
            box.nextIndicator = next;

            // 퀵 메뉴 (대사창 오른쪽 위)
            var qm = Child("QuickMenu", boxRt);
            Place(qm, new Vector2(1, 1), new Vector2(1, 0), new Vector2(-24, 8), new Vector2(1300, 42));
            var qhl = qm.gameObject.AddComponent<HorizontalLayoutGroup>();
            qhl.spacing = 6;
            qhl.childAlignment = TextAnchor.LowerRight;
            qhl.childControlWidth = qhl.childControlHeight = true;
            qhl.childForceExpandWidth = qhl.childForceExpandHeight = false;
            box.quickSaveButton = QuickButton(qm, "Q.저장");
            box.quickLoadButton = QuickButton(qm, "Q.불러오기");
            box.saveButton = QuickButton(qm, "저장");
            box.loadButton = QuickButton(qm, "불러오기");
            box.logButton = QuickButton(qm, "로그");
            box.autoButton = QuickButton(qm, "오토");
            box.skipButton = QuickButton(qm, "스킵");
            box.configButton = QuickButton(qm, "설정");
            box.titleButton = QuickButton(qm, "타이틀");

            engine.hud = BuildHUD(gameUI);

            // 알림 문구 (저장 완료 등)
            var notice = Txt(gameUI, "Notice", "", 32, TextAnchor.MiddleCenter, TextColor);
            Place(notice.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -48), new Vector2(900, 60));
            AddShadow(notice);
            notice.gameObject.SetActive(false);
            box.noticeText = notice;

            // 선택지
            var choiceRt = Child("ChoiceMenu", gameUI);
            Stretch(choiceRt, 0, 330, 0, 0);
            Img(choiceRt, new Color(0, 0, 0, 0.3f)).raycastTarget = true;
            var choice = choiceRt.gameObject.AddComponent<VNChoiceMenu>();
            var options = Child("Options", choiceRt);
            Place(options, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 640));
            var vl = options.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 22;
            vl.childAlignment = TextAnchor.MiddleCenter;
            vl.childControlWidth = vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            var template = MakeButton(options, "OptionTemplate", "선택지", 34, ButtonColor, Accent);
            template.gameObject.AddComponent<LayoutElement>().preferredHeight = 88;
            template.gameObject.SetActive(false);
            choice.container = options;
            choice.optionTemplate = template;
            choiceRt.gameObject.SetActive(false);
            engine.choiceMenu = choice;
        }

        static VNAffectionHUD BuildHUD(Transform parent)
        {
            var root = Child("AffectionHUD", parent);
            Stretch(root);
            var hud = root.gameObject.AddComponent<VNAffectionHUD>();

            var panel = Child("Panel", root);
            Place(panel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -30), new Vector2(560, 150));
            Img(panel, PanelColor, round);
            var o = panel.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(Accent.r, Accent.g, Accent.b, 0.35f);
            o.effectDistance = new Vector2(2, -2);
            hud.panel = panel.gameObject;

            hud.dayText = Txt(panel, "Day", "1일차", 26, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.92f));
            Place(hud.dayText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -12), new Vector2(504, 36));

            hud.nameText = Txt(panel, "Name", "♥ 승주", 30, TextAnchor.MiddleLeft, Accent, FontStyle.Bold);
            Place(hud.nameText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -52), new Vector2(240, 40));

            hud.stageText = Txt(panel, "Stage", "그냥 친구", 24, TextAnchor.MiddleRight, TextColor);
            Place(hud.stageText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -52), new Vector2(280, 40));

            var gaugeBg = Img(Child("Gauge", panel), new Color(1, 1, 1, 0.12f), round);
            Place(gaugeBg.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 20), new Vector2(370, 26));
            hud.gaugeFill = Img(Child("Fill", gaugeBg.rectTransform), Accent, round);
            var fillRt = hud.gaugeFill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0.2f, 1);
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

            hud.valueText = Txt(panel, "Value", "0 / 100", 24, TextAnchor.MiddleRight, TextColor);
            Place(hud.valueText.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-28, 14), new Vector2(130, 36));

            hud.deltaText = Txt(panel, "Delta", "♥ +10", 34, TextAnchor.MiddleLeft, Accent, FontStyle.Bold);
            Place(hud.deltaText.rectTransform, new Vector2(1, 0.5f), new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(220, 50));
            AddShadow(hud.deltaText);
            hud.deltaText.gameObject.SetActive(false);

            panel.gameObject.SetActive(false);
            return hud;
        }

        static VNTitleMenu BuildTitle(Transform root)
        {
            var rt = Child("TitleMenu", root);
            Stretch(rt);
            var title = rt.gameObject.AddComponent<VNTitleMenu>();
            Stretch(Img(Child("BlackBase", rt), Color.black).rectTransform);
            title.background = Img(Child("Background", rt), Color.white);
            Stretch(title.background.rectTransform);

            var shade = Img(Child("Shade", rt), new Color(0.05f, 0.03f, 0.1f, 0.45f));
            shade.rectTransform.anchorMin = Vector2.zero;
            shade.rectTransform.anchorMax = new Vector2(0.42f, 1);
            shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;

            title.titleText = Txt(rt, "Title", "타이틀", 92, TextAnchor.LowerLeft, Color.white, FontStyle.Bold);
            Place(title.titleText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(120, -120), new Vector2(1500, 180));
            title.titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            var o = title.titleText.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(AccentDark.r, AccentDark.g, AccentDark.b, 0.9f);
            o.effectDistance = new Vector2(3, -3);

            title.subtitleText = Txt(rt, "Subtitle", "부제", 36, TextAnchor.UpperLeft, new Color(1f, 0.85f, 0.92f), FontStyle.Italic);
            Place(title.subtitleText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(128, -310), new Vector2(1200, 60));
            AddShadow(title.subtitleText);

            var buttons = Child("Buttons", rt);
            Place(buttons, Vector2.zero, Vector2.zero, new Vector2(120, 130), new Vector2(460, 420));
            var vl = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 20;
            vl.childAlignment = TextAnchor.LowerLeft;
            vl.childControlWidth = vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            title.newGameButton = TitleButton(buttons, "새로 시작");
            title.loadButton = TitleButton(buttons, "불러오기");
            title.settingsButton = TitleButton(buttons, "환경 설정");
            title.quitButton = TitleButton(buttons, "종료");

            var ver = Txt(rt, "Version", "v0.1", 24, TextAnchor.LowerRight, new Color(1, 1, 1, 0.6f));
            Place(ver.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 20), new Vector2(300, 40));
            return title;
        }

        static VNSaveLoadMenu BuildSaveLoad(Transform root)
        {
            var rt = Modal(root, "SaveLoadMenu", new Vector2(1560, 900), out var win);
            var menu = rt.gameObject.AddComponent<VNSaveLoadMenu>();
            menu.headerText = Header(win, "저장하기");

            var grid = Child("Slots", win);
            Stretch(grid, 50, 130, 50, 110);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(715, 150);
            g.spacing = new Vector2(30, 20);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 2;
            g.childAlignment = TextAnchor.UpperCenter;

            var slot = MakeButton(grid, "SlotTemplate", "No.1", 30, SlotColor, AccentDark);
            var label = slot.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            Stretch(label.rectTransform, 36, 16, 36, 16);
            label.lineSpacing = 1.25f;
            slot.gameObject.SetActive(false);
            menu.slotTemplate = slot;
            menu.slotContainer = grid;

            menu.closeButton = BottomButton(win, "닫기");
            return menu;
        }

        static VNSettingsMenu BuildSettings(Transform root)
        {
            var rt = Modal(root, "SettingsMenu", new Vector2(1100, 780), out var win);
            var menu = rt.gameObject.AddComponent<VNSettingsMenu>();
            Header(win, "환경 설정");

            menu.textSpeedSlider = SliderRow(win, "텍스트 속도", -140, 10f, 150f, false, out menu.textSpeedValue);
            menu.autoDelaySlider = SliderRow(win, "오토 대기 시간", -240, 0.5f, 5f, false, out menu.autoDelayValue);
            menu.bgmSlider = SliderRow(win, "배경음 볼륨", -340, 0f, 1f, false, out menu.bgmValue);
            menu.seSlider = SliderRow(win, "효과음 볼륨", -440, 0f, 1f, false, out menu.seValue);
            menu.fullscreenToggle = ToggleRow(win, "전체 화면", -540);

            menu.closeButton = BottomButton(win, "닫기");
            return menu;
        }

        static VNBacklog BuildBacklog(Transform root)
        {
            var rt = Modal(root, "Backlog", new Vector2(1500, 920), out var win);
            var log = rt.gameObject.AddComponent<VNBacklog>();
            Header(win, "대사 로그");

            // 스크롤 영역
            var sv = Child("Scroll", win);
            Stretch(sv, 50, 130, 50, 110);
            var srImg = Img(sv, new Color(0, 0, 0, 0.25f), round);
            srImg.raycastTarget = true;
            var sr = sv.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 40;

            var viewport = Child("Viewport", sv);
            Stretch(viewport, 10, 10, 34, 10);
            viewport.gameObject.AddComponent<RectMask2D>();
            sr.viewport = viewport;

            var content = Child("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var vl = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(30, 30, 20, 20);
            vl.spacing = 28;
            vl.childControlWidth = vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = content;

            // 스크롤바
            var bar = Child("Scrollbar", sv);
            bar.anchorMin = new Vector2(1, 0);
            bar.anchorMax = new Vector2(1, 1);
            bar.pivot = new Vector2(1, 0.5f);
            bar.offsetMin = new Vector2(-20, 14);
            bar.offsetMax = new Vector2(-8, -14);
            Img(bar, new Color(1, 1, 1, 0.08f), round).raycastTarget = true;
            var sb = bar.gameObject.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;
            var area = Child("Sliding Area", bar);
            Stretch(area);
            var handle = Img(Child("Handle", area), Accent, round);
            handle.raycastTarget = true;
            handle.rectTransform.offsetMin = handle.rectTransform.offsetMax = Vector2.zero;
            sb.handleRect = handle.rectTransform;
            sb.targetGraphic = handle;
            sr.verticalScrollbar = sb;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            var entry = Txt(content, "EntryTemplate", "대사", 32, TextAnchor.UpperLeft, TextColor);
            entry.lineSpacing = 1.2f;
            entry.gameObject.SetActive(false);
            log.entryTemplate = entry;
            log.content = content;
            log.scrollRect = sr;

            log.closeButton = BottomButton(win, "닫기");
            return log;
        }

        static VNConfirmDialog BuildConfirm(Transform root)
        {
            var rt = Modal(root, "ConfirmDialog", new Vector2(820, 380), out var win);
            var dlg = rt.gameObject.AddComponent<VNConfirmDialog>();
            dlg.messageText = Txt(win, "Message", "확인 메시지", 34, TextAnchor.MiddleCenter, TextColor);
            Stretch(dlg.messageText.rectTransform, 40, 130, 40, 40);
            dlg.messageText.lineSpacing = 1.2f;

            dlg.yesButton = MakeButton(win, "Yes", "예", 32, AccentDark, Accent);
            Place((RectTransform)dlg.yesButton.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-140, 40), new Vector2(240, 72));
            dlg.noButton = MakeButton(win, "No", "아니오", 32, ButtonColor, Accent);
            Place((RectTransform)dlg.noButton.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(140, 40), new Vector2(240, 72));
            return dlg;
        }

        // ───────────────────────── 부품 ─────────────────────────

        static void LoadResources()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            AssetDatabase.ImportAsset(RoundSpritePath);
            round = AssetDatabase.LoadAssetAtPath<Sprite>(RoundSpritePath);
            if (round == null) round = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            check = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
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
            t.fontStyle = style;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static void AddShadow(Graphic g)
        {
            var s = g.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0, 0, 0, 0.75f);
            s.effectDistance = new Vector2(2, -2);
        }

        static Button MakeButton(Transform parent, string name, string label, int fontSize, Color normal, Color highlight)
        {
            var rt = Child(name, parent);
            var img = Img(rt, Color.white, round);
            img.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            var cb = ColorBlock.defaultColorBlock;
            cb.normalColor = normal;
            cb.selectedColor = normal;
            cb.highlightedColor = highlight;
            cb.pressedColor = new Color(highlight.r * 0.8f, highlight.g * 0.8f, highlight.b * 0.8f, 1f);
            cb.disabledColor = new Color(normal.r, normal.g, normal.b, 0.45f);
            cb.fadeDuration = 0.08f;
            b.colors = cb;

            var t = Txt(rt, "Text", label, fontSize, TextAnchor.MiddleCenter, TextColor);
            Stretch(t.rectTransform, 16, 6, 16, 6);
            return b;
        }

        static Button QuickButton(Transform parent, string label)
        {
            var b = MakeButton(parent, label, label, 24, new Color(0, 0, 0, 0.45f), AccentDark);
            var t = b.GetComponentInChildren<Text>();
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            Stretch(t.rectTransform);
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 44 + label.Length * 24;
            le.preferredHeight = 42;
            return b;
        }

        static Button TitleButton(Transform parent, string label)
        {
            var b = MakeButton(parent, label, label, 36, new Color(0.1f, 0.08f, 0.16f, 0.82f), Accent);
            b.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
            Stretch(b.GetComponentInChildren<Text>().rectTransform, 44, 6, 16, 6);
            b.gameObject.AddComponent<LayoutElement>().preferredHeight = 78;
            return b;
        }

        static Button BottomButton(RectTransform win, string label)
        {
            var b = MakeButton(win, "Close", label, 32, ButtonColor, Accent);
            Place((RectTransform)b.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(260, 70));
            return b;
        }

        static RectTransform Modal(Transform root, string name, Vector2 size, out RectTransform window)
        {
            var rt = Child(name, root);
            Stretch(rt);
            Img(rt, DimColor).raycastTarget = true;

            window = Child("Window", rt);
            Place(window, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var w = Img(window, WindowColor, round);
            w.raycastTarget = true;
            var o = w.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(Accent.r, Accent.g, Accent.b, 0.5f);
            o.effectDistance = new Vector2(2, -2);
            return rt;
        }

        static Text Header(RectTransform win, string title)
        {
            var t = Txt(win, "Header", title, 46, TextAnchor.MiddleCenter, Accent, FontStyle.Bold);
            Place(t.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(800, 80));
            return t;
        }

        static Slider SliderRow(RectTransform win, string label, float y, float min, float max, bool whole, out Text valueText)
        {
            var row = Child(label, win);
            row.anchorMin = new Vector2(0, 1);
            row.anchorMax = new Vector2(1, 1);
            row.pivot = new Vector2(0.5f, 1);
            row.offsetMin = new Vector2(80, y - 70);
            row.offsetMax = new Vector2(-80, y);

            var l = Txt(row, "Label", label, 32, TextAnchor.MiddleLeft, TextColor);
            Place(l.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(280, 60));

            var sRt = Child("Slider", row);
            Place(sRt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(300, 0), new Vector2(480, 24));
            var bg = Img(Child("Background", sRt), new Color(1, 1, 1, 0.12f), round);
            bg.raycastTarget = true;
            Stretch(bg.rectTransform);
            var fillArea = Child("Fill Area", sRt);
            Stretch(fillArea, 0, 0, 0, 0);
            var fill = Img(Child("Fill", fillArea), Accent, round);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0, 1);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var handleArea = Child("Handle Slide Area", sRt);
            Stretch(handleArea, 16, 0, 16, 0);
            var handle = Img(Child("Handle", handleArea), Color.white, null);
            handle.sprite = knob;
            handle.raycastTarget = true;
            handle.rectTransform.anchorMin = new Vector2(0, 0);
            handle.rectTransform.anchorMax = new Vector2(0, 1);
            handle.rectTransform.sizeDelta = new Vector2(40, 16);

            var slider = sRt.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = whole;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };

            valueText = Txt(row, "Value", "", 28, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.92f));
            Place(valueText.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(160, 60));
            return slider;
        }

        static Toggle ToggleRow(RectTransform win, string label, float y)
        {
            var row = Child(label, win);
            row.anchorMin = new Vector2(0, 1);
            row.anchorMax = new Vector2(1, 1);
            row.pivot = new Vector2(0.5f, 1);
            row.offsetMin = new Vector2(80, y - 70);
            row.offsetMax = new Vector2(-80, y);

            var l = Txt(row, "Label", label, 32, TextAnchor.MiddleLeft, TextColor);
            Place(l.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(280, 60));

            var box = Img(Child("Box", row), new Color(1, 1, 1, 0.12f), round);
            box.raycastTarget = true;
            Place(box.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(300, 0), new Vector2(48, 48));
            var mark = Img(Child("Checkmark", box.rectTransform), Accent);
            mark.sprite = check;
            Stretch(mark.rectTransform, 6, 6, 6, 6);

            var toggle = row.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = mark;
            toggle.navigation = new Navigation { mode = Navigation.Mode.None };
            return toggle;
        }
    }
}
