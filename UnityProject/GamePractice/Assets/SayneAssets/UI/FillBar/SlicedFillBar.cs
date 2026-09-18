using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// Image.fillAmount 대신 RectTransform 의 폭을 줄여서 채우는 게이지.
    /// Filled 타입은 스프라이트를 통째로 늘린 뒤 잘라내기 때문에 양끝 모양이 뭉개진다.
    /// 여기서는 9슬라이스(Sliced) 이미지의 폭만 바꾸므로 테두리와 양끝이 원본 크기 그대로 남는다.
    /// 여백은 부모(FillArea)가 가지고, 이 오브젝트는 좌우 여백 없이 부모를 0~1 로 채운다.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class SlicedFillBar : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float _fillAmount = 1f;

        private RectTransform _rect;

        private RectTransform Rect => _rect ??= (RectTransform)transform;

        /// <summary>채움 비율 0~1. Image.fillAmount 와 같은 의미다.</summary>
        public float FillAmount
        {
            get => _fillAmount;
            set
            {
                _fillAmount = Mathf.Clamp01(value);
                Apply();
            }
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Apply()
        {
            var rect = Rect;
            rect.anchorMin = new Vector2(0f, rect.anchorMin.y);
            rect.anchorMax = new Vector2(_fillAmount, rect.anchorMax.y);
            rect.offsetMin = new Vector2(0f, rect.offsetMin.y);
            rect.offsetMax = new Vector2(0f, rect.offsetMax.y);
        }
    }
}
