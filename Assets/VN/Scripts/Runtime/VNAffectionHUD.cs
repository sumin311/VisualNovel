using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    /// <summary>
    /// 화면 왼쪽 위의 호감도 게이지 + 날짜 표시.
    /// 스토리에서  @hud on / @hud off,  @day 1일차 — 4월 3일(월)  로 조작합니다.
    /// </summary>
    public class VNAffectionHUD : MonoBehaviour
    {
        [Header("호감도 설정 (여기서 수정)")]
        [Tooltip("게이지에 표시할 변수 이름 (스토리의 @set 변수와 같아야 함)")]
        public string variableName = "승주호감도";
        [Tooltip("게이지 옆에 표시할 이름")]
        public string displayName = "승주";
        public int maxValue = 100;
        [Tooltip("호감도 구간별 상태 문구 (0부터 최대값까지 균등하게 나눔)")]
        public string[] stageNames = { "그냥 친구", "조금 신경 쓰임", "두근두근", "서로 좋아하는 중?" };

        [Header("참조 (씬 생성 시 자동 연결)")]
        public GameObject panel;
        public Text dayText;
        public Text nameText;
        public Image gaugeFill;
        public Text valueText;
        public Text stageText;
        public Text deltaText;

        public bool Visible { get; private set; }
        public string DayLabel { get; private set; } = "";

        Coroutine gaugeRoutine, deltaRoutine;
        float shownValue;
        Vector2 deltaBasePos;
        bool deltaBaseStored;

        public void SetVisible(bool visible)
        {
            Visible = visible;
            panel.SetActive(visible);
        }

        public void SetDay(string label)
        {
            DayLabel = label ?? "";
            dayText.text = DayLabel;
            dayText.gameObject.SetActive(!string.IsNullOrEmpty(DayLabel));
        }

        /// <param name="animate">false면 게이지를 즉시 맞춤 (불러오기 등)</param>
        public void Refresh(int value, bool animate)
        {
            nameText.text = "♥ " + displayName;
            valueText.text = $"{value} / {maxValue}";
            stageText.text = StageName(value);
            if (gaugeRoutine != null) { StopCoroutine(gaugeRoutine); gaugeRoutine = null; }
            if (!animate || !isActiveAndEnabled) { shownValue = value; ApplyFill(); return; }
            gaugeRoutine = StartCoroutine(AnimateGauge(value));
        }

        public void ShowDelta(int delta)
        {
            if (delta == 0 || !isActiveAndEnabled || !Visible) return;
            if (deltaRoutine != null) StopCoroutine(deltaRoutine);
            deltaRoutine = StartCoroutine(DeltaPopup(delta));
        }

        string StageName(int value)
        {
            if (stageNames == null || stageNames.Length == 0) return "";
            float t = Mathf.Clamp01((float)value / Mathf.Max(1, maxValue));
            int i = Mathf.Min(stageNames.Length - 1, Mathf.FloorToInt(t * stageNames.Length));
            return stageNames[i];
        }

        void ApplyFill()
        {
            float t = Mathf.Clamp01(shownValue / Mathf.Max(1, maxValue));
            var rt = gaugeFill.rectTransform;
            rt.anchorMax = new Vector2(t, 1f);
            gaugeFill.enabled = t > 0.001f;
        }

        IEnumerator AnimateGauge(int target)
        {
            float from = shownValue;
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                shownValue = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t / 0.6f));
                ApplyFill();
                yield return null;
            }
            shownValue = target;
            ApplyFill();
            gaugeRoutine = null;
        }

        IEnumerator DeltaPopup(int delta)
        {
            deltaText.gameObject.SetActive(true);
            deltaText.text = delta > 0 ? $"♥ +{delta}" : $"{delta}";
            Color c = delta > 0 ? new Color(1f, 0.56f, 0.72f) : new Color(0.6f, 0.75f, 1f);
            var rt = deltaText.rectTransform;
            if (!deltaBaseStored) { deltaBasePos = rt.anchoredPosition; deltaBaseStored = true; }
            Vector2 basePos = deltaBasePos;
            for (float t = 0f; t < 1.6f; t += Time.deltaTime)
            {
                c.a = t < 1f ? 1f : 1f - (t - 1f) / 0.6f;
                deltaText.color = c;
                rt.anchoredPosition = basePos + new Vector2(0f, Mathf.Min(t, 0.4f) * 30f);
                yield return null;
            }
            rt.anchoredPosition = basePos;
            deltaText.gameObject.SetActive(false);
            deltaRoutine = null;
        }
    }
}
