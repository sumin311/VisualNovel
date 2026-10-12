using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    /// <summary>다른 그래픽의 색을 그대로 따라감 (메뉴 탭 아이콘 위 글자 등)</summary>
    [RequireComponent(typeof(Graphic))]
    public class VNTintFollower : MonoBehaviour
    {
        public Graphic source;
        Graphic self;

        void Awake() => self = GetComponent<Graphic>();

        void LateUpdate()
        {
            if (source != null && self != null && self.color != source.color) self.color = source.color;
        }
    }
}
