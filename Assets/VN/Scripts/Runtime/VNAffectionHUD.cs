using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    /// <summary>
    /// 화면 오른쪽 위의 날짜 표시 + 호감도 스탯창.
    /// 스탯창은 퀵 메뉴 "스탯" 버튼(또는 S 키)으로 펴고 접으며, 접혀 있어도 호감도가 바뀌면 잠깐 떠요.
    /// 스토리에서  @hud on / @hud off,  @day 1일차 — 4월 3일(월),  @gauge 승현 서연 기주 수민  으로 조작합니다.
    /// 게이지가 1개면 큰 패널(상태 문구 포함), 2개 이상이면 작은 줄 여러 개로 표시합니다.
    /// </summary>
    public class VNAffectionHUD : MonoBehaviour
    {
        [Header("호감도 설정 (여기서 수정)")]
        [Tooltip("@gauge 를 안 쓰면 표시할 게이지 이름")]
        public string defaultGauge = "승주";
        [Tooltip("게이지 이름 뒤에 붙여 변수 이름을 만듦. 예) 승주 → 승주호감도")]
        public string variableSuffix = "호감도";
        public int maxValue = 100;
        [Tooltip("호감도 구간별 상태 문구 (0부터 최대값까지 균등하게 나눔). 게이지가 1개일 때만 표시")]
        public string[] stageNames = { "그냥 친구", "조금 신경 쓰임", "두근두근", "서로 좋아하는 중?" };

        [Header("스탯창 (오른쪽 위)")]
        [Tooltip("호감도가 바뀌면 스탯창을 잠깐 띄워 주는 시간(초). 0이면 안 띄움")]
        public float peekSeconds = 2.5f;

        [Header("참조 — 날짜 (씬 생성 시 자동 연결)")]
        public GameObject dayChip;
        public Text dayText;

        [Header("참조 — 게이지 1개 (씬 생성 시 자동 연결)")]
        public GameObject panel;
        public Text nameText;
        public Image gaugeFill;
        public Text valueText;
        public Text stageText;
        public Text deltaText;

        [Header("참조 — 게이지 여러 개 (씬 생성 시 자동 연결)")]
        public GameObject multiPanel;
        [Tooltip("Name / Gauge/Fill / Value / Delta 자식을 가진 줄 템플릿")]
        public GameObject rowTemplate;

        public bool Visible { get; private set; }
        public string DayLabel { get; private set; } = "";
        /// <summary>현재 게이지 이름들 (세이브용, 공백으로 구분)</summary>
        public string GaugeList => string.Join(" ", names);

        class Gauge
        {
            public string name, variable;
            public Text nameText, valueText, stageText, deltaText;
            public Image fill;
            public float shown;
            public Coroutine gaugeRoutine, deltaRoutine;
            public Vector2 deltaBasePos;
            public bool deltaBaseStored;
        }

        readonly List<string> names = new List<string>();
        readonly List<Gauge> gauges = new List<Gauge>();
        readonly List<GameObject> rows = new List<GameObject>();
        Gauge single;

        bool Multi => names.Count > 1;

        /// <param name="gaugeNames">null 이나 빈 배열이면 기본 게이지</param>
        public void SetGauges(IList<string> gaugeNames)
        {
            names.Clear();
            if (gaugeNames != null)
                foreach (var n in gaugeNames)
                    if (!string.IsNullOrWhiteSpace(n)) names.Add(n.Trim());
            if (names.Count == 0) names.Add(defaultGauge);

            foreach (var g in gauges) StopRoutines(g);
            gauges.Clear();
            foreach (var r in rows) Destroy(r);
            rows.Clear();

            if (Multi)
            {
                foreach (var n in names)
                {
                    var row = Instantiate(rowTemplate, rowTemplate.transform.parent);
                    row.name = n;
                    row.SetActive(true);
                    rows.Add(row);
                    var g = new Gauge
                    {
                        name = n,
                        variable = n + variableSuffix,
                        nameText = row.transform.Find("Name").GetComponent<Text>(),
                        fill = row.transform.Find("Gauge/Fill").GetComponent<Image>(),
                        valueText = row.transform.Find("Value").GetComponent<Text>(),
                        deltaText = row.transform.Find("Delta").GetComponent<Text>(),
                    };
                    Color c = VNScript.GetNameColor(n);
                    if (c != Color.white) { g.nameText.color = c; g.fill.color = c; }
                    g.deltaText.gameObject.SetActive(false);
                    gauges.Add(g);
                }
            }
            else
            {
                if (single == null)
                    single = new Gauge { nameText = nameText, fill = gaugeFill, valueText = valueText, stageText = stageText, deltaText = deltaText };
                single.name = names[0];
                single.variable = names[0] + variableSuffix;
                gauges.Add(single);
            }

            ApplyPanels();
            SetDay(DayLabel);
            RefreshAll(false);
        }

        public void SetVisible(bool visible)
        {
            Visible = visible;
            ApplyPanels();
        }

        public void SetDay(string label)
        {
            DayLabel = label ?? "";
            dayText.text = DayLabel;
            ApplyPanels();
        }

        /// <summary>스탯창을 계속 펴 둘지 (퀵 메뉴 "스탯" 버튼 / S 키). 다음 실행에도 기억함</summary>
        public bool StatsOpen
        {
            get => PlayerPrefs.GetInt("vn.statsOpen", 0) == 1;
            private set { PlayerPrefs.SetInt("vn.statsOpen", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public void ToggleStats()
        {
            StatsOpen = !StatsOpen;
            peeking = false;
            if (peekRoutine != null) { StopCoroutine(peekRoutine); peekRoutine = null; }
            ApplyPanels();
        }

        /// <summary>변수 값이 바뀌었을 때 엔진이 호출. 게이지 변수가 아니면 무시.</summary>
        public void OnVariableChanged(string key, int before, int after)
        {
            foreach (var g in gauges)
            {
                if (g.variable != key) continue;
                if (after != before) Peek();
                Refresh(g, after, true);
                ShowDelta(g, after - before);
            }
        }

        bool peeking;
        Coroutine peekRoutine;

        /// <summary>스탯창이 닫혀 있으면 잠깐 열었다가 닫음</summary>
        void Peek()
        {
            if (StatsOpen || !Visible || peekSeconds <= 0f || !isActiveAndEnabled) return;
            if (peekRoutine != null) StopCoroutine(peekRoutine);
            peekRoutine = StartCoroutine(PeekRoutine());
        }

        IEnumerator PeekRoutine()
        {
            peeking = true;
            ApplyPanels();
            yield return new WaitForSeconds(peekSeconds);
            peeking = false;
            ApplyPanels();
            peekRoutine = null;
        }

        /// <param name="animate">false면 게이지를 즉시 맞춤 (불러오기 등)</param>
        public void RefreshAll(bool animate)
        {
            foreach (var g in gauges) Refresh(g, VNEngine.Instance != null ? VNEngine.Instance.GetVariable(g.variable) : 0, animate);
        }

        void ApplyPanels()
        {
            if (!Visible && peekRoutine != null) { StopCoroutine(peekRoutine); peekRoutine = null; peeking = false; }
            dayChip.SetActive(Visible && !string.IsNullOrEmpty(DayLabel));
            bool show = Visible && (StatsOpen || peeking);
            panel.SetActive(show && !Multi);
            multiPanel.SetActive(show && Multi);
        }

        void Refresh(Gauge g, int value, bool animate)
        {
            g.nameText.text = "♥ " + g.name;
            g.valueText.text = Multi ? value.ToString() : $"{value} / {maxValue}";
            if (g.stageText != null) g.stageText.text = StageName(value);
            if (g.gaugeRoutine != null) { StopCoroutine(g.gaugeRoutine); g.gaugeRoutine = null; }
            if (!animate || !isActiveAndEnabled) { g.shown = value; ApplyFill(g); return; }
            g.gaugeRoutine = StartCoroutine(AnimateGauge(g, value));
        }

        void ShowDelta(Gauge g, int delta)
        {
            if (delta == 0 || !isActiveAndEnabled || !Visible) return;
            if (g.deltaRoutine != null) StopCoroutine(g.deltaRoutine);
            g.deltaRoutine = StartCoroutine(DeltaPopup(g, delta));
        }

        void StopRoutines(Gauge g)
        {
            if (g.gaugeRoutine != null) StopCoroutine(g.gaugeRoutine);
            if (g.deltaRoutine != null) StopCoroutine(g.deltaRoutine);
            g.gaugeRoutine = g.deltaRoutine = null;
            if (g.deltaBaseStored) g.deltaText.rectTransform.anchoredPosition = g.deltaBasePos;
            g.deltaText.gameObject.SetActive(false);
        }

        string StageName(int value)
        {
            if (stageNames == null || stageNames.Length == 0) return "";
            float t = Mathf.Clamp01((float)value / Mathf.Max(1, maxValue));
            int i = Mathf.Min(stageNames.Length - 1, Mathf.FloorToInt(t * stageNames.Length));
            return stageNames[i];
        }

        void ApplyFill(Gauge g)
        {
            float t = Mathf.Clamp01(g.shown / Mathf.Max(1, maxValue));
            g.fill.rectTransform.anchorMax = new Vector2(t, 1f);
            g.fill.enabled = t > 0.001f;
        }

        IEnumerator AnimateGauge(Gauge g, int target)
        {
            float from = g.shown;
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                g.shown = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t / 0.6f));
                ApplyFill(g);
                yield return null;
            }
            g.shown = target;
            ApplyFill(g);
            g.gaugeRoutine = null;
        }

        IEnumerator DeltaPopup(Gauge g, int delta)
        {
            var text = g.deltaText;
            text.gameObject.SetActive(true);
            text.text = delta > 0 ? $"♥ +{delta}" : $"{delta}";
            Color c = delta > 0 ? new Color(1f, 0.56f, 0.72f) : new Color(0.6f, 0.75f, 1f);
            var rt = text.rectTransform;
            if (!g.deltaBaseStored) { g.deltaBasePos = rt.anchoredPosition; g.deltaBaseStored = true; }
            Vector2 basePos = g.deltaBasePos;
            float rise = Multi ? 8f : 12f;
            for (float t = 0f; t < 1.6f; t += Time.deltaTime)
            {
                c.a = t < 1f ? 1f : 1f - (t - 1f) / 0.6f;
                text.color = c;
                rt.anchoredPosition = basePos + new Vector2(0f, Mathf.Min(t, 0.4f) / 0.4f * rise);
                yield return null;
            }
            rt.anchoredPosition = basePos;
            text.gameObject.SetActive(false);
            g.deltaRoutine = null;
        }
    }
}
