using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 말풍선 한 개. 받은 글을 한 글자씩 찍고, 넘기라는 신호(Advance)를 받으면 닫힌다.
    /// 무슨 글인지, 누가 넘기는지는 모른다 — 틀어 달라는 대로 틀 뿐이다.
    /// 글이 길면 풍선이 아래위로 늘어나고, 프리팹에 잡아 둔 크기보다 작아지지는 않는다.
    /// </summary>
    public class SpeechBubble : MonoBehaviour
    {
        private const float CharsPerSecond = 40f;
        private const float PopInSeconds = 0.2f;
        private const float PopOutSeconds = 0.12f;
        private const float BackOvershoot = 1.70158f;

        /// <summary>폭을 글에 맞출 때의 하한. 이보다 좁으면 꼬리와 넘김 표시가 풍선 밖으로 나간다.</summary>
        private const float MinFittedWidth = 220f;

        private const float NextMarkBobDistance = 8f;
        private const float NextMarkBobSpeed = 5f;

        [SerializeField] private RectTransform _body;
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private RectTransform _nextMark;

        /// <summary>켜면 짧은 글일 때 폭도 글에 맞춰 줄어든다. 프리팹에 잡아 둔 폭이 상한이다.</summary>
        [SerializeField] private bool _isWidthFitted;

        private Vector2 _baseSize;
        private bool _isAdvanceRequested;

        private void Awake()
        {
            _baseSize = _body.sizeDelta;
        }

        /// <summary>글이 찍히는 중이면 한 번에 다 보여 주고, 다 찍힌 뒤면 닫는다.</summary>
        public void Advance()
        {
            _isAdvanceRequested = true;
        }

        public async UniTask PlayAsync(string text, CancellationToken token)
        {
            _text.text = text;
            _text.maxVisibleCharacters = 0;
            _nextMark.gameObject.SetActive(false);

            Fit(text);
            _text.ForceMeshUpdate();
            var total = _text.textInfo.characterCount;

            await UnscaledTween.RunAsync(PopInSeconds, t => _body.localScale = Vector3.one * EaseOutBack(t), token);

            _isAdvanceRequested = false;
            var typed = 0f;
            while (typed < total && !_isAdvanceRequested)
            {
                typed += CharsPerSecond * Time.unscaledDeltaTime;
                _text.maxVisibleCharacters = Mathf.Min(total, (int)typed);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            _text.maxVisibleCharacters = total;

            _isAdvanceRequested = false;
            _nextMark.gameObject.SetActive(true);
            var markRest = _nextMark.anchoredPosition;
            var waited = 0f;
            while (!_isAdvanceRequested)
            {
                waited += Time.unscaledDeltaTime;
                var drop = Mathf.Abs(Mathf.Sin(waited * NextMarkBobSpeed)) * NextMarkBobDistance;
                _nextMark.anchoredPosition = markRest + Vector2.down * drop;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            _nextMark.anchoredPosition = markRest;

            await UnscaledTween.RunAsync(PopOutSeconds, t => _body.localScale = Vector3.one * (1f - t * t), token);
        }

        private void Fit(string text)
        {
            // 글 상자는 풍선 안에 여백을 두고 꽉 차게 걸려 있다 — 그 여백이 곧 sizeDelta 의 음수다.
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
    }
}
