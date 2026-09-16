using UnityEngine;
using UnityEngine.EventSystems;

namespace Sayne
{
    /// <summary>
    /// 터치한 지점에 나타났다가 떼면 사라지는 플로팅 조이스틱.
    /// 이 컴포넌트가 붙은 RectTransform 전체가 터치 영역이다(레이캐스트 타깃 Graphic 필요).
    /// </summary>
    public class FloatingJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _base;
        [SerializeField] private RectTransform _knob;
        [SerializeField] private float _radius = 120f;

        /// <summary>손가락이 화면에 닿아 있는 동안 true.</summary>
        public bool IsActive { get; private set; }

        /// <summary>중심 기준 방향. 크기 0~1.</summary>
        public Vector2 Direction { get; private set; }

        private void Awake()
        {
            Hide();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsActive = true;
            Direction = Vector2.zero;

            _base.gameObject.SetActive(true);
            _base.anchoredPosition = ToLocalPoint(eventData);
            _knob.anchoredPosition = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsActive)
            {
                return;
            }

            var offset = Vector2.ClampMagnitude(ToLocalPoint(eventData) - _base.anchoredPosition, _radius);
            _knob.anchoredPosition = offset;
            Direction = offset / _radius;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Hide();
        }

        private Vector2 ToLocalPoint(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform, eventData.position, eventData.pressEventCamera, out var local);
            return local;
        }

        private void Hide()
        {
            IsActive = false;
            Direction = Vector2.zero;

            if (_base != null)
            {
                _base.gameObject.SetActive(false);
            }
        }
    }
}
