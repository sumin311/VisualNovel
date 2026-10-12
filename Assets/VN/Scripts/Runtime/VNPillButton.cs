using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VN
{
    /// <summary>
    /// 알약 모양 버튼의 마우스 올림 효과.
    /// 테두리만 있는 버튼은 안쪽이 채워지고 글자색이 바뀌며, 모든 버튼이 살짝 커집니다.
    /// </summary>
    public class VNPillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("마우스를 올리면 나타나는 채움 (없으면 크기만 변함)")]
        public Graphic hoverFill;
        [Tooltip("마우스를 올리면 나타나는 장식 (양옆 화살표 등)")]
        public GameObject hoverMarks;
        public Text label;
        public Color labelNormal = Color.white;
        public Color labelHover = Color.white;
        [Tooltip("마우스를 올렸을 때 크기 배율")]
        public float hoverScale = 1.03f;

        Selectable selectable;
        bool hovering;

        void Awake()
        {
            selectable = GetComponent<Selectable>();
            Apply(false);
        }

        void OnDisable()
        {
            hovering = false;
            Apply(false);
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (selectable != null && !selectable.IsInteractable()) return;
            hovering = true;
            Apply(true);
        }

        public void OnPointerExit(PointerEventData e)
        {
            hovering = false;
            Apply(false);
        }

        void Update()
        {
            // 비활성화되면 효과 해제
            if (hovering && selectable != null && !selectable.IsInteractable()) { hovering = false; Apply(false); }
        }

        void Apply(bool on)
        {
            if (hoverFill != null) hoverFill.enabled = on;
            if (hoverMarks != null) hoverMarks.SetActive(on);
            if (label != null) label.color = on ? labelHover : labelNormal;
            transform.localScale = Vector3.one * (on ? hoverScale : 1f);
        }
    }
}
