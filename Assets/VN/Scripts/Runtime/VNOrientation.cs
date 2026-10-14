using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    /// <summary>
    /// 화면이 세로로 길면(폰을 세로로 들면) 세로 배치로, 가로면 원래(가로) 배치로 바꿉니다.
    /// 씬은 가로 배치로 만들어져 있고, 세로일 때만 여기서 위치·크기를 덮어씀 → 가로로 돌아오면 원래 값으로 되돌림.
    /// 세로 화면 기준 크기: 가로 900 (세로는 폰 비율에 따라 1600 이상)
    /// </summary>
    public class VNOrientation : MonoBehaviour
    {
        public static readonly Vector2 PortraitReference = new Vector2(900, 1600);

        public static bool IsPortrait => Screen.height > Screen.width;

        VNEngine engine;
        CanvasScaler scaler;
        Vector2 landRef;
        CanvasScaler.ScreenMatchMode landMatch;
        float landMatchValue;

        readonly List<Action<bool>> appliers = new List<Action<bool>>();
        Vector2Int lastScreen;
        bool? lastPortrait;
        float W, H; // 세로 화면일 때 캔버스 크기

        public static VNOrientation Create(VNEngine engine, Canvas canvas)
        {
            var o = canvas.gameObject.AddComponent<VNOrientation>();
            o.engine = engine;
            o.Setup(canvas);
            return o;
        }

        void Setup(Canvas canvas)
        {
            scaler = canvas.GetComponent<CanvasScaler>();
            landRef = scaler.referenceResolution;
            landMatch = scaler.screenMatchMode;
            landMatchValue = scaler.matchWidthOrHeight;
            var root = canvas.transform;

            SetupStage();
            SetupGameUI(engine.gameUI.transform);
            SetupTitle(engine.titleMenu.transform);

            foreach (var name in new[] { "SaveLoadMenu", "SettingsMenu", "Backlog", "Gallery" })
                SetupMenuWindow(root.Find(name), true, 0);
            SetupMenuWindow(engine.titleMenu.modePanel.transform, false, 0);
            var gender = engine.titleMenu.modePanel.transform.parent.Find("GenderSelect");
            if (gender != null) SetupMenuWindow(gender, false, 900);

            SetupSaveLoad(engine.saveLoadMenu.slotContainer);
            SetupSettings(root.Find("SettingsMenu/Window/Content"));
            SetupGallery(engine.gallery.cgContainer);
        }

        void Update()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            if (size == lastScreen) return;
            lastScreen = size;
            bool portrait = IsPortrait;
            Apply(portrait, lastPortrait != portrait);
            lastPortrait = portrait;
        }

        void Apply(bool portrait, bool orientationChanged)
        {
            if (portrait)
            {
                scaler.referenceResolution = PortraitReference;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                float s = Mathf.Min(Screen.width / PortraitReference.x, Screen.height / PortraitReference.y);
                W = Screen.width / s;
                H = Screen.height / s;
            }
            else
            {
                scaler.referenceResolution = landRef;
                scaler.screenMatchMode = landMatch;
                scaler.matchWidthOrHeight = landMatchValue;
            }
            foreach (var a in appliers) a(portrait);
            if (orientationChanged || portrait) engine.stage.Relayout();
        }

        // ───────────────────────── 등록 도우미 ─────────────────────────

        struct RectState
        {
            public Vector2 anchorMin, anchorMax, pivot, pos, size;
        }

        static RectState Snap(RectTransform rt) => new RectState
        {
            anchorMin = rt.anchorMin, anchorMax = rt.anchorMax, pivot = rt.pivot,
            pos = rt.anchoredPosition, size = rt.sizeDelta,
        };

        static void Restore(RectTransform rt, RectState s)
        {
            rt.anchorMin = s.anchorMin;
            rt.anchorMax = s.anchorMax;
            rt.pivot = s.pivot;
            rt.anchoredPosition = s.pos;
            rt.sizeDelta = s.size;
        }

        /// <summary>가로 배치를 기억해 두고, 세로일 때만 portrait 로 위치를 바꿈</summary>
        void Rect(Transform t, Action<RectTransform> portrait)
        {
            if (t == null) return;
            var rt = (RectTransform)t;
            var land = Snap(rt);
            appliers.Add(p => { Restore(rt, land); if (p) portrait(rt); });
        }

        /// <summary>가로일 때 값을 기억해 두고, 세로일 때만 portrait() 값으로 바꿈</summary>
        void Value<T>(Func<T> get, Action<T> set, Func<T> portrait)
        {
            T land = get();
            appliers.Add(p => set(p ? portrait() : land));
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        // ───────────────────────── 화면별 세로 배치 ─────────────────────────

        const float BoxTop = 360; // 세로 화면에서 대사창 윗변 높이

        void SetupStage()
        {
            var st = engine.stage;
            // 캐릭터는 대사창 위로 크게. 화면이 길수록 크게 (머리 위로 날짜·스탯창 자리를 남김)
            Value(() => st.characterHeight, v => st.characterHeight = v, () => Mathf.Clamp(H - 640, 900, 1320));
            Value(() => st.characterBottom, v => st.characterBottom = v, () => BoxTop - 120);
            Value(() => st.speakerX, v => st.speakerX = v, () => 0.6f);
            Value(() => st.speakerScale, v => st.speakerScale = v, () => 1f);
            Value(() => st.sideMinX, v => st.sideMinX = v, () => 0.16f);
            Value(() => st.sideMaxX, v => st.sideMaxX = v, () => 0.32f);
            Value(() => st.sideScale, v => st.sideScale = v, () => 0.8f);
        }

        void SetupGameUI(Transform ui)
        {
            var box = ui.Find("DialogueBox");
            Rect(box, rt => { rt.offsetMin = new Vector2(16, 24); rt.offsetMax = new Vector2(-16, BoxTop); });
            Rect(box.Find("Body"), rt => Stretch(rt, 48, 40, 48, 58));

            // 선택지: 대사창 위 영역 가운데
            var choice = ui.Find("ChoiceMenu");
            Rect(choice, rt => Stretch(rt, 0, BoxTop + 16, 0, 0));
            Rect(choice.Find("Options"), rt => rt.sizeDelta = new Vector2(820, rt.sizeDelta.y));

            // 퀵 메뉴: 화면 맨 위 한 줄에 버튼을 고르게 나눠 꽉 채움
            var qm = ui.Find("QuickMenu");
            Rect(qm, rt =>
            {
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.anchoredPosition = new Vector2(0, -14);
                rt.sizeDelta = new Vector2(-28, 56);
            });
            Value(() => qm.localScale, v => qm.localScale = v, () => Vector3.one);
            var qh = qm.GetComponent<HorizontalLayoutGroup>();
            Value(() => qh.spacing, v => qh.spacing = v, () => 6f);
            Value(() => qh.childForceExpandWidth, v => qh.childForceExpandWidth = v, () => true);
            Value(() => qh.childForceExpandHeight, v => qh.childForceExpandHeight = v, () => true);

            // 날짜·스탯창: 퀵 메뉴 아래 줄로
            var hud = ui.Find("AffectionHUD");
            Rect(hud.Find("DayChip"), rt => rt.anchoredPosition = new Vector2(-24, -100));
            Rect(hud.Find("Panel"), rt => rt.anchoredPosition = new Vector2(-24, -158));
            Rect(hud.Find("MultiPanel"), rt => rt.anchoredPosition = new Vector2(-24, -158));
            Rect(ui.Find("Notice"), rt => rt.anchoredPosition = new Vector2(0, -230));
        }

        void SetupTitle(Transform title)
        {
            var safe = title.Find("Safe");
            // 제목·버튼은 위쪽 가운데, 캐릭터는 아래에 크게
            Rect(safe.Find("Right"), rt => Place(rt, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -490), rt.sizeDelta));
            float[] xs = { 190, 450, 710 };
            float[] hs = { 1060, 1200, 1060 };
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                Rect(safe.Find("Characters/Character" + i), rt =>
                {
                    rt.anchoredPosition = new Vector2(xs[k], -60);
                    rt.sizeDelta = new Vector2(hs[k] * 0.62f, hs[k]);
                });
            }
        }

        /// <summary>메뉴 창을 화면 폭에 맞게 좁고 길게. 왼쪽 아이콘 탭은 창 아래 한 줄로</summary>
        void SetupMenuWindow(Transform menu, bool withTabs, float fixedHeight)
        {
            if (menu == null) return;
            var win = (RectTransform)menu.Find("Window");
            Rect(win, rt =>
            {
                float h = fixedHeight > 0 ? fixedHeight : withTabs ? Mathf.Clamp(H - 420, 1000, 1500) : rt.sizeDelta.y;
                Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, withTabs ? 70 : 0), new Vector2(W - 40, h));
            });
            Rect(win.Find("Desc"), rt => rt.sizeDelta = new Vector2(W - 140, rt.sizeDelta.y));

            var tabs = win.Find("Tabs");
            if (!withTabs || tabs == null) return;

            // 탭 줄 (세로 화면용). 탭 버튼을 가로/세로 묶음 사이에서 옮겨 다님
            var row = new GameObject("TabsRow", typeof(RectTransform)).GetComponent<RectTransform>();
            row.gameObject.layer = win.gameObject.layer;
            row.SetParent(win, false);
            row.SetAsFirstSibling();
            Place(row, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(0, 104));
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 12;
            hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childControlWidth = hl.childControlHeight = true;
            hl.childForceExpandWidth = hl.childForceExpandHeight = false;
            row.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var items = new List<Transform>();
            foreach (Transform t in tabs)
            {
                items.Add(t);
                var le = t.GetComponent<LayoutElement>();
                if (le != null) le.preferredWidth = 120;
            }
            appliers.Add(p =>
            {
                foreach (var t in items) t.SetParent(p ? row : tabs, false);
                row.gameObject.SetActive(p);
            });
        }

        void SetupSaveLoad(Transform slots)
        {
            var g = slots != null ? slots.GetComponent<GridLayoutGroup>() : null;
            if (g == null) return;
            Value(() => g.cellSize, v => g.cellSize = v, () => new Vector2(W - 180, 112));
            Value(() => g.spacing, v => g.spacing = v, () => new Vector2(20, 12));
            Value(() => g.constraintCount, v => g.constraintCount = v, () => 1);
        }

        /// <summary>설정 줄: 이름·값은 위에, 슬라이더는 아래에 길게</summary>
        void SetupSettings(Transform content)
        {
            if (content == null) return;
            float y = -30;
            foreach (Transform row in content)
            {
                var slider = row.Find("Slider");
                float top = y, height = slider != null ? 124 : 76;
                Rect(row, rt => { rt.offsetMin = new Vector2(28, top - height); rt.offsetMax = new Vector2(-28, top); });
                if (slider != null)
                {
                    Rect(row.Find("Label"), rt => Place(rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -8), new Vector2(360, 56)));
                    Rect(row.Find("Value"), rt => Place(rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-32, -8), new Vector2(200, 56)));
                    Rect(slider, rt =>
                    {
                        rt.anchorMin = new Vector2(0, 0);
                        rt.anchorMax = new Vector2(1, 0);
                        rt.pivot = new Vector2(0.5f, 0);
                        rt.offsetMin = new Vector2(40, 30);
                        rt.offsetMax = new Vector2(-40, 48);
                    });
                }
                y -= height + 20;
            }
        }

        void SetupGallery(Transform cgPage)
        {
            var g = cgPage != null ? cgPage.GetComponent<GridLayoutGroup>() : null;
            if (g == null) return;
            Value(() => g.cellSize, v => g.cellSize = v, () => new Vector2((W - 200) / 2f, (W - 200) / 2f * 0.66f));
            Value(() => g.constraintCount, v => g.constraintCount = v, () => 2);
        }
    }
}
