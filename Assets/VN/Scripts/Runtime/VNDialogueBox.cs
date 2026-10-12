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
        public Text nameText;
        public Text bodyText;
        public Graphic nextIndicator;
        public Text noticeText;

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

        [Header("오토/스킵 켜짐 표시 색")]
        public Color toggleOnColor = new Color(1f, 0.56f, 0.72f);
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
                nameText.color = nameColor;
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
            if (noticeText != null) noticeText.gameObject.SetActive(false);
        }

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
            noticeText.gameObject.SetActive(true);
            var c = noticeText.color;
            c.a = 1f;
            noticeText.color = c;
            yield return new WaitForSeconds(1.2f);
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                c.a = 1f - t / 0.5f;
                noticeText.color = c;
                yield return null;
            }
            noticeText.gameObject.SetActive(false);
            noticeRoutine = null;
        }

        void Update()
        {
            if (nextIndicator == null) return;
            bool show = !IsTyping && !string.IsNullOrEmpty(fullText);
            nextIndicator.enabled = show;
            if (show)
            {
                var c = nextIndicator.color;
                c.a = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.time * 3f));
                nextIndicator.color = c;
            }
        }
    }
}
