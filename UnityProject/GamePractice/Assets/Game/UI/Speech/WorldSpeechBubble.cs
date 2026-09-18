using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 캐릭터 머리 위 월드에 뜨는 혼잣말 풍선. 게임을 멈추지 않고, 누르지 않아도 알아서 찍히고 사라진다.
    /// 밖에서는 만들고(Create) · 틀고(Play) · 부술(Destroy) 뿐이다 — 머리 위 자리, 카메라 쪽으로 서기,
    /// 한 글자씩 찍기, 머물렀다 사라지기는 전부 이 안에서 한다.
    /// 말하는 캐릭터의 자식으로 붙어 함께 움직이고, 그 캐릭터가 사라지면 같이 사라진다.
    /// </summary>
    public class WorldSpeechBubble : MonoBehaviour
    {
        /// <summary>머리 꼭대기에서 꼬리 끝까지의 월드 간격.</summary>
        private const float HeadGap = 0.25f;

        private const float CharsPerSecond = 14f;
        private const float PopInSeconds = 0.2f;
        private const float PopOutSeconds = 0.25f;

        /// <summary>다 찍힌 뒤 머무는 시간. 글이 길수록 조금 더 머문다.</summary>
        private const float HoldSeconds = 1.2f;
        private const float HoldSecondsPerChar = 0.05f;

        /// <summary>폭을 글에 맞출 때의 하한. 이보다 좁으면 꼬리가 풍선 밖으로 나간다.</summary>
        private const float MinWidth = 160f;

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _anchor;
        [SerializeField] private RectTransform _body;
        [SerializeField] private TextMeshProUGUI _text;

        /// <summary>프리팹에 잡아 둔 풍선 크기. 폭은 상한, 높이는 하한이다.</summary>
        private Vector2 _baseSize;

        private Sequence _playing;

        /// <summary>말하는 캐릭터 머리 위에 풍선을 하나 붙인다. 틀기 전까지는 보이지 않는다.</summary>
        public static WorldSpeechBubble Create(WorldSpeechBubble prefab, Character speaker)
        {
            var bubble = Instantiate(prefab, speaker.transform, false);
            bubble.PlaceAbove(speaker.GetHeight());
            return bubble;
        }

        private void Awake()
        {
            _baseSize = _body.sizeDelta;
            _group.alpha = 0f;
        }

        /// <summary>대사를 튼다. 이미 틀고 있던 게 있으면 끊고 새로 시작한다.</summary>
        public void Play(string text)
        {
            _playing?.Kill();

            _text.text = text;
            Fit(text);
            _text.ForceMeshUpdate();

            var total = _text.textInfo.characterCount;
            _text.maxVisibleCharacters = 0;

            _group.alpha = 1f;
            _body.localScale = Vector3.one * 0.6f;

            _playing = DOTween.Sequence()
                .Append(_body.DOScale(1f, PopInSeconds).SetEase(Ease.OutBack))
                .Append(DOTween.To(() => _text.maxVisibleCharacters, count => _text.maxVisibleCharacters = count,
                    total, total / CharsPerSecond).SetEase(Ease.Linear))
                .AppendInterval(HoldSeconds + total * HoldSecondsPerChar)
                .Append(_group.DOFade(0f, PopOutSeconds))
                .SetLink(gameObject);
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        /// <summary>
        /// 뿌리는 발에 두고, 풍선 자리만 머리 위로 올린다. 뿌리가 카메라 쪽으로 서므로 "위" 는 곧 화면 위다.
        /// 월드 간격을 캔버스 단위로 옮기려고 뿌리의 배율로 나눈다.
        /// </summary>
        private void PlaceAbove(float height)
        {
            transform.localPosition = Vector3.zero;
            _anchor.anchoredPosition = new Vector2(0f, (height + HeadGap) / transform.lossyScale.y);
        }

        /// <summary>짧은 글이면 폭이 줄고, 긴 글이면 줄이 바뀌며 위로 자란다.</summary>
        private void Fit(string text)
        {
            // 글 상자는 풍선 안에 여백을 두고 꽉 차게 걸려 있다 — 그 여백이 곧 sizeDelta 의 음수다.
            var padding = -_text.rectTransform.sizeDelta;
            var preferred = _text.GetPreferredValues(text, _baseSize.x - padding.x, 0f);

            var width = Mathf.Clamp(Mathf.Ceil(preferred.x) + padding.x, MinWidth, _baseSize.x);
            var height = Mathf.Max(_baseSize.y, Mathf.Ceil(preferred.y) + padding.y);

            _body.sizeDelta = new Vector2(width, height);
        }
    }
}
