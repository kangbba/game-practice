using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 화면을 덮고 뜨는 창의 공통 바탕. 열림 상태를 리액티브로 내놓는다 —
    /// "열려 있는 동안 게임 중단" 같은 정책은 창이 아니라 바깥에서 이 값에 건다.
    ///
    /// 열릴 때 화면 전체를 덮는 막을 창 바로 뒤에 깐다. 뒤가 어두워지는 건 덤이고,
    /// 진짜 목적은 뒤쪽 버튼이 안 눌리게 막는 것이다 — 창을 열어둔 채 다른 창을 또 열 수 없다.
    /// </summary>
    public abstract class PopupWindow : MonoBehaviour
    {
        private static readonly Color BackdropColor = new Color(0f, 0f, 0.05f, 0.55f);

        private readonly ReactiveProperty<bool> _isOpen = new ReactiveProperty<bool>(false);

        private Image _backdrop;

        public ReadOnlyReactiveProperty<bool> IsOpen => _isOpen;

        public void Show()
        {
            OnShow();

            ShowBackdrop();
            gameObject.SetActive(true);

            _isOpen.Value = true;
        }

        public void Hide()
        {
            gameObject.SetActive(false);

            if (_backdrop != null)
            {
                _backdrop.gameObject.SetActive(false);
            }

            _isOpen.Value = false;
        }

        /// <summary>열리기 직전. 탭·선택 같은 창 안 상태를 처음으로 돌려놓는 자리.</summary>
        protected virtual void OnShow()
        {
        }

        /// <summary>막은 창과 형제로 두고 바로 앞 순번에 꽂는다 — 창보다 뒤에, 나머지 UI 보다 앞에 그려진다.</summary>
        private void ShowBackdrop()
        {
            if (_backdrop == null)
            {
                _backdrop = CreateBackdrop();
            }

            _backdrop.transform.SetSiblingIndex(transform.GetSiblingIndex());
            _backdrop.gameObject.SetActive(true);
        }

        private Image CreateBackdrop()
        {
            var root = new GameObject($"{name}Backdrop", typeof(RectTransform), typeof(Image));

            var rect = (RectTransform)root.transform;
            rect.SetParent(transform.parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = root.GetComponent<Image>();
            image.color = BackdropColor;
            image.raycastTarget = true;

            return image;
        }

        protected virtual void OnDestroy()
        {
            _isOpen.Dispose();
        }
    }
}
