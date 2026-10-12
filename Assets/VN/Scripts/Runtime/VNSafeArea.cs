using UnityEngine;

namespace VN
{
    /// <summary>폰의 노치·둥근 모서리를 피해서, 이 영역을 화면의 안전 영역(Screen.safeArea)에 맞춥니다.</summary>
    [RequireComponent(typeof(RectTransform))]
    public class VNSafeArea : MonoBehaviour
    {
        Rect applied;
        Vector2Int screen;

        void Update()
        {
            var sa = Screen.safeArea;
            if (sa == applied && screen.x == Screen.width && screen.y == Screen.height) return;
            applied = sa;
            screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            rt.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
