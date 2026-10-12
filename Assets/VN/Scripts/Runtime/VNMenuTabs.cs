using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    public enum VNMenuKind { Save, Load, Log, Gallery, Settings }

    /// <summary>
    /// 메뉴 창 왼쪽의 아이콘 탭 (저장 / 불러오기 / 로그 / 갤러리 / 설정).
    /// 누르면 지금 창을 닫고 그 메뉴를 엽니다. 타이틀 화면에서는 저장·로그 탭을 숨깁니다.
    /// </summary>
    public class VNMenuTabs : MonoBehaviour
    {
        [System.Serializable]
        public class Tab
        {
            public VNMenuKind kind;
            public Button button;
            public Image background;
            public Image icon;
            public Text label;
        }

        public Tab[] tabs;
        [Tooltip("이 창의 기본 탭 (저장/불러오기 창은 열 때 바뀜)")]
        public VNMenuKind current;
        public Color normalBg = Color.white;
        public Color selectedBg = new Color(0.29f, 0.17f, 0.16f);
        public Color normalFg = new Color(0.29f, 0.17f, 0.16f);
        public Color selectedFg = Color.white;

        void Awake()
        {
            foreach (var t in tabs)
            {
                var kind = t.kind;
                t.button.onClick.AddListener(() =>
                {
                    if (kind != current && VNEngine.Instance != null) VNEngine.Instance.OpenMenu(kind);
                });
            }
        }

        void OnEnable() => Refresh();

        public void Highlight(VNMenuKind kind)
        {
            current = kind;
            Refresh();
        }

        void Refresh()
        {
            bool inGame = VNEngine.Instance != null && VNEngine.Instance.InGame;
            foreach (var t in tabs)
            {
                bool available = inGame || (t.kind != VNMenuKind.Save && t.kind != VNMenuKind.Log);
                t.button.gameObject.SetActive(available);
                bool on = t.kind == current;
                if (t.background != null) t.background.color = on ? selectedBg : normalBg;
                if (t.icon != null) t.icon.color = on ? selectedFg : normalFg;
                if (t.label != null) t.label.color = on ? selectedFg : normalFg;
            }
        }
    }
}
