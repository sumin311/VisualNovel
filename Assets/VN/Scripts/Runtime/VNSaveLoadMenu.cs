using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    public class VNSaveLoadMenu : MonoBehaviour
    {
        public Text headerText;
        public RectTransform slotContainer;
        public Button slotTemplate;
        public Button closeButton;

        public bool IsOpen => gameObject.activeSelf;

        readonly List<GameObject> spawned = new List<GameObject>();
        bool saveMode;

        void Awake() => closeButton.onClick.AddListener(Close);

        public void Open(bool save)
        {
            saveMode = save;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            headerText.text = save ? "저장하기" : "불러오기";
            Refresh();
        }

        public void Close() => gameObject.SetActive(false);

        void Refresh()
        {
            foreach (var g in spawned) Destroy(g);
            spawned.Clear();

            for (int slot = 1; slot <= VNSaveSystem.SlotCount; slot++)
            {
                var b = Instantiate(slotTemplate, slotContainer);
                b.gameObject.SetActive(true);
                var data = VNSaveSystem.Load(slot);
                b.GetComponentInChildren<Text>().text = data == null
                    ? $"<b>No.{slot}</b>\n<color=#8a8aa0>- 비어 있음 -</color>"
                    : $"<b>No.{slot}</b>    <color=#b8b8cc>{data.savedAt}</color>{(string.IsNullOrEmpty(data.modeName) ? "" : $"    <color=#ff8fb8>{data.modeName}</color>")}\n{Shorten(data.preview, 34)}";
                b.interactable = saveMode || data != null;
                int s = slot;
                bool exists = data != null;
                b.onClick.AddListener(() => OnSlotClicked(s, exists));
                spawned.Add(b.gameObject);
            }
        }

        void OnSlotClicked(int slot, bool exists)
        {
            var engine = VNEngine.Instance;
            if (saveMode)
            {
                if (exists)
                    engine.confirmDialog.Show($"{slot}번 슬롯에 덮어쓸까요?", () => { engine.SaveToSlot(slot); Refresh(); });
                else
                {
                    engine.SaveToSlot(slot);
                    Refresh();
                }
            }
            else
            {
                if (engine.InGame)
                    engine.confirmDialog.Show($"{slot}번 슬롯을 불러올까요?\n현재 진행 상황은 사라집니다.", () => engine.LoadFromSlot(slot));
                else
                    engine.LoadFromSlot(slot);
            }
        }

        static string Shorten(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\n", " ");
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }
    }
}
