using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VN
{
    /// <summary>타이틀의 "새로 시작"에서 고르는 게임 모드</summary>
    [Serializable]
    public class VNGameMode
    {
        public string name;
        [TextArea] public string description;
        [Tooltip("이 모드를 고르면 실행할 스토리 파일 (Assets/Resources/Story/ 안의 .txt, 확장자 제외)")]
        public string startScript;

        public VNGameMode(string name, string description, string startScript)
        {
            this.name = name;
            this.description = description;
            this.startScript = startScript;
        }
    }

    /// <summary>
    /// 스토리 진행의 중심. 스토리 파일을 한 줄씩 실행하고, 입력/메뉴/세이브를 관리합니다.
    /// </summary>
    public class VNEngine : MonoBehaviour
    {
        public static VNEngine Instance { get; private set; }

        [Header("시작 설정")]
        [Tooltip("새로 시작을 누르면 고를 수 있는 모드들")]
        public VNGameMode[] modes =
        {
            new VNGameMode("승주를 꼬시기", "'나'가 되어, 말수 적은 승주의 마음을 얻어 보자.", "start"),
            new VNGameMode("승주가 되기", "승주가 되어 보자. 친구 넷이 전부 나를 노린다?!", "harem_start"),
        };
        [Tooltip("비워두면 파일 처음부터. 테스트할 때 특정 라벨부터 시작하려면 입력 (모든 모드에 적용)")]
        public string startLabel = "";
        [Tooltip("타이틀 화면 BGM (Assets/Resources/Audio/BGM/). 비워두면 재생 안 함")]
        public string titleBgm = "";
        [Tooltip("플레이어 성별 변수 (1 = 남자, 2 = 여자). 대사의 {남자일때|여자일때} 에 사용")]
        public string genderVariable = "성별";

        [Header("폰트")]
        [Tooltip("한글 폰트(.ttf/.otf)를 넣으면 모든 UI 텍스트에 적용. 비워두면 OS의 한글 폰트(맑은 고딕 등) 사용")]
        public Font customFont;

        [Header("참조 (VN > 메인 씬 다시 생성 시 자동 연결)")]
        public VNStage stage;
        public VNAudio audioPlayer;
        public GameObject gameUI;
        public Button clickCatcher;
        public VNDialogueBox dialogue;
        public VNChoiceMenu choiceMenu;
        public VNTitleMenu titleMenu;
        public VNSaveLoadMenu saveLoadMenu;
        public VNSettingsMenu settingsMenu;
        public VNBacklog backlog;
        public VNConfirmDialog confirmDialog;
        public VNAffectionHUD hud;
        public VNGallery gallery;

        public bool InGame { get; private set; }
        public bool AutoMode { get; private set; }
        public bool SkipMode => skipToggle || skipHeld;

        bool AnyMenuOpen => saveLoadMenu.IsOpen || settingsMenu.IsOpen || backlog.IsOpen || confirmDialog.IsOpen || gallery.IsOpen;

        VNScript script;
        int index;          // 다음에 실행할 줄
        int currentIndex;   // 지금 실행 중인 줄 (세이브 위치)
        readonly Dictionary<string, int> variables = new Dictionary<string, int>();
        Coroutine runner;
        bool advanceRequested;
        bool skipToggle, skipHeld;
        bool uiHidden;
        bool endRequested;
        int choiceResult = -1;
        string lastSpeaker, lastText;
        int modeIndex;

        static readonly Regex interpolateRegex = new Regex(@"\{([^{}|]+)\}");
        static readonly Regex genderRegex = new Regex(@"\{([^{}|]*)\|([^{}|]*)\}");
        static readonly Regex setRegex = new Regex(@"^\s*(\S+?)\s*(\+=|-=|=)\s*(\S+)\s*$");
        static readonly Regex condRegex = new Regex(@"^\s*(\S+?)\s*(>=|<=|==|!=|>|<|=)\s*(\S+)\s*$");

        // ───────────────────────── 초기화 ─────────────────────────

        void Awake()
        {
            Instance = this;
            ApplyFont();
        }

        void Start()
        {
            WireButtons();
            audioPlayer.ApplyVolume();
            CloseAllMenus();
            choiceMenu.Hide();
            ShowTitle();
        }

        void ApplyFont()
        {
            Font f = customFont;
            if (f == null)
            {
                try
                {
                    f = Font.CreateDynamicFontFromOSFont(
                        new[] { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR", "NanumGothic" }, 32);
                }
                catch (Exception) { f = null; }
            }
            if (f == null) return;
            foreach (var t in FindObjectsOfType<Text>(true)) t.font = f;
        }

        void WireButtons()
        {
            clickCatcher.onClick.AddListener(OnScreenClick);

            dialogue.quickSaveButton.onClick.AddListener(QuickSave);
            dialogue.quickLoadButton.onClick.AddListener(QuickLoad);
            dialogue.saveButton.onClick.AddListener(() => saveLoadMenu.Open(true));
            dialogue.loadButton.onClick.AddListener(() => saveLoadMenu.Open(false));
            dialogue.logButton.onClick.AddListener(backlog.Open);
            dialogue.autoButton.onClick.AddListener(ToggleAuto);
            dialogue.skipButton.onClick.AddListener(ToggleSkip);
            dialogue.statButton.onClick.AddListener(ToggleStats);
            dialogue.configButton.onClick.AddListener(settingsMenu.Open);
            dialogue.titleButton.onClick.AddListener(() =>
                confirmDialog.Show("타이틀 화면으로 돌아갈까요?\n저장하지 않은 진행 상황은 사라집니다.", ReturnToTitle));

            titleMenu.newGameButton.onClick.AddListener(() =>
            {
                if (modes.Length == 1) NewGame(0);
                else titleMenu.OpenModeSelect(modes, NewGame);
            });
            titleMenu.loadButton.onClick.AddListener(() => saveLoadMenu.Open(false));
            titleMenu.galleryButton.onClick.AddListener(gallery.Open);
            titleMenu.settingsButton.onClick.AddListener(settingsMenu.Open);
            titleMenu.quitButton.onClick.AddListener(() => confirmDialog.Show("게임을 종료할까요?", QuitGame));
        }

        // ───────────────────────── 입력 ─────────────────────────

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && CloseTopMenu()) return;

            if (!InGame || AnyMenuOpen)
            {
                skipHeld = false;
                return;
            }

            // 버튼 클릭 후 스페이스/엔터가 그 버튼을 다시 누르지 않도록 선택 해제
            var es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject != null) es.SetSelectedGameObject(null);

            skipHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                OnScreenClick();
            if (Input.GetKeyDown(KeyCode.S))
                ToggleStats();
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.H))
                SetUIHidden(!uiHidden);
            if (Input.mouseScrollDelta.y > 0f && !uiHidden)
                backlog.Open();
        }

        void OnScreenClick()
        {
            if (uiHidden) { SetUIHidden(false); return; }
            if (skipToggle) { skipToggle = false; UpdateToggleVisuals(); return; }
            advanceRequested = true;
        }

        bool CloseTopMenu()
        {
            if (confirmDialog.IsOpen) { confirmDialog.Close(); return true; }
            if (gallery.IsOpen && gallery.ViewerOpen) { gallery.CloseViewer(); return true; }
            if (gallery.IsOpen) { gallery.Close(); return true; }
            if (backlog.IsOpen) { backlog.Close(); return true; }
            if (settingsMenu.IsOpen) { settingsMenu.Close(); return true; }
            if (saveLoadMenu.IsOpen) { saveLoadMenu.Close(); return true; }
            if (titleMenu.ModeSelectOpen) { titleMenu.CloseModeSelect(); return true; }
            return false;
        }

        void CloseAllMenus()
        {
            confirmDialog.Close();
            backlog.Close();
            settingsMenu.gameObject.SetActive(false);
            saveLoadMenu.Close();
            gallery.Close();
        }

        void ToggleAuto()
        {
            AutoMode = !AutoMode;
            if (AutoMode) skipToggle = false;
            UpdateToggleVisuals();
        }

        void ToggleSkip()
        {
            skipToggle = !skipToggle;
            if (skipToggle) AutoMode = false;
            UpdateToggleVisuals();
        }

        void ToggleStats()
        {
            hud.ToggleStats();
            UpdateToggleVisuals();
        }

        void UpdateToggleVisuals()
        {
            dialogue.SetToggle(dialogue.autoButton, AutoMode);
            dialogue.SetToggle(dialogue.skipButton, skipToggle);
            dialogue.SetToggle(dialogue.statButton, hud.StatsOpen);
        }

        void SetUIHidden(bool hidden)
        {
            uiHidden = hidden;
            dialogue.SetVisible(!hidden);
            hud.gameObject.SetActive(!hidden);
        }

        // ───────────────────────── 게임 흐름 ─────────────────────────

        public void NewGame(int mode)
        {
            string startScript = modes[mode].startScript;
            var s = VNScript.Load(startScript);
            if (s == null) return;
            ResetGameState();
            modeIndex = mode;

            int start = 0;
            if (!string.IsNullOrEmpty(startLabel) && !s.labels.TryGetValue(startLabel, out start))
            {
                Debug.LogWarning($"[VN] 시작 라벨 '{startLabel}'을(를) {startScript}.txt 에서 찾을 수 없어 처음부터 시작합니다.");
                start = 0;
            }
            EnterGame();
            runner = StartCoroutine(Run(s, start));
        }

        public void ReturnToTitle()
        {
            ResetGameState();
            ShowTitle();
        }

        void ShowTitle()
        {
            InGame = false;
            gameUI.SetActive(false);
            titleMenu.Show();
            if (!string.IsNullOrEmpty(titleBgm)) audioPlayer.PlayBgm(titleBgm);
        }

        void EnterGame()
        {
            InGame = true;
            titleMenu.Hide();
            gameUI.SetActive(true);
        }

        void ResetGameState()
        {
            if (runner != null) { StopCoroutine(runner); runner = null; }
            script = null;
            variables.Clear();
            stage.ClearAll();
            audioPlayer.StopBgm(0f);
            backlog.Clear();
            dialogue.Clear();
            choiceMenu.Hide();
            hud.SetVisible(false);
            hud.SetDay("");
            hud.SetGauges(null);
            AutoMode = false;
            skipToggle = false;
            UpdateToggleVisuals();
            SetUIHidden(false);
            lastSpeaker = null;
            lastText = "";
        }

        void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        IEnumerator Run(VNScript s, int startIndex)
        {
            script = s;
            index = startIndex;
            endRequested = false;

            while (!endRequested && script != null && index < script.lines.Count)
            {
                currentIndex = index;
                VNLine line = script.lines[index++];
                switch (line.type)
                {
                    case VNLineType.Dialogue:
                        yield return ShowDialogue(line);
                        break;
                    case VNLineType.Choice:
                        yield return ShowChoice(line);
                        break;
                    case VNLineType.Command:
                        float wait = Execute(line);
                        if (wait > 0f) yield return new WaitForSeconds(wait);
                        break;
                }
            }

            runner = null;
            ReturnToTitle();
        }

        IEnumerator ShowDialogue(VNLine line)
        {
            string speaker = line.speaker;
            string text = Interpolate(line.text);
            Color nameColor = VNScript.GetNameColor(speaker);

            if (speaker != null && !string.IsNullOrEmpty(line.expression))
                stage.SetExpression(speaker, line.expression);
            stage.Focus(speaker);

            lastSpeaker = speaker;
            lastText = text;
            backlog.Add(speaker, text, nameColor);
            dialogue.Show(speaker, text, nameColor, SkipMode);

            advanceRequested = false;
            float autoTimer = 0f;
            while (true)
            {
                if (SkipMode && !AnyMenuOpen)
                {
                    dialogue.CompleteTyping();
                    yield return new WaitForSeconds(0.05f);
                    break;
                }
                if (advanceRequested)
                {
                    advanceRequested = false;
                    if (dialogue.IsTyping) dialogue.CompleteTyping();
                    else break;
                }
                else if (AutoMode && !dialogue.IsTyping && !AnyMenuOpen)
                {
                    autoTimer += Time.deltaTime;
                    if (autoTimer >= VNSettings.AutoDelay + text.Length * 0.02f) break;
                }
                yield return null;
            }
        }

        IEnumerator ShowChoice(VNLine line)
        {
            skipToggle = false;
            UpdateToggleVisuals();

            choiceResult = -1;
            var texts = new List<string>();
            foreach (var o in line.options) texts.Add(Interpolate(o.text));
            choiceMenu.Show(texts, i => choiceResult = i);

            while (choiceResult < 0) yield return null;

            choiceMenu.Hide();
            var chosen = line.options[choiceResult];
            backlog.Add(null, "▶ " + texts[choiceResult], Color.white);
            if (!string.IsNullOrEmpty(chosen.target)) Jump(chosen.target, line);
        }

        // ───────────────────────── 명령어 ─────────────────────────

        /// <returns>명령 실행 후 기다릴 시간(초)</returns>
        float Execute(VNLine l)
        {
            string[] a = l.args;
            bool skip = SkipMode;

            switch (l.command)
            {
                case "bg":
                {
                    float d = skip ? 0f : ArgFloat(a, 1, 0.5f);
                    stage.SetBackground(Arg(a, 0), d);
                    return d;
                }
                case "show":
                    if (a.Length == 0) return Error(l, "사용법: @show 캐릭터 [표정] [left/center/right]");
                    float? pos = a.Length >= 3 ? VNStage.ParsePosition(a[2], 0.5f) : (float?)null;
                    stage.ShowCharacter(a[0], Arg(a, 1), pos, skip ? 0f : 0.3f);
                    return 0f;

                case "hide":
                    if (a.Length == 0 || a[0] == "all") stage.HideAll(skip ? 0f : 0.3f);
                    else stage.HideCharacter(a[0], skip ? 0f : 0.3f);
                    return 0f;

                case "cg":
                {
                    float d = skip ? 0f : ArgFloat(a, 1, 0.5f);
                    if (a.Length == 0 || a[0] == "off") stage.HideCG(d);
                    else
                    {
                        stage.ShowCG(a[0], d);
                        VNUnlocks.UnlockCG(a[0]); // 갤러리에 해금
                    }
                    return d;
                }

                case "ending":
                    if (a.Length == 0) return Error(l, "사용법: @ending 엔딩 이름");
                    VNUnlocks.UnlockEnding(string.Join(" ", a));
                    return 0f;
                case "bgm":
                    if (a.Length == 0 || a[0] == "off" || a[0] == "stop") audioPlayer.StopBgm(ArgFloat(a, 1, 1f));
                    else audioPlayer.PlayBgm(a[0]);
                    return 0f;

                case "se":
                    if (a.Length > 0 && !skip) audioPlayer.PlaySe(a[0]);
                    return 0f;

                case "wait":
                    return skip ? 0f : ArgFloat(a, 0, 1f);

                case "jump":
                    if (a.Length == 0) return Error(l, "사용법: @jump 라벨  /  @jump 파일  /  @jump 파일:라벨");
                    Jump(a[0], l);
                    return 0f;

                case "set":
                    DoSet(l);
                    return 0f;

                case "if":
                    DoIf(l);
                    return 0f;

                case "day":
                    hud.SetDay(string.Join(" ", a));
                    return 0f;

                case "gauge":
                    hud.SetGauges(a);
                    return 0f;

                case "hud":
                    hud.SetVisible(a.Length == 0 || a[0] != "off");
                    return 0f;

                case "end":
                    endRequested = true;
                    return 0f;

                default:
                    return Error(l, $"알 수 없는 명령어 @{l.command}");
            }
        }

        void Jump(string target, VNLine from)
        {
            string file = null, label = target;
            int colon = target.IndexOf(':');
            if (colon >= 0)
            {
                file = target.Substring(0, colon).Trim();
                label = target.Substring(colon + 1).Trim();
            }
            else if (!script.labels.ContainsKey(target))
            {
                file = target; // 현재 파일에 그런 라벨이 없으면 파일 이름으로 간주
                label = null;
            }

            if (!string.IsNullOrEmpty(file))
            {
                var s = VNScript.Load(file);
                if (s == null) { Error(from, $"이동할 파일 '{file}'이(가) 없습니다."); return; }
                script = s;
            }

            if (string.IsNullOrEmpty(label)) index = 0;
            else if (script.labels.TryGetValue(label, out int li)) index = li;
            else Error(from, $"라벨 '{label}'을(를) {script.name}.txt 에서 찾을 수 없습니다.");
        }

        void DoSet(VNLine l)
        {
            var m = setRegex.Match(string.Join(" ", l.args));
            if (!m.Success) { Error(l, "사용법: @set 변수 = 값  (또는 +=, -=)"); return; }

            string key = m.Groups[1].Value;
            int value = ResolveValue(m.Groups[3].Value);
            variables.TryGetValue(key, out int cur);
            switch (m.Groups[2].Value)
            {
                case "=": cur = value; break;
                case "+=": cur += value; break;
                case "-=": cur -= value; break;
            }
            int before = GetVariable(key);
            variables[key] = cur;
            hud.OnVariableChanged(key, before, cur);
        }

        void DoIf(VNLine l)
        {
            string joined = string.Join(" ", l.args);
            int arrow = joined.LastIndexOf("->");
            if (arrow < 0) { Error(l, "사용법: @if 변수 >= 값 -> 라벨"); return; }

            var m = condRegex.Match(joined.Substring(0, arrow));
            if (!m.Success) { Error(l, "조건식을 이해할 수 없습니다. 예: @if 호감도 >= 10 -> 라벨"); return; }

            int left = ResolveValue(m.Groups[1].Value);
            int right = ResolveValue(m.Groups[3].Value);
            bool ok;
            switch (m.Groups[2].Value)
            {
                case ">=": ok = left >= right; break;
                case "<=": ok = left <= right; break;
                case ">": ok = left > right; break;
                case "<": ok = left < right; break;
                case "!=": ok = left != right; break;
                default: ok = left == right; break;
            }
            if (ok) Jump(joined.Substring(arrow + 2).Trim(), l);
        }

        int ResolveValue(string s)
        {
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)) return v;
            variables.TryGetValue(s, out v);
            return v;
        }

        /// <summary>{남자일때|여자일때} → 성별에 맞는 말,  {변수} → 변수 값</summary>
        string Interpolate(string text)
        {
            bool female = GetVariable(genderVariable) == 2;
            text = genderRegex.Replace(text, m => female ? m.Groups[2].Value : m.Groups[1].Value);
            return interpolateRegex.Replace(text, m =>
                variables.TryGetValue(m.Groups[1].Value, out int v) ? v.ToString() : m.Value);
        }

        float Error(VNLine l, string msg)
        {
            Debug.LogError($"[VN] {script?.name}.txt {l.sourceLine}번째 줄: {msg}");
            return 0f;
        }

        static string Arg(string[] a, int i) => i < a.Length ? a[i] : null;

        static float ArgFloat(string[] a, int i, float fallback) =>
            i < a.Length && float.TryParse(a[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ? f : fallback;

        // ───────────────────────── 세이브 / 로드 ─────────────────────────

        public int GetVariable(string key) => variables.TryGetValue(key, out int v) ? v : 0;
        public void SetVariable(string key, int value) => variables[key] = value;

        public VNSaveData CaptureState()
        {
            var d = new VNSaveData
            {
                scriptName = script.name,
                lineIndex = currentIndex,
                background = stage.CurrentBackground,
                cg = stage.CurrentCG,
                bgm = audioPlayer.CurrentBgm,
                characters = stage.GetStates(),
                speaker = stage.CurrentSpeaker,
                savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                dayLabel = hud.DayLabel,
                hudVisible = hud.Visible,
                gauges = hud.GaugeList,
                modeName = modes[modeIndex].name,
                startScript = modes[modeIndex].startScript,
                preview = string.IsNullOrEmpty(lastSpeaker) ? lastText : $"{lastSpeaker}: {lastText}",
            };
            foreach (var kv in variables) d.variables.Add(new VNVariable { key = kv.Key, value = kv.Value });
            return d;
        }

        public void SaveToSlot(int slot)
        {
            if (!InGame || script == null) return;
            VNSaveSystem.Save(slot, CaptureState());
            dialogue.ShowNotice(slot == VNSaveSystem.QuickSlot ? "빠른 저장 완료" : $"{slot}번 슬롯에 저장했습니다");
        }

        public void LoadFromSlot(int slot)
        {
            var d = VNSaveSystem.Load(slot);
            if (d != null) LoadState(d);
        }

        public void LoadState(VNSaveData d)
        {
            var s = VNScript.Load(d.scriptName);
            if (s == null) return;

            ResetGameState();
            // 이름표 색(@color)은 보통 모드의 첫 파일에 있으므로 한 번 읽어 둠
            if (!string.IsNullOrEmpty(d.startScript)) VNScript.Load(d.startScript);
            modeIndex = Mathf.Max(0, Array.FindIndex(modes, m => m.startScript == d.startScript));
            foreach (var v in d.variables) variables[v.key] = v.value;
            stage.SetBackground(d.background, 0f);
            foreach (var c in d.characters) stage.ShowCharacter(c.name, c.expression, c.x, 0f);
            stage.Focus(d.speaker);
            if (!string.IsNullOrEmpty(d.cg)) stage.ShowCG(d.cg, 0f);
            if (!string.IsNullOrEmpty(d.bgm)) audioPlayer.PlayBgm(d.bgm);
            hud.SetGauges(string.IsNullOrEmpty(d.gauges) ? null : d.gauges.Split(' '));
            hud.SetDay(d.dayLabel);
            hud.SetVisible(d.hudVisible);

            CloseAllMenus();
            EnterGame();
            runner = StartCoroutine(Run(s, Mathf.Clamp(d.lineIndex, 0, s.lines.Count)));
        }

        void QuickSave() => SaveToSlot(VNSaveSystem.QuickSlot);

        void QuickLoad()
        {
            if (!VNSaveSystem.Exists(VNSaveSystem.QuickSlot))
            {
                dialogue.ShowNotice("빠른 저장 데이터가 없습니다");
                return;
            }
            confirmDialog.Show("빠른 저장 데이터를 불러올까요?", () => LoadFromSlot(VNSaveSystem.QuickSlot));
        }
    }
}
