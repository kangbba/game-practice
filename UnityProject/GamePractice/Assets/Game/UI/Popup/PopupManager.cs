using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Sayne
{
    /// <summary>
    /// 팝업 매니저가 찍어내는 프리팹. 무엇을 쓸지 런타임에 종류로 고르는 것이라 목록이 아니라 묻는 문으로 낸다.
    /// </summary>
    public interface IPopupAssets
    {
        PopupWindow GetPopupPrefab(PopupType type);
    }

    /// <summary>
    /// 팝업의 주인. 열 때 종류(PopupType)에 짝지은 프리팹으로 창을 만들어 Bind 된 대로 볼 것을 넣어 주고, 닫히면 파괴한다.
    /// 창은 열려 있는 동안만 산다 — 다시 열면 새로 만들어 처음 상태로 뜬다. 열린 동안은 게임을 멈춘다.
    /// 여는 문은 여기 하나다 — 블러를 깔지는 종류가 정하고, 그 순서(투명하게 열기 → 화면 찍기 → 드러내기)는 여기서만 돈다.
    /// 닫기는 창 안의 X 가 스스로 해도 된다. 창은 닫혔다고 알리기만 하고, 치우는 건 여기서 한다.
    /// </summary>
    public class PopupManager : ManagerBase
    {
        private const string RootName = "PopupCanvas";

        /// <summary>전투 HUD(0) 위, 튜토리얼(90)·UI 연출(100) 아래.</summary>
        private const int SortingOrder = 50;

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        private readonly ScreenBlurManager _screenBlurManager;
        private readonly IPopupAssets _assets;

        // 창마다 무엇을 넣어 줄지. 창이 보는 것은 조립하는 쪽(BattleScope)이 Bind 로 알려 준다 — 여기는 창이 뭘 보는지 모른다.
        private readonly Dictionary<PopupType, Action<PopupWindow>> _injectors = new Dictionary<PopupType, Action<PopupWindow>>();

        private Canvas _canvas;

        public PopupManager(ScreenBlurManager screenBlurManager, IPopupAssets assets)
        {
            _screenBlurManager = screenBlurManager;
            _assets = assets;
        }

        protected override void OnInit()
        {
            var root = new GameObject(RootName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            _canvas = root.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = SortingOrder;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
        }

        protected override void OnRelease()
        {
            // 게임이 끝나 씬이 내려갈 때는 유니티가 캔버스를 먼저 치운다. 그때는 이미 없는 것을 또 치우지 않는다.
            // 열려 있던 창은 캔버스와 같이 사라진다.
            if (_canvas != null)
            {
                Object.Destroy(_canvas.gameObject);
            }
        }

        /// <summary>창을 새로 만들어 볼 것을 넣고 연다. 닫히면 멈춤을 풀고 파괴한다.</summary>
        public void Open(PopupType type)
        {
            var popup = Object.Instantiate(_assets.GetPopupPrefab(type), _canvas.transform, false);
            _injectors[type](popup);

            var pause = Pause.Hold();
            popup.Open();

            popup.IsOpen
                .Where(isOpen => !isOpen)
                .Take(1)
                .Subscribe((popup, pause), (_, state) =>
                {
                    state.pause.Dispose();
                    Object.Destroy(state.popup.gameObject);
                });

            RevealAsync(type, popup).Forget();
        }

        /// <summary>
        /// 이 종류의 창을 열 때 무엇을 넣어 줄지 등록한다. 창 타입이 프리팹과 어긋나면 열 때 캐스트에서 바로 터진다.
        /// 돌려준 것을 치우면 등록이 풀린다 — 창이 보는 매니저가 먼저 사라지는 쪽(전투)이면 그쪽이 사라질 때 같이 푼다.
        /// </summary>
        public IDisposable Bind<TWindow>(PopupType type, Action<TWindow> inject) where TWindow : PopupWindow
        {
            _injectors.Add(type, popup => inject((TWindow)popup));
            return Disposable.Create((self: this, type), state => state.self._injectors.Remove(state.type));
        }

        /// <summary>창은 투명하게 먼저 열려 있다. 그 프레임의 화면을 찍어 깐 뒤 드러낸다 — 창 자신이 배경에 찍히지 않는다.</summary>
        private async UniTaskVoid RevealAsync(PopupType type, PopupWindow popup)
        {
            var blurredScreen = PopupTypes.IsBlurred(type)
                ? await _screenBlurManager.CaptureAsync(LifeToken)
                : null;

            popup.Reveal(blurredScreen);
        }
    }
}
