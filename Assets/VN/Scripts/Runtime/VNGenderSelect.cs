using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    /// <summary>
    /// 새로 시작 → 모드 선택 → <b>성별 선택</b> 창.
    /// 씬을 다시 만들지 않도록, 실행 중에 타이틀의 "모드 선택" 창을 복제해서 모양을 그대로 씀.
    /// 고른 성별은 PlayerPrefs 에 기억해 두었다가 다음에 기본값으로 보여 줌.
    /// </summary>
    public class VNGenderSelect : MonoBehaviour
    {
        static readonly Color Selected = new Color(1f, 0.90f, 0.94f);
        static readonly Color MaleAccent = new Color(0.45f, 0.62f, 0.93f);
        static readonly Color FemaleAccent = new Color(0.95f, 0.48f, 0.64f);
        static readonly Color Muted = new Color(0.55f, 0.44f, 0.41f);

        RectTransform list;
        Button template;
        Text header, desc;
        Action<Dictionary<string, int>> onDone;
        string prefsKey;
        readonly List<GameObject> spawned = new List<GameObject>();
        readonly Dictionary<string, int> choice = new Dictionary<string, int>();
        readonly Dictionary<string, Button> buttons = new Dictionary<string, Button>();

        public bool IsOpen => gameObject.activeSelf;

        public static VNGenderSelect Create(VNTitleMenu title)
        {
            var go = Instantiate(title.modePanel, title.modePanel.transform.parent);
            go.name = "GenderSelect";
            var gs = go.AddComponent<VNGenderSelect>();
            gs.Init();
            go.SetActive(false);
            return gs;
        }

        void Init()
        {
            var win = (RectTransform)transform.Find("Window");
            win.sizeDelta = new Vector2(win.sizeDelta.x + 100, 900);
            header = win.Find("Header")?.GetComponent<Text>();
            desc = win.Find("Desc")?.GetComponent<Text>();
            list = (RectTransform)win.Find("Content/Modes");
            template = list.Find("ModeTemplate").GetComponent<Button>();
            foreach (Transform t in list) if (t != template.transform) Destroy(t.gameObject);
            var vlg = list.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) vlg.spacing = 12;
            // 닫기(X) 버튼
            foreach (var b in win.GetComponentsInChildren<Button>(true))
                if (!b.transform.IsChildOf(list)) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(Close); }
        }

        public void Open(string modeKey, IList<string> cast, Func<string, string> roleOf, Action<Dictionary<string, int>> done)
        {
            onDone = done;
            prefsKey = "vn.gender." + modeKey + ".";
            if (header != null) header.text = "성별 선택";
            if (desc != null) desc.text = "등장인물의 성별을 마음대로 정해 보세요. (다음에도 기억해요)";

            foreach (var g in spawned) Destroy(g);
            spawned.Clear(); choice.Clear(); buttons.Clear();

            foreach (var name in cast)
            {
                choice[name] = PlayerPrefs.GetInt(prefsKey + name, name == "나" ? 1 : 2);
                spawned.Add(MakeRow(name, roleOf(name)));
            }

            var start = Instantiate(template, list);
            start.gameObject.SetActive(true);
            start.name = "Start";
            SetLabel(start, "<b>이대로 시작하기</b>", 34, TextAnchor.MiddleCenter);
            var le = start.GetComponent<LayoutElement>();
            if (le != null) le.preferredHeight = 96;
            SetAccent(start, FemaleAccent, true);
            start.onClick.AddListener(Confirm);
            spawned.Add(start.gameObject);

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Refresh();
        }

        GameObject MakeRow(string name, string role)
        {
            var row = new GameObject(name, typeof(RectTransform));
            row.layer = gameObject.layer;
            row.transform.SetParent(list, false);
            row.AddComponent<LayoutElement>().preferredHeight = 74;
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16;
            h.padding = new RectOffset(24, 12, 0, 0);
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;

            var src = template.GetComponentInChildren<Text>(true);
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.layer = gameObject.layer;
            labelGo.transform.SetParent(row.transform, false);
            var label = labelGo.AddComponent<Text>();
            label.font = src.font;
            label.fontSize = 34;
            label.color = src.color;
            label.alignment = TextAnchor.MiddleLeft;
            label.supportRichText = true;
            label.raycastTarget = false;
            label.text = $"<b>{name}</b>  <size=22><color=#8C6F68>{role}</color></size>";
            var lle = labelGo.AddComponent<LayoutElement>();
            lle.preferredWidth = 440;
            lle.flexibleWidth = 1;

            foreach (int g in new[] { 1, 2 })
            {
                var b = Instantiate(template, row.transform);
                b.gameObject.SetActive(true);
                b.name = g == 1 ? "Male" : "Female";
                SetLabel(b, g == 1 ? "남자" : "여자", 30, TextAnchor.MiddleCenter);
                var ble = b.GetComponent<LayoutElement>();
                if (ble == null) ble = b.gameObject.AddComponent<LayoutElement>();
                ble.preferredWidth = 210;
                ble.preferredHeight = 66;
                int gg = g; string n = name;
                b.onClick.AddListener(() => { choice[n] = gg; Refresh(); });
                buttons[name + g] = b;
            }
            return row;
        }

        static void SetLabel(Button b, string text, int size, TextAnchor align)
        {
            var t = b.GetComponentInChildren<Text>(true);
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            var rt = t.rectTransform;
            rt.offsetMin = new Vector2(40, 6);
            rt.offsetMax = new Vector2(-16, -6);
        }

        static void SetAccent(Button b, Color c, bool on)
        {
            var accent = b.transform.Find("Accent");
            if (accent == null) return;
            var img = accent.GetComponent<Image>();
            img.color = c;
            img.enabled = on;
            var rt = (RectTransform)accent;
            rt.offsetMin = new Vector2(18, 18);
            rt.offsetMax = new Vector2(30, -18);
        }

        void Refresh()
        {
            foreach (var kv in choice)
                foreach (int g in new[] { 1, 2 })
                {
                    if (!buttons.TryGetValue(kv.Key + g, out var b)) continue;
                    bool on = kv.Value == g;
                    if (b.targetGraphic != null) b.targetGraphic.color = on ? Selected : Color.white;
                    SetAccent(b, g == 1 ? MaleAccent : FemaleAccent, on);
                    var t = b.GetComponentInChildren<Text>(true);
                    t.text = on ? $"<b>{(g == 1 ? "남자" : "여자")}</b>" : (g == 1 ? "남자" : "여자");
                    t.color = on ? new Color(0.29f, 0.17f, 0.16f) : Muted;
                }
        }

        void Confirm()
        {
            foreach (var kv in choice) PlayerPrefs.SetInt(prefsKey + kv.Key, kv.Value);
            PlayerPrefs.Save();
            var result = new Dictionary<string, int>(choice);
            var cb = onDone;
            Close();
            cb?.Invoke(result);
        }

        public void Close() => gameObject.SetActive(false);
    }
}
