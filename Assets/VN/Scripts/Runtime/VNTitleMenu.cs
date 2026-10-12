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
        public string subtitle = "~ 꼬실 것인가, 꼬심당할 것인가 ~";
        [Tooltip("Assets/Resources/Backgrounds/ 안의 이미지 이름")]
        public string backgroundName = "타이틀";

        [Header("참조 (씬 생성 시 자동 연결)")]
        public Image background;
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
                    $"<b>{modes[i].name}</b>\n<size=26><color=#d8d0e8>{modes[i].description}</color></size>";
                b.onClick.AddListener(() => { CloseModeSelect(); onSelect(idx); });
                modeButtons.Add(b.gameObject);
            }
            modePanel.SetActive(true);
        }

        public void CloseModeSelect()
        {
            if (modePanel != null) modePanel.SetActive(false);
        }
    }
}
