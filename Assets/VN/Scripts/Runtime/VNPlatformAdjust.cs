using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    /// <summary>
    /// 폰(모바일 브라우저 포함) / 웹에서만 다르게 보이게 하는 설정.
    /// 버튼을 키우거나, 폰·웹에서 의미 없는 메뉴(종료, 전체 화면)를 숨깁니다.
    /// </summary>
    public class VNPlatformAdjust : MonoBehaviour
    {
        [Tooltip("폰에서 크기 배율 (1 = 그대로)")]
        public float mobileScale = 1f;
        [Tooltip("폰에서 이 오브젝트 아래 글자 크기 배율 (1 = 그대로)")]
        public float mobileTextScale = 1f;
        public bool hideOnMobile;
        public bool hideOnWeb;

        public static bool IsMobile => Application.isMobilePlatform;
        public static bool IsWeb => Application.platform == RuntimePlatform.WebGLPlayer;

        void Awake()
        {
            if ((hideOnMobile && IsMobile) || (hideOnWeb && IsWeb))
            {
                gameObject.SetActive(false);
                return;
            }
            if (!IsMobile) return;
            if (!Mathf.Approximately(mobileScale, 1f)) transform.localScale *= mobileScale;
            if (!Mathf.Approximately(mobileTextScale, 1f))
                foreach (var t in GetComponentsInChildren<Text>(true))
                    t.fontSize = Mathf.RoundToInt(t.fontSize * mobileTextScale);
        }
    }
}
