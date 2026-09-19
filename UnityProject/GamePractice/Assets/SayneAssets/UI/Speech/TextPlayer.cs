using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 글을 한 글자씩 틀어 주는 재생기. 말풍선 모양의 몸을 쓰지만 무슨 상황인지는 모른다 —
    /// 누가 무슨 말을 왜 하는지, 누가 넘기는지 전부 밖의 사정이다.
    /// 지금 어디까지 틀었는지는 State 로 흘려보내니, 겉껍데기는 그걸 보고 자기 모습을 정하면 된다.
    /// 글이 길면 몸이 아래위로 늘어나고, 프리팹에 잡아 둔 크기보다 작아지지는 않는다.
    /// </summary>
    public class TextPlayer : MonoBehaviour
    {
        /// <summary>한 글자씩 찍는 빠르기. 40 으로 두니 한 줄이 0.4초 만에 다 찍혀 한 번에 뜬 것처럼 보였다.</summary>
        private const float CharsPerSecond = 16f;
        private const float PopInSeconds = 0.2f;
        private const float PopOutSeconds = 0.12f;
        private const float BackOvershoot = 1.70158f;

        /// <summary>폭을 글에 맞출 때의 하한. 이보다 좁으면 꼬리와 넘김 표시가 몸 밖으로 나간다.</summary>
        private const float MinFittedWidth = 220f;

        private const float NextMarkBobDistance = 8f;
        private const float NextMarkBobSpeed = 5f;

        [SerializeField] private RectTransform _body;
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private RectTransform _nextMark;

        /// <summary>켜면 짧은 글일 때 폭도 글에 맞춰 줄어든다. 프리팹에 잡아 둔 폭이 상한이다.</summary>
        [SerializeField] private bool _isWidthFitted;

        private readonly ReactiveProperty<TextPlayState> _state = new ReactiveProperty<TextPlayState>(TextPlayState.Idle);
        private readonly Subject<Unit> _advanced = new Subject<Unit>();

        private Vector2 _baseSize;

        /// <summary>지금 어디까지 틀었나. 껍데기가 이걸 보고 입력 차단·투명도 같은 걸 스스로 정한다.</summary>
        public ReadOnlyReactiveProperty<TextPlayState> State => _state;

        /// <summary>틀고 있는 중인가. State 의 파생값이라 따로 들지 않는다.</summary>
        public Observable<bool> IsPlaying => _state.Select(state => state != TextPlayState.Idle);

        /// <summary>넘기라는 신호가 들어왔다.</summary>
        public Observable<Unit> Advanced => _advanced;

        private void Awake()
        {
            _baseSize = _body.sizeDelta;
        }

        /// <summary>글이 찍히는 중이면 한 번에 다 보여 주고, 다 찍힌 뒤면 닫는다.</summary>
        public void Advance()
        {
            _advanced.OnNext(Unit.Default);
        }

        /// <param name="showsNextMark">다 찍힌 뒤 넘김 표시를 띄울지. 누르지 않아도 밖에서 알아서 넘기는 대사면 끈다.</param>
        public async UniTask PlayAsync(string text, CancellationToken token, bool showsNextMark = true)
        {
            _text.text = text;
            _text.maxVisibleCharacters = 0;
            _nextMark.gameObject.SetActive(false);

            Fit(text);
            _text.ForceMeshUpdate();
            var total = _text.textInfo.characterCount;

            _state.Value = TextPlayState.Opening;
            await UnscaledTween.RunAsync(PopInSeconds, t => _body.localScale = Vector3.one * EaseOutBack(t), token);

            _state.Value = TextPlayState.Typing;
            await TypeAsync(total, token);

            _state.Value = TextPlayState.Waiting;
            await WaitAdvanceAsync(showsNextMark, token);

            _state.Value = TextPlayState.Closing;
            await UnscaledTween.RunAsync(PopOutSeconds, t => _body.localScale = Vector3.one * (1f - t * t), token);

            _state.Value = TextPlayState.Idle;
        }

        /// <summary>글자를 차례로 드러낸다. 도중에 넘기면 남은 글자를 한 번에 보여 준다.</summary>
        private async UniTask TypeAsync(int total, CancellationToken token)
        {
            var isSkipped = false;
            using var skip = _advanced.Subscribe(_ => isSkipped = true);

            var typed = 0f;
            while (typed < total && !isSkipped)
            {
                typed += CharsPerSecond * Time.unscaledDeltaTime;
                _text.maxVisibleCharacters = Mathf.Min(total, (int)typed);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            _text.maxVisibleCharacters = total;
        }

        /// <summary>넘김 표시를 위아래로 흔들며 신호를 기다린다. 표시를 끄면 조용히 기다리기만 한다.</summary>
        private async UniTask WaitAdvanceAsync(bool showsNextMark, CancellationToken token)
        {
            _nextMark.gameObject.SetActive(showsNextMark);

            var rest = _nextMark.anchoredPosition;
            var isAdvanced = false;
            using var advance = _advanced.Subscribe(_ => isAdvanced = true);

            var waited = 0f;
            while (!isAdvanced)
            {
                waited += Time.unscaledDeltaTime;
                var drop = Mathf.Abs(Mathf.Sin(waited * NextMarkBobSpeed)) * NextMarkBobDistance;
                _nextMark.anchoredPosition = rest + Vector2.down * drop;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            _nextMark.anchoredPosition = rest;
        }

        private void Fit(string text)
        {
            // 글 상자는 몸 안에 여백을 두고 꽉 차게 걸려 있다 — 그 여백이 곧 sizeDelta 의 음수다.
            var padding = -_text.rectTransform.sizeDelta;
            var preferred = _text.GetPreferredValues(text, _baseSize.x - padding.x, 0f);

            var width = _isWidthFitted
                ? Mathf.Clamp(Mathf.Ceil(preferred.x) + padding.x, MinFittedWidth, _baseSize.x)
                : _baseSize.x;
            var height = Mathf.Max(_baseSize.y, Mathf.Ceil(preferred.y) + padding.y);

            _body.sizeDelta = new Vector2(width, height);
        }

        private static float EaseOutBack(float t)
        {
            var p = t - 1f;
            return 1f + (BackOvershoot + 1f) * p * p * p + BackOvershoot * p * p;
        }

        private void OnDestroy()
        {
            _state.Dispose();
            _advanced.Dispose();
        }
    }
}
