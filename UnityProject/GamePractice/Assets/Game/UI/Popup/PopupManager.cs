using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 팝업의 주인. 종류(PopupType)에 짝지은 프리팹으로 창을 만들고, 여닫고, 열린 동안 게임을 멈춘다.
    /// 여는 문은 여기 하나다 — 블러를 깔지는 종류가 정하고, 그 순서(투명하게 열기 → 화면 찍기 → 드러내기)는 여기서만 돈다.
    /// 닫기는 창 안의 X 가 스스로 해도 된다. 열림 상태는 창이 들고 있고 멈춤은 그걸 구독한다.
    /// </summary>
    public class PopupManager : ManagerBase
    {
        private const string RootName = "PopupCanvas";

        /// <summary>전투 HUD(0) 위, 튜토리얼(90)·UI 연출(100) 아래.</summary>
        private const int SortingOrder = 50;

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        private readonly PauseManager _pauseManager;
        private readonly ScreenBlurManager _screenBlurManager;
        private readonly UIAssetManager _uiAssetManager;

        private readonly Dictionary<PopupType, PopupWindow> _popups = new Dictionary<PopupType, PopupWindow>();

        private Canvas _canvas;

        public PopupManager(PauseManager pauseManager, ScreenBlurManager screenBlurManager, UIAssetManager uiAssetManager)
        {
            _pauseManager = pauseManager;
            _screenBlurManager = screenBlurManager;
            _uiAssetManager = uiAssetManager;
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
            if (_canvas != null)
            {
                Object.Destroy(_canvas.gameObject);
            }

            _popups.Clear();
        }

        /// <summary>
        /// 종류에 짝지은 프리팹으로 창을 만들어 둔다. 닫힌 채로 태어난다.
        /// 창마다 필요한 매니저가 달라 Init 은 만든 쪽이 돌려받은 창에 한다.
        /// </summary>
        public T Create<T>(PopupType type) where T : PopupWindow
        {
            var popup = Object.Instantiate(_uiAssetManager.GetPopupPrefab(type), _canvas.transform, false);
            popup.gameObject.SetActive(false);

            _popups.Add(type, popup);
            _pauseManager.PauseWhile(popup.IsOpen);

            return (T)popup;
        }

        public void Open(PopupType type)
        {
            OpenAsync(type).Forget();
        }

        public void Close(PopupType type)
        {
            _popups[type].Close();
        }

        /// <summary>창을 투명하게 먼저 열고, 그 프레임의 화면을 찍어 깐 뒤 드러낸다 — 창 자신이 배경에 찍히지 않는다.</summary>
        private async UniTaskVoid OpenAsync(PopupType type)
        {
            var popup = _popups[type];
            popup.Open();

            var blurredScreen = PopupTypes.IsBlurred(type)
                ? await _screenBlurManager.CaptureAsync(LifeToken)
                : null;

            popup.Reveal(blurredScreen);
        }
    }
}
