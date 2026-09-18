using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 로딩 화면. 게이지를 채우다가 다 차면 "TAP TO START" 를 띄우고 탭을 기다린다.
    /// 만들고 치우는 건 LoadingScreenManager 가 한다 — 여기는 받은 진행도를 그리고 연출만 돌린다.
    /// 연출은 unscaled 시간으로 돈다.
    /// </summary>
    public class LoadingPanel : MonoBehaviour
    {
        /// <summary>게이지가 1초에 차오르는 최대 비율. 로드가 순식간에 끝나도 게이지가 한 번에 튀지 않고 흐른다.</summary>
        private const float FillSpeed = 1.1f;

        /// <summary>문구 하나가 머무는 시간, 말줄임 점이 하나 늘어나는 간격.</summary>
        private const float MessageSeconds = 1.6f;
        private const float DotSeconds = 0.35f;
        private const int MaxDots = 3;

        private const int EmberCount = 26;
        private const float FadeOutSeconds = 0.55f;

        /// <summary>불티가 바닥에서 떠오르는 높이. 기준 해상도(1080) 단위.</summary>
        private const float EmberRiseMin = 480f;
        private const float EmberRiseMax = 1000f;

        private static readonly string[] Messages =
        {
            "붉은 달을 띄우는 중",
            "성문을 여는 중",
            "고블린을 깨우는 중",
            "무기를 벼리는 중",
            "오우거에게 몽둥이를 쥐여 주는 중",
            "전장을 펼치는 중",
        };

        [Header("배경")]
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _emberRoot;
        [SerializeField] private Image _emberTemplate;

        [Header("제목")]
        [SerializeField] private CanvasGroup _titleGroup;
        [SerializeField] private RectTransform _titleDivider;

        [Header("게이지")]
        [SerializeField] private CanvasGroup _gaugeGroup;
        [SerializeField] private SlicedFillBar _gaugeFill;
        [SerializeField] private RectTransform _gaugeHead;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private TextMeshProUGUI _percentText;

        [Header("시작")]
        [SerializeField] private CanvasGroup _tapGroup;
        /// <summary>화면 전체. 손가락이 닿는 순간(PointerDown) 시작한다 — 떼기를 기다리지 않는다.</summary>
        [SerializeField] private EventTrigger _tapTrigger;
        [SerializeField] private TextMeshProUGUI _versionText;

        [SerializeField] private CanvasGroup _rootGroup;

        private float _targetProgress;
        private float _shownProgress;
        private int _messageIndex;
        private int _dotCount;

        /// <summary>로드가 얼마나 끝났는지. 0~1. 게이지는 여기까지 따라 차오르고, 값이 내려와도 뒤로 가지 않는다.</summary>
        public void SetProgress(float ratio)
        {
            _targetProgress = Mathf.Max(_targetProgress, ratio);
        }

        /// <summary>게이지가 끝까지 차기를 기다렸다가 시작 안내를 띄우고, 탭이 들어오면 끝난다.</summary>
        public async UniTask WaitStartAsync(CancellationToken token)
        {
            await UniTask.WaitUntil(() => _shownProgress >= 1f, cancellationToken: token);

            ShowTapToStart();

            var tapped = new UniTaskCompletionSource();
            var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDown.callback.AddListener(_ => tapped.TrySetResult());
            _tapTrigger.triggers.Add(pointerDown);

            await tapped.Task.AttachExternalCancellation(token);

            _tapTrigger.triggers.Remove(pointerDown);

            ((RectTransform)_tapGroup.transform).DOPunchScale(Vector3.one * 0.12f, 0.3f, 8, 0.6f)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        /// <summary>화면 전체가 옅어진다. 다 사라진 뒤의 정리는 부른 쪽이 한다.</summary>
        public Tween FadeOut()
        {
            // 천천히 다가오던 배경을 멈추고 그 자리에서 한 번 더 확 다가간다.
            _background.DOKill();

            return DOTween.Sequence()
                .Append(_rootGroup.DOFade(0f, FadeOutSeconds).SetEase(Ease.InQuad))
                .Join(_background.DOScale(_background.localScale * 1.08f, FadeOutSeconds).SetEase(Ease.InQuad))
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void Awake()
        {
            _versionText.text = $"v{Application.version}";
            _tapGroup.alpha = 0f;
            RenderMessage();

            SpawnEmbers();
            PlayIntro();
        }

        private void Update()
        {
            _shownProgress = Mathf.MoveTowards(_shownProgress, _targetProgress, FillSpeed * Time.unscaledDeltaTime);

            _gaugeFill.FillAmount = _shownProgress;
            _gaugeHead.anchorMin = _gaugeHead.anchorMax = new Vector2(_shownProgress, 0.5f);
            _percentText.text = $"{Mathf.FloorToInt(_shownProgress * 100f)}%";
        }

        /// <summary>검은 화면에서 배경이 떠오르고, 제목이 내려앉고, 게이지가 올라온다. 배경은 그 뒤로 천천히 다가온다.</summary>
        private void PlayIntro()
        {
            _rootGroup.alpha = 0f;
            _titleGroup.alpha = 0f;
            _gaugeGroup.alpha = 0f;
            _titleDivider.localScale = new Vector3(0f, 1f, 1f);

            DOTween.Sequence()
                .Insert(0f, _rootGroup.DOFade(1f, 0.6f))
                .Insert(0.3f, _titleGroup.DOFade(1f, 0.7f))
                .Insert(0.3f, ((RectTransform)_titleGroup.transform).DOAnchorPos(new Vector2(0f, 30f), 0.8f)
                    .From(true).SetEase(Ease.OutCubic))
                .Insert(0.7f, _titleDivider.DOScaleX(1f, 0.6f).SetEase(Ease.OutCubic))
                .Insert(0.8f, _gaugeGroup.DOFade(1f, 0.5f))
                .SetUpdate(true)
                .SetLink(gameObject);

            _background.DOScale(1.06f, 18f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(gameObject);

            DOTween.Sequence()
                .AppendInterval(MessageSeconds)
                .AppendCallback(NextMessage)
                .SetLoops(-1)
                .SetUpdate(true)
                .SetLink(gameObject);

            DOTween.Sequence()
                .AppendInterval(DotSeconds)
                .AppendCallback(NextDot)
                .SetLoops(-1)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void NextMessage()
        {
            _messageIndex = (_messageIndex + 1) % Messages.Length;
            _dotCount = 0;
            RenderMessage();
        }

        private void NextDot()
        {
            _dotCount = (_dotCount + 1) % (MaxDots + 1);
            RenderMessage();
        }

        private void RenderMessage()
        {
            _messageText.text = Messages[_messageIndex] + new string('.', _dotCount);
        }

        /// <summary>게이지 자리를 비우고, 그 자리에서 "TAP TO START" 가 숨을 쉰다. 이때부터 화면 어디든 누르면 시작이다.</summary>
        private void ShowTapToStart()
        {

            var tapRect = (RectTransform)_tapGroup.transform;

            DOTween.Sequence()
                .Append(_gaugeGroup.DOFade(0f, 0.35f))
                .Append(_tapGroup.DOFade(1f, 0.4f))
                .Join(tapRect.DOScale(1f, 0.5f).From(Vector3.one * 0.85f).SetEase(Ease.OutBack))
                .OnComplete(PulseTapToStart)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        /// <summary>끝없이 숨 쉰다. 시퀀스 안에는 무한 반복을 넣을 수 없어 따로 돈다.</summary>
        private void PulseTapToStart()
        {
            _tapGroup.DOFade(0.35f, 0.9f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        /// <summary>
        /// 아래에서 피어올라 흔들리며 사라지는 불티. 하나하나 제 속도·크기로 돌아 한 무리처럼 보이지 않는다.
        /// </summary>
        private void SpawnEmbers()
        {
            _emberTemplate.gameObject.SetActive(false);

            for (var i = 0; i < EmberCount; i++)
            {
                var ember = Instantiate(_emberTemplate, _emberRoot);
                ember.gameObject.SetActive(true);

                // 처음엔 제각각 늦게 출발시켜 화면에 한꺼번에 몰려 오르지 않게 한다.
                PlayEmber(ember, Random.Range(0f, 6f));
            }
        }

        /// <summary>가로 자리는 앵커로 잡는다 — 캔버스 크기가 아직 안 잡힌 첫 프레임에도 화면 폭 전체에 고루 뿌려진다.</summary>
        private void PlayEmber(Image ember, float delay)
        {
            var rect = (RectTransform)ember.transform;

            var x = Random.Range(0f, 1f);
            var rise = Random.Range(EmberRiseMin, EmberRiseMax);
            var drift = Random.Range(-160f, 160f);
            var seconds = Random.Range(4.5f, 9f);
            var peak = Random.Range(0.45f, 0.95f);

            rect.anchorMin = rect.anchorMax = new Vector2(x, 0f);
            rect.anchoredPosition = new Vector2(0f, -20f);
            rect.localScale = Vector3.one * Random.Range(0.35f, 1f);
            ember.color = new Color(ember.color.r, ember.color.g, ember.color.b, 0f);

            DOTween.Sequence()
                .AppendInterval(delay)
                .Append(rect.DOAnchorPosY(rise, seconds).SetEase(Ease.OutSine))
                .Join(rect.DOAnchorPosX(drift, seconds).SetEase(Ease.InOutSine))
                .Join(ember.DOFade(peak, seconds * 0.25f))
                .Insert(delay + seconds * 0.55f, ember.DOFade(0f, seconds * 0.45f))
                .OnComplete(() => PlayEmber(ember, Random.Range(0f, 1.5f)))
                .SetUpdate(true)
                .SetLink(gameObject);
        }
    }
}
