using UnityEngine;

namespace VN
{
    /// <summary>폰을 세로로 들고 있으면 "가로로 돌려 주세요" 안내를 덮어 띄웁니다.</summary>
    public class VNRotateHint : MonoBehaviour
    {
        public GameObject overlay;

        void Update()
        {
            // 폰에서만 (PC 브라우저 창이 세로로 길어도 막지 않음)
            bool portrait = VNPlatformAdjust.IsMobile && Screen.height > Screen.width;
            if (overlay.activeSelf != portrait)
            {
                overlay.SetActive(portrait);
                if (portrait) transform.SetAsLastSibling(); // 다른 메뉴보다 위에
            }
        }
    }
}
