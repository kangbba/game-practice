using UnityEngine;

namespace Sayne
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class BattleHUDSafeArea : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _currentSafeArea;
        private Vector2Int _currentScreenSize;

        private void OnEnable()
        {
            _rect = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            if (_currentSafeArea != Screen.safeArea || _currentScreenSize.x != Screen.width || _currentScreenSize.y != Screen.height)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            _currentSafeArea = Screen.safeArea;
            _currentScreenSize = new Vector2Int(Screen.width, Screen.height);
            _rect.anchorMin = new Vector2(_currentSafeArea.xMin / Screen.width, _currentSafeArea.yMin / Screen.height);
            _rect.anchorMax = new Vector2(_currentSafeArea.xMax / Screen.width, _currentSafeArea.yMax / Screen.height);
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
