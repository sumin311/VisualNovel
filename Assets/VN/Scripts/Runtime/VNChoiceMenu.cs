using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    /// <summary>선택지 버튼 목록. 숫자키 1~9로도 선택 가능</summary>
    public class VNChoiceMenu : MonoBehaviour
    {
        public RectTransform container;
        public Button optionTemplate;

        readonly List<GameObject> spawned = new List<GameObject>();
        Action<int> callback;

        public void Show(IList<string> options, Action<int> onSelect)
        {
            Clear();
            callback = onSelect;
            gameObject.SetActive(true);
            for (int i = 0; i < options.Count; i++)
            {
                var b = Instantiate(optionTemplate, container);
                b.gameObject.SetActive(true);
                b.GetComponentInChildren<Text>().text = options[i];
                int idx = i;
                b.onClick.AddListener(() => Select(idx));
                spawned.Add(b.gameObject);
            }
        }

        public void Hide()
        {
            Clear();
            callback = null;
            gameObject.SetActive(false);
        }

        void Select(int i)
        {
            var cb = callback;
            callback = null;
            cb?.Invoke(i);
        }

        void Clear()
        {
            foreach (var g in spawned) Destroy(g);
            spawned.Clear();
        }

        void Update()
        {
            if (callback == null) return;
            for (int i = 0; i < spawned.Count && i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                {
                    Select(i);
                    return;
                }
            }
        }
    }
}
