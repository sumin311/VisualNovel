using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    /// <summary>알약 모양 스위치. Toggle이 켜지면 주황 채움 + 동그라미가 오른쪽으로</summary>
    [RequireComponent(typeof(Toggle))]
    public class VNSwitch : MonoBehaviour
    {
        public Graphic onFill;
        public RectTransform knob;
        public float offX = 5f;
        public float onX = 45f;

        Toggle toggle;

        void Awake()
        {
            toggle = GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(_ => Apply());
        }

        void OnEnable() => Apply();
        void Update() => Apply(); // SetIsOnWithoutNotify 로 바뀐 경우도 반영

        void Apply()
        {
            if (toggle == null) return;
            bool on = toggle.isOn;
            if (onFill != null) onFill.enabled = on;
            if (knob != null) knob.anchoredPosition = new Vector2(on ? onX : offX, 0f);
        }
    }
}
