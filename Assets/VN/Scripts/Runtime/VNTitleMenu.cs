using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    public class VNTitleMenu : MonoBehaviour
    {
        [Header("타이틀 화면 내용 (여기서 수정)")]
        public string gameTitle = "승주 공략 대작전";
        public string subtitle = "~ 친구에서, 그 다음으로 ~";
        [Tooltip("Assets/Resources/Backgrounds/ 안의 이미지 이름")]
        public string backgroundName = "타이틀";

        [Header("참조 (씬 생성 시 자동 연결)")]
        public Image background;
        public Text titleText;
        public Text subtitleText;
        public Button newGameButton;
        public Button loadButton;
        public Button settingsButton;
        public Button quitButton;

        public void Show()
        {
            gameObject.SetActive(true);
            titleText.text = gameTitle;
            subtitleText.text = subtitle;
            var sp = VNStage.LoadSprite("Backgrounds/" + backgroundName);
            background.sprite = sp;
            background.enabled = sp != null;
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
