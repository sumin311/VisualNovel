using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    /// <summary>지나간 대사 기록 (마우스 휠 위로 / 로그 버튼)</summary>
    public class VNBacklog : MonoBehaviour
    {
        public ScrollRect scrollRect;
        public RectTransform content;
        public Text entryTemplate;
        public Button closeButton;
        public int maxEntries = 200;

        struct Entry
        {
            public string speaker;
            public string text;
            public Color color;
        }

        readonly List<Entry> entries = new List<Entry>();
        readonly List<GameObject> spawned = new List<GameObject>();

        public bool IsOpen => gameObject.activeSelf;

        void Awake() => closeButton.onClick.AddListener(Close);

        public void Add(string speaker, string text, Color nameColor)
        {
            entries.Add(new Entry { speaker = speaker, text = text, color = nameColor });
            if (entries.Count > maxEntries) entries.RemoveAt(0);
        }

        public void Clear() => entries.Clear();

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            foreach (var g in spawned) Destroy(g);
            spawned.Clear();

            foreach (var e in entries)
            {
                var t = Instantiate(entryTemplate, content);
                t.gameObject.SetActive(true);
                t.text = string.IsNullOrEmpty(e.speaker)
                    ? e.text
                    : $"<color=#{ColorUtility.ToHtmlStringRGB(e.color)}><b>{e.speaker}</b></color>\n{e.text}";
                spawned.Add(t.gameObject);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            scrollRect.verticalNormalizedPosition = 0f;
        }

        public void Close() => gameObject.SetActive(false);
    }
}
