using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    public class VNTitleMenu : MonoBehaviour
    {
        [Header("타이틀 화면 내용 (여기서 수정)")]
        public string gameTitle = "모두의 승주";
        public string subtitle = "~ 폭풍을 부르는 승주 ~";
        [Tooltip("Assets/Resources/Backgrounds/ 안의 이미지 이름")]
        public string backgroundName = "타이틀";
        [Tooltip("타이틀 왼쪽에 세울 캐릭터 (Characters/이름_기본.png). 순서: 왼쪽 뒤, 가운데 앞, 오른쪽 뒤. 비우면 안 띄움")]
        public string[] titleCharacters = { "서연", "승주", "기주" };

        [Header("참조 (씬 생성 시 자동 연결)")]
        public Image background;
        public Image[] characterSlots;
        public Text titleText;
        public Text subtitleText;
        public Button newGameButton;
        public Button loadButton;
        public Button galleryButton;
        public Button settingsButton;
        public Button quitButton;

        [Header("모드 선택 창 (씬 생성 시 자동 연결)")]
        public GameObject modePanel;
        public RectTransform modeContainer;
        public Button modeTemplate;
        public Button modeBackButton;
        [Tooltip("모드 카드 강조색 (순서대로)")]
        public Color[] modeColors = { new Color(0.95f, 0.48f, 0.64f), new Color(0.73f, 0.65f, 0.91f) };

        readonly List<GameObject> modeButtons = new List<GameObject>();

        public bool ModeSelectOpen => modePanel != null && modePanel.activeSelf;

        void Awake()
        {
            if (modeBackButton != null) modeBackButton.onClick.AddListener(CloseModeSelect);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            CloseModeSelect();
            titleText.text = gameTitle;
            subtitleText.text = subtitle;
            var sp = VNStage.LoadSprite("Backgrounds/" + backgroundName);
            background.sprite = sp;
            background.enabled = sp != null;
            VNStage.Cover(background); // 세로 화면에서도 찌그러지지 않게

            if (characterSlots != null)
                for (int i = 0; i < characterSlots.Length; i++)
                {
                    string n = titleCharacters != null && i < titleCharacters.Length ? titleCharacters[i] : null;
                    Sprite c = string.IsNullOrEmpty(n) ? null
                        : VNStage.LoadSprite($"Characters/{n}_{VNScript.DefaultExpression}", false) ?? VNStage.LoadSprite($"Characters/{n}", false);
                    characterSlots[i].sprite = c;
                    characterSlots[i].enabled = c != null;
                }
        }

        public void Hide() => gameObject.SetActive(false);

        /// <summary>모드 버튼을 만들어 모드 선택 창을 엽니다.</summary>
        public void OpenModeSelect(VNGameMode[] modes, Action<int> onSelect)
        {
            foreach (var b in modeButtons) Destroy(b);
            modeButtons.Clear();
            for (int i = 0; i < modes.Length; i++)
            {
                int idx = i;
                var b = Instantiate(modeTemplate, modeContainer);
                b.gameObject.SetActive(true);
                b.GetComponentInChildren<Text>().text =
                    $"<b>{modes[i].name}</b>\n<size=26><color=#8C6F68>{modes[i].description}</color></size>";
                if (modeColors != null && modeColors.Length > 0)
                {
                    // 카드 왼쪽 강조 띠가 있으면 거기에, 없으면 카드 전체에 색칠
                    var accent = b.transform.Find("Accent");
                    var img = accent != null ? accent.GetComponent<Image>() : b.GetComponent<Image>();
                    img.color = modeColors[i % modeColors.Length];
                }
                b.onClick.AddListener(() => { CloseModeSelect(); onSelect(idx); });
                modeButtons.Add(b.gameObject);
            }
            modePanel.SetActive(true);
            modePanel.transform.SetAsLastSibling();
        }

        public void CloseModeSelect()
        {
            if (modePanel != null) modePanel.SetActive(false);
        }
    }
}
