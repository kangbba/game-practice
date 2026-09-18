using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 화면 아래에 뜨는 초상화 + 말풍선 한 벌. 그림과 글을 받아서 틀고, 화면을 누르면 넘어간다.
    /// 누구의 어떤 대사인지는 부르는 쪽이 정한다. 떠 있는 동안은 화면 전체가 입력을 먹어서 아래 UI 가 눌리지 않는다.
    /// 글을 찍는 일은 TextPlayer 가 하고, 여기는 초상화와 덮개를 맡는다.
    /// </summary>
    public class SpeechBubbleWidget : MonoBehaviour
    {
        private const float FadeSeconds = 0.15f;
        private const float PortraitStartScale = 0.8f;

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Button _tapBtn;
        [SerializeField] private RectTransform _portraitRoot;
        [SerializeField] private Image _portrait;
        [SerializeField] private TextPlayer _player;

        private void Awake()
        {
            _tapBtn.onClick.AddListener(_player.Advance);
            _group.alpha = 0f;

            // 입력은 틀고 있는 동안만 먹는다. 켜고 끄는 걸 손으로 짚지 않고 재생기 상태를 따라간다.
            _player.IsPlaying
                .Subscribe(this, (isPlaying, self) => self._group.blocksRaycasts = isPlaying)
                .AddTo(this);
        }

        public async UniTask PlayAsync(Sprite portrait, string text, CancellationToken token)
        {
            _portrait.sprite = portrait;

            var fadeIn = UnscaledTween.RunAsync(FadeSeconds, t =>
            {
                _group.alpha = t;
                _portraitRoot.localScale = Vector3.one * Mathf.Lerp(PortraitStartScale, 1f, t);
            }, token);

            await UniTask.WhenAll(fadeIn, _player.PlayAsync(text, token));

            await UnscaledTween.RunAsync(FadeSeconds, t => _group.alpha = 1f - t, token);
        }
    }
}
