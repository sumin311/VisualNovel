using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    public class VNSettingsMenu : MonoBehaviour
    {
        public Slider textSpeedSlider;
        public Slider autoDelaySlider;
        public Slider bgmSlider;
        public Slider seSlider;
        public Text textSpeedValue;
        public Text autoDelayValue;
        public Text bgmValue;
        public Text seValue;
        public Toggle fullscreenToggle;
        public Button closeButton;

        public bool IsOpen => gameObject.activeSelf;

        bool initialized;

        void Init()
        {
            if (initialized) return;
            initialized = true;
            textSpeedSlider.onValueChanged.AddListener(v => { VNSettings.TextSpeed = v; UpdateLabels(); });
            autoDelaySlider.onValueChanged.AddListener(v => { VNSettings.AutoDelay = v; UpdateLabels(); });
            bgmSlider.onValueChanged.AddListener(v => { VNSettings.BgmVolume = v; ApplyAudio(); UpdateLabels(); });
            seSlider.onValueChanged.AddListener(v => { VNSettings.SeVolume = v; ApplyAudio(); UpdateLabels(); });
            fullscreenToggle.onValueChanged.AddListener(v => Screen.fullScreen = v);
            closeButton.onClick.AddListener(Close);
        }

        public void Open()
        {
            Init();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            textSpeedSlider.SetValueWithoutNotify(VNSettings.TextSpeed);
            autoDelaySlider.SetValueWithoutNotify(VNSettings.AutoDelay);
            bgmSlider.SetValueWithoutNotify(VNSettings.BgmVolume);
            seSlider.SetValueWithoutNotify(VNSettings.SeVolume);
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
            UpdateLabels();
        }

        public void Close()
        {
            VNSettings.Save();
            gameObject.SetActive(false);
        }

        void ApplyAudio()
        {
            if (VNEngine.Instance != null) VNEngine.Instance.audioPlayer.ApplyVolume();
        }

        void UpdateLabels()
        {
            textSpeedValue.text = $"{VNSettings.TextSpeed:0} 글자/초";
            autoDelayValue.text = $"{VNSettings.AutoDelay:0.0} 초";
            bgmValue.text = $"{VNSettings.BgmVolume * 100f:0}%";
            seValue.text = $"{VNSettings.SeVolume * 100f:0}%";
        }
    }
}
