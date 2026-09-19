using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 화면을 덮고 뜨는 창의 공통 바탕. 만들고 열고 파괴하는 건 PopupManager 가 하고, 창은 자기 모습과 열림 상태만 든다.
    /// 열 때마다 새로 태어나므로 창 안 상태(탭·선택)는 Init 에서 처음 모습으로 잡으면 된다.
    ///
    /// 프리팹 구조가 곧 규칙이다: 이 컴포넌트가 붙은 뿌리는 화면을 꽉 채우는 입력 막이고,
    /// 그 아래로 흐린 화면 → 옅은 암막 → 모달 순으로 깔린다. 뒤판이 모달의 부모라 순서가 뒤집힐 일이 없다.
    /// </summary>
    public abstract class PopupWindow : MonoBehaviour
    {
        private const float RevealSeconds = 0.15f;

        [Header("팝업 바탕")]
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RawImage _blur;

        private readonly ReactiveProperty<bool> _isOpen = new ReactiveProperty<bool>(false);

        public ReadOnlyReactiveProperty<bool> IsOpen => _isOpen;

        /// <summary>
        /// 켜기만 하고 아직 보이지 않게 둔다. 매니저가 이 프레임의 화면을 찍은 뒤 Reveal 로 드러낸다.
        /// 열림 상태는 여기서 바로 켠다 — 게임은 여는 그 프레임부터 멈춘다.
        /// </summary>
        public void Open()
        {
            _group.alpha = 0f;
            _isOpen.Value = true;
        }

        /// <summary>흐린 화면을 깔고 드러낸다. null 이면 흐림 없이 암막만 깔린다.</summary>
        public void Reveal(Texture blurredScreen)
        {
            _blur.texture = blurredScreen;
            _blur.enabled = blurredScreen != null;

            _group.DOFade(1f, RevealSeconds)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        /// <summary>닫혔다고 알린다. 매니저가 이걸 듣고 창을 파괴한다.</summary>
        public void Close()
        {
            _isOpen.Value = false;
        }

        protected virtual void OnDestroy()
        {
            _isOpen.Dispose();
        }
    }
}
