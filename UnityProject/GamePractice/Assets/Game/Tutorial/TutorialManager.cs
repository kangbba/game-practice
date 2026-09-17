using System;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 안내 대사를 트는 창구. 화면 아래 초상화 대사와 캐릭터 머리 위 말풍선 두 가지를 틀 수 있다.
    /// 무엇을 언제 말할지는 부르는 쪽이 정한다 — 여기는 틀고, 트는 동안 게임을 멈출 뿐이다.
    /// 한 번에 하나만 튼다. 이어서 틀려면 앞의 것을 await 한 뒤에 부른다.
    /// </summary>
    public class TutorialManager : ManagerBase
    {
        private const string RootName = "TutorialRoot";

        /// <summary>전투 HUD(0)와 창들 위, 궁극기 컷인(100) 아래.</summary>
        private const int SortingOrder = 90;

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        /// <summary>발에서 머리까지의 월드 오프셋. HP 바와 같은 지점이다.</summary>
        private static readonly Vector3 BubbleWorldOffset = new Vector3(0f, 2.2f, 0f);

        /// <summary>머리 위 HP 바를 가리지 않도록 화면상으로 더 띄우는 값.</summary>
        private static readonly Vector2 BubbleScreenOffset = new Vector2(0f, 36f);

        private readonly PauseManager _pauseManager;
        private readonly CameraManager _cameraManager;
        private readonly TutorialWidget _widgetPrefab;
        private readonly OverlaySpeechBubble _overlayBubblePrefab;

        private readonly ReactiveProperty<bool> _isPlaying = new ReactiveProperty<bool>(false);

        private Canvas _canvas;
        private TutorialWidget _widget;
        private OverlaySpeechBubble _overlayBubble;

        public ReadOnlyReactiveProperty<bool> IsPlaying => _isPlaying;

        public TutorialManager(PauseManager pauseManager, CameraManager cameraManager,
            TutorialWidget widgetPrefab, OverlaySpeechBubble overlayBubblePrefab)
        {
            _pauseManager = pauseManager;
            _cameraManager = cameraManager;
            _widgetPrefab = widgetPrefab;
            _overlayBubblePrefab = overlayBubblePrefab;
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

            _overlayBubble = UnityEngine.Object.Instantiate(_overlayBubblePrefab, _canvas.transform);
            _widget = UnityEngine.Object.Instantiate(_widgetPrefab, _canvas.transform);

            _pauseManager.PauseWhile(_isPlaying);
        }

        protected override void OnRelease()
        {
            UnityEngine.Object.Destroy(_canvas.gameObject);

            _canvas = null;
            _widget = null;
            _overlayBubble = null;
            _isPlaying.Dispose();
        }

        /// <summary>화면 아래에 초상화와 함께 대사를 튼다. 플레이어가 넘기면 끝난다.</summary>
        public async UniTask PlayAsync(Sprite portrait, string text)
        {
            Begin();
            await _widget.PlayAsync(portrait, text, LifeToken);
            _isPlaying.Value = false;
        }

        /// <summary>대상의 머리 위에 말풍선을 튼다. 플레이어가 넘기면 끝난다.</summary>
        public async UniTask PlayAsync(Transform target, string text)
        {
            Begin();
            _overlayBubble.Attach(_cameraManager.Camera, target, BubbleWorldOffset, BubbleScreenOffset);
            await _overlayBubble.PlayAsync(text, LifeToken);
            _isPlaying.Value = false;
        }

        private void Begin()
        {
            if (_isPlaying.Value)
            {
                throw new InvalidOperationException("TutorialManager: 앞의 대사가 끝나기 전에 PlayAsync 를 또 불렀다");
            }

            _isPlaying.Value = true;
        }
    }
}
