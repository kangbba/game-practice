using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>튜토리얼 매니저가 찍어내는 말풍선 프리팹. 화면 아래 위젯과 머리 위 말풍선.</summary>
    public interface ITutorialAssets
    {
        SpeechBubbleWidget SpeechBubbleWidgetPrefab { get; }
        OverlaySpeechBubble OverlaySpeechBubblePrefab { get; }
    }

    /// <summary>
    /// 안내 대사를 트는 창구. 게임을 멈추고 읽히는 대사 두 가지(화면 아래 초상화 대사, 머리 위 말풍선)와
    /// 게임을 멈추지 않는 머리 위 혼잣말을 틀 수 있다. 머리 위 말풍선은 둘 다 같은 프리팹(OverlaySpeechBubble)이다.
    /// 머리 위 말풍선은 HP 바와 같은 자리(HeadAnchor)에 뜬다 — 떠 있는 동안 그 캐릭터가 말하는 중(IsSpeaking)이라고 알리고, HP 바가 이걸 보고 비켜 준다.
    /// 무엇을 언제 말할지는 부르는 쪽이 정한다 — 여기는 틀고, 멈추는 대사를 트는 동안 게임을 멈출 뿐이다.
    /// 멈추는 대사는 한 번에 하나만 튼다. 이어서 틀려면 앞의 것을 await 한 뒤에 부른다.
    /// </summary>
    public class TutorialManager : ManagerBase
    {
        private const string RootName = "TutorialRoot";

        /// <summary>전투 HUD(0)와 창들 위, 궁극기 컷인(100) 아래.</summary>
        private const int SortingOrder = 90;

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        private readonly ITutorialAssets _assets;

        private readonly ReactiveProperty<bool> _isPlaying = new ReactiveProperty<bool>(false);

        private Canvas _canvas;
        private SpeechBubbleWidget _widget;

        /// <summary>지금 말하고 있는 혼잣말. 한 캐릭터가 새로 말하면 앞의 풍선은 끊고 치운다.</summary>
        private readonly Dictionary<Character, CancellationTokenSource> _speaking = new Dictionary<Character, CancellationTokenSource>();

        /// <summary>캐릭터 머리 위에 떠 있는 말풍선 수. 0 이 되면 빠진다 — 멈추는 대사와 혼잣말이 한 몸 위에 겹칠 수 있어서 센다.</summary>
        private readonly Dictionary<Character, int> _bubbleCounts = new Dictionary<Character, int>();

        /// <summary>어느 캐릭터의 말풍선이 뜨거나 졌다.</summary>
        private readonly Subject<Character> _bubbleChanged = new Subject<Character>();

        public ReadOnlyReactiveProperty<bool> IsPlaying => _isPlaying;

        /// <summary>
        /// 이 캐릭터 머리 위에 말풍선이 떠 있는가. 구독하는 순간 지금 상태부터 흘린다.
        /// 머리 위 HP 바가 이걸 보고 말풍선이 떠 있는 동안 비켜 준다 — 둘은 같은 자리(HeadAnchor)를 쓴다.
        /// </summary>
        public Observable<bool> IsSpeaking(Character speaker)
        {
            return _bubbleChanged
                .Where(speaker, (changed, target) => changed == target)
                .Prepend(speaker)
                .Select(this, (target, self) => self._bubbleCounts.ContainsKey(target))
                .DistinctUntilChanged();
        }

        public TutorialManager(ITutorialAssets assets)
        {
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

            _widget = UnityEngine.Object.Instantiate(_assets.SpeechBubbleWidgetPrefab, _canvas.transform);

            Pause.While(_isPlaying).RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            UnityEngine.Object.Destroy(_canvas.gameObject);

            _canvas = null;
            _widget = null;
            _isPlaying.Dispose();
            _bubbleChanged.Dispose();
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
        /// 붙는 자리는 머리 위 HP 바와 같다(HeadAnchor). 떠 있는 동안 HP 바는 비켜 준다.
        /// </summary>
        public async UniTask PlayAsync(Character speaker, Sprite portrait, string text)
        {
            Begin();
            var bubble = SpawnBubble(speaker, portrait);

            using (MarkSpeaking(speaker))
            {
                await bubble.PlayAsync(text, LifeToken);
            }

            UnityEngine.Object.Destroy(bubble.gameObject);
            _isPlaying.Value = false;
        }

        /// <summary>
        /// 말하는 이의 머리 위에 혼잣말을 띄운다. 게임을 멈추지 않고, 누르지 않아도 알아서 사라진다.
        /// 풍선은 말할 때 만들어 끝나면 치운다. 도중에 말하는 이가 죽거나 치워지면 그 자리에서 같이 치운다.
        /// </summary>
        public async UniTask SayAsync(Character speaker, Sprite portrait, string text)
        {
            if (_speaking.Remove(speaker, out var previous))
            {
                previous.Cancel();
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(LifeToken, speaker.destroyCancellationToken);
            _speaking.Add(speaker, cts);
            using var died = speaker.Died.Subscribe(cts, (_, source) => source.Cancel());

            var bubble = SpawnBubble(speaker, portrait);
            using var speaking = MarkSpeaking(speaker);

            try
            {
                await bubble.SayAsync(text, cts.Token);
            }
            finally
            {
                if (_speaking.TryGetValue(speaker, out var current) && current == cts)
                {
                    _speaking.Remove(speaker);
                }

                // 매니저가 내려가며 캔버스째 부서졌으면 풍선은 이미 없다.
                if (bubble != null)
                {
                    UnityEngine.Object.Destroy(bubble.gameObject);
                }
            }
        }

        /// <summary>머리 위 말풍선을 HP 바와 같은 자리(HeadAnchor)에 세운다.</summary>
        private OverlaySpeechBubble SpawnBubble(Character speaker, Sprite portrait)
        {
            var camera = GameCamera.Camera;
            var bubble = UnityEngine.Object.Instantiate(_assets.OverlaySpeechBubblePrefab, _canvas.transform);
            bubble.Init(camera, speaker.transform, HeadAnchor.WorldOffset(camera, speaker.GetHeight()),
                HeadAnchor.ScreenOffset, portrait);
            return bubble;
        }

        /// <summary>이 캐릭터 머리 위에 말풍선이 떴다고 알린다. 돌려받은 걸 치우면 졌다고 알린다.</summary>
        private IDisposable MarkSpeaking(Character speaker)
        {
            _bubbleCounts[speaker] = _bubbleCounts.GetValueOrDefault(speaker) + 1;
            _bubbleChanged.OnNext(speaker);

            return Disposable.Create((self: this, speaker), state => state.self.UnmarkSpeaking(state.speaker));
        }

        private void UnmarkSpeaking(Character speaker)
        {
            var left = _bubbleCounts[speaker] - 1;
            if (left > 0)
            {
                _bubbleCounts[speaker] = left;
            }
            else
            {
                _bubbleCounts.Remove(speaker);
            }

            _bubbleChanged.OnNext(speaker);
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
