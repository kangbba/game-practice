using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 월드에 떠서 캐릭터 머리 위를 따라다니는 혼잣말 풍선. 게임을 멈추지 않고, 누르지 않아도 알아서 찍히고 사라진다.
    /// WorldHPBar 와 같은 원리다 — 월드 캔버스 하나에 모여 있고, 무엇을 따라다닐지만 받아 늘 카메라를 정면으로 본다.
    /// 캐릭터도 죽음도 모르고, 만들고 치우는 건 만든 쪽 일이다. 한 번 말하고 치워지는 일회용이다.
    /// 크기는 건드리지 않는다. 모양은 프리팹이 정하고, 월드에 얼마나 크게 띄울지(배율)는 만드는 쪽이 생성할 때 건다.
    /// </summary>
    public class WorldSpeechBubble : MonoBehaviour
    {
        private const float CharsPerSecond = 14f;
        private const float PopInSeconds = 0.2f;
        private const float PopOutSeconds = 0.25f;

        /// <summary>다 찍힌 뒤 머무는 시간. 글이 길수록 조금 더 머문다.</summary>
        private const float HoldSeconds = 1.2f;
        private const float HoldSecondsPerChar = 0.05f;

        /// <summary>폭을 글에 맞출 때의 하한. 이보다 좁으면 꼬리가 풍선 밖으로 나간다 — 프리팹 그림이 두 배라 하한도 두 배다.</summary>
        private const float MinWidth = 320f;

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _body;
        [SerializeField] private TextMeshProUGUI _text;

        /// <summary>말하는 이 얼굴 칸. 안 꽂은 프리팹은 얼굴 없이 글만 쓴다. 꽂았으면 얼굴을 받았을 때만 켠다.</summary>
        [SerializeField] private Image _portrait;

        private Transform _camera;
        private Transform _target;

        /// <summary>몸의 발에서 꼬리 끝까지의 월드 벡터.</summary>
        private Vector3 _worldOffset;

        /// <summary>프리팹에 잡아 둔 풍선 크기. 폭은 상한, 높이는 하한이다.</summary>
        private Vector2 _baseSize;

        /// <param name="worldOffset">몸의 발에서 꼬리 끝까지. 머리 위 어디에 설지는 만든 쪽이 정한다.</param>
        /// <param name="portrait">말하는 이 얼굴. null 이면 칸을 끈다. 칸 없는 프리팹이면 쓰지 않는다.</param>
        public void Init(Camera camera, Transform target, Vector3 worldOffset, Sprite portrait)
        {
            _baseSize = _body.sizeDelta;
            _group.alpha = 0f;
            ShowPortrait(portrait);

            _camera = camera.transform;
            _target = target;
            _worldOffset = worldOffset;

            // 첫 프레임부터 제자리에 서 있어야 한다. 루프를 기다리면 한 프레임 동안 원점에 찍힌다.
            Place();

            // 카메라와 같은 단계(PostLateUpdate)에서, 카메라보다 늦게 구독해 그 뒤에 돈다 —
            // 카메라 회전을 그대로 베끼기 때문에 순서가 뒤바뀌면 지난 프레임 각도로 선다.
            Observable.EveryUpdate(UnityFrameProvider.PostLateUpdate)
                .Subscribe(this, (_, self) => self.Place())
                .AddTo(this);
        }

        /// <summary>대사를 한 글자씩 찍고, 머물렀다가 사라질 때까지 기다린다. 끝나도 스스로 부서지지 않는다.</summary>
        public async UniTask PlayAsync(string text, CancellationToken token)
        {
            _text.text = text;
            Fit(text);
            _text.ForceMeshUpdate();

            var total = _text.textInfo.characterCount;
            _text.maxVisibleCharacters = 0;

            _group.alpha = 1f;
            _body.localScale = Vector3.one * 0.6f;

            var finished = new UniTaskCompletionSource();

            DOTween.Sequence()
                .Append(_body.DOScale(1f, PopInSeconds).SetEase(Ease.OutBack))
                .Append(DOTween.To(() => _text.maxVisibleCharacters, count => _text.maxVisibleCharacters = count,
                    total, total / CharsPerSecond).SetEase(Ease.Linear))
                .AppendInterval(HoldSeconds + total * HoldSecondsPerChar)
                .Append(_group.DOFade(0f, PopOutSeconds))
                .OnComplete(() => finished.TrySetResult())
                .SetLink(gameObject);

            await finished.Task.AttachExternalCancellation(token);
        }

        private void Place()
        {
            transform.SetPositionAndRotation(_target.position + _worldOffset, _camera.rotation);
        }

        /// <summary>칸이 있는 프리팹이면 얼굴을 넣고 켠다. 얼굴이 없으면 칸을 끄고 글을 그 자리까지 넓힌다 — 왼쪽 여백을 오른쪽과 같게.</summary>
        private void ShowPortrait(Sprite portrait)
        {
            if (_portrait == null) return;

            _portrait.sprite = portrait;
            _portrait.gameObject.SetActive(portrait != null);

            if (portrait == null)
            {
                var textRect = _text.rectTransform;
                textRect.offsetMin = new Vector2(-textRect.offsetMax.x, textRect.offsetMin.y);
            }
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
