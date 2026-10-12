using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    /// <summary>하단 대사창: 이름표, 타자기 효과, 퀵 메뉴 버튼</summary>
    public class VNDialogueBox : MonoBehaviour
    {
        public CanvasGroup canvasGroup;
        public GameObject namePlate;
        [Tooltip("이름표 배경. 말하는 캐릭터의 이름표 색(@color)으로 칠해짐")]
        public Image namePlateImage;
        [Tooltip("@color 가 없는 이름의 이름표 색")]
        public Color defaultPlateColor = new Color(0.25f, 0.25f, 0.25f);
        public Color darkText = new Color(0.11f, 0.11f, 0.11f);
        public Text nameText;
        public Text bodyText;
        public Graphic nextIndicator;
        public Text noticeText;
        [Tooltip("알림 배경 (없으면 글자만 표시)")]
        public GameObject noticeRoot;

        [Header("퀵 메뉴 버튼")]
        [Tooltip("퀵 메뉴 버튼 묶음 (화면 위쪽). 대사창을 숨기면 같이 숨김")]
        public GameObject quickMenu;
        public Button quickSaveButton;
        public Button quickLoadButton;
        public Button saveButton;
        public Button loadButton;
        public Button logButton;
        public Button autoButton;
        public Button skipButton;
        public Button statButton;
        public Button configButton;
        public Button titleButton;
        [Tooltip("대사창 숨기기 (폰에서는 오른쪽 클릭/H 키가 없어서 버튼으로)")]
        public Button hideButton;

        [Header("오토/스킵 켜짐 표시 색")]
        public Color toggleOnColor = new Color(1f, 0.45f, 0f);
        public Color toggleOffColor = new Color(0.96f, 0.96f, 0.98f);

        public bool IsTyping { get; private set; }

        string fullText;
        Coroutine typing;
        Coroutine noticeRoutine;

        public void Show(string speaker, string text, Color nameColor, bool instant)
        {
            bool hasName = !string.IsNullOrEmpty(speaker);
            namePlate.SetActive(hasName);
            if (hasName)
            {
                nameText.text = speaker;
                if (namePlateImage != null)
                {
                    // 이름표 = 캐릭터 색 알약, 글자는 배경 밝기에 맞춰 흰색/검은색
                    bool hasColor = nameColor != Color.white;
                    Color plate = hasColor ? nameColor : defaultPlateColor;
                    namePlateImage.color = plate;
                    float lum = 0.299f * plate.r + 0.587f * plate.g + 0.114f * plate.b;
                    nameText.color = lum > 0.68f ? darkText : Color.white;
                }
                else nameText.color = nameColor;
            }

            fullText = text;
            if (typing != null) { StopCoroutine(typing); typing = null; }
            if (instant || !isActiveAndEnabled)
            {
                bodyText.text = text;
                IsTyping = false;
            }
            else typing = StartCoroutine(Type());
        }

        IEnumerator Type()
        {
            IsTyping = true;
            float shown = 0f;
            while (shown < fullText.Length)
            {
                shown += Time.deltaTime * VNSettings.TextSpeed;
                int n = Mathf.Min(fullText.Length, Mathf.FloorToInt(shown));
                // 나머지 글자는 투명하게 붙여서 줄바꿈 위치가 튀지 않게 함
                bodyText.text = fullText.Substring(0, n) + "<color=#00000000>" + fullText.Substring(n) + "</color>";
                yield return null;
            }
            bodyText.text = fullText;
            IsTyping = false;
            typing = null;
        }

        public void CompleteTyping()
        {
            if (!IsTyping) return;
            if (typing != null) { StopCoroutine(typing); typing = null; }
            bodyText.text = fullText;
            IsTyping = false;
        }

        public void Clear()
        {
            CompleteTyping();
            fullText = null;
            bodyText.text = "";
            namePlate.SetActive(false);
            // 알림 도중 타이틀로 나가면 코루틴이 멈춰 글자가 남으므로 정리
            if (noticeRoutine != null) { StopCoroutine(noticeRoutine); noticeRoutine = null; }
            if (noticeText != null) NoticeObject.SetActive(false);
        }

        /// <summary>알림 배경(있으면)까지 포함한 알림 오브젝트</summary>
        GameObject NoticeObject => noticeRoot != null ? noticeRoot : noticeText.gameObject;

        public void SetVisible(bool visible)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
            if (quickMenu != null) quickMenu.SetActive(visible);
        }

        public void SetToggle(Button button, bool on)
        {
            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.color = on ? toggleOnColor : toggleOffColor;
        }

        public void ShowNotice(string message)
        {
            if (noticeText == null) return;
            if (noticeRoutine != null) StopCoroutine(noticeRoutine);
            noticeRoutine = StartCoroutine(Notice(message));
        }

        IEnumerator Notice(string message)
        {
            noticeText.text = message;
            var go = NoticeObject;
            go.SetActive(true);
            var group = go.GetComponent<CanvasGroup>();
            if (group == null) group = go.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.alpha = 1f;
            yield return new WaitForSeconds(1.2f);
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                group.alpha = 1f - t / 0.5f;
                yield return null;
            }
            go.SetActive(false);
            noticeRoutine = null;
        }

        void Update()
        {
            if (nextIndicator == null) return;
            bool show = !IsTyping && !string.IsNullOrEmpty(fullText);
            var go = nextIndicator.gameObject;
            if (go.activeSelf != show) go.SetActive(show);
            if (show)
            {
                // 살짝 위아래로 통통 튀는 표시
                var rt = nextIndicator.rectTransform;
                if (!indicatorBaseStored) { indicatorBase = rt.anchoredPosition; indicatorBaseStored = true; }
                rt.anchoredPosition = indicatorBase + new Vector2(0f, Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 6f);
            }
        }

        Vector2 indicatorBase;
        bool indicatorBaseStored;
    }
}
