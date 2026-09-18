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

        /// <summary>머리 꼭대기에서 말풍선 꼬리 끝까지의 월드 간격. 머리 위 HP 바와 같은 원리로 잰다 — 키는 캐릭터마다 다르다.</summary>
        private const float BubbleHeadGap = 0.25f;

        /// <summary>머리 위 HP 바(화면상 22 위)를 가리지 않도록 그보다 더 띄우는 값. 기준 해상도 단위.</summary>
        private static readonly Vector2 BubbleScreenOffset = new Vector2(0f, 64f);

        private readonly PauseManager _pauseManager;
        private readonly CameraManager _cameraManager;
        private readonly SpeechBubbleWidget _widgetPrefab;
        private readonly OverlaySpeechBubble _overlayBubblePrefab;

        private readonly ReactiveProperty<bool> _isPlaying = new ReactiveProperty<bool>(false);

        private Canvas _canvas;
        private SpeechBubbleWidget _widget;
        private OverlaySpeechBubble _overlayBubble;

        public ReadOnlyReactiveProperty<bool> IsPlaying => _isPlaying;

        public TutorialManager(PauseManager pauseManager, CameraManager cameraManager,
            SpeechBubbleWidget widgetPrefab, OverlaySpeechBubble overlayBubblePrefab)
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

        /// <summary>
        /// 말하는 이의 머리 위에 초상화 붙은 말풍선을 튼다. 플레이어가 넘기면 끝난다.
        /// 붙는 자리는 머리 위 HP 바와 같은 원리다 — 키에 카메라 위쪽 방향을 곱한 월드 지점을 화면으로 옮기고, 픽셀만큼 더 띄운다.
        /// </summary>
        public async UniTask PlayAsync(Character speaker, Sprite portrait, string text)
        {
            Begin();
            var camera = _cameraManager.Camera;
            var headOffset = camera.transform.up * (speaker.GetHeight() + BubbleHeadGap);
            _overlayBubble.Attach(camera, speaker.transform, headOffset, BubbleScreenOffset);
            await _overlayBubble.PlayAsync(portrait, text, LifeToken);
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
