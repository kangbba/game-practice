using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 화면을 덮고 뜨는 창의 공통 바탕. 열림 상태를 리액티브로 내놓는다 —
    /// "열려 있는 동안 게임 중단" 같은 정책은 창이 아니라 바깥에서 이 값에 건다.
    /// </summary>
    public abstract class PopupWindow : MonoBehaviour
    {
        private readonly ReactiveProperty<bool> _isOpen = new ReactiveProperty<bool>(false);

        public ReadOnlyReactiveProperty<bool> IsOpen => _isOpen;

        public void Show()
        {
            OnShow();
            gameObject.SetActive(true);
            _isOpen.Value = true;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            _isOpen.Value = false;
        }

        /// <summary>열리기 직전. 탭·선택 같은 창 안 상태를 처음으로 돌려놓는 자리.</summary>
        protected virtual void OnShow()
        {
        }

        protected virtual void OnDestroy()
        {
            _isOpen.Dispose();
        }
    }
}
