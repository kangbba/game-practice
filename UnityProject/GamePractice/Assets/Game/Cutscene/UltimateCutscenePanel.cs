using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 궁극기 컷인. 화면 전체를 덮는 투명 패널 위에 어둠·위아래 띠·사선 밴드·초상·기술 이름을 얹는다.
    /// 초상은 밴드(마스크) 안에 들어 있어서 밴드가 열리는 만큼만 보인다 — 눈높이 띠만 잘라 보여주는 전형적인 컷인이다.
    /// 게임이 멈춘 동안 도는 연출이라 트윈은 전부 unscaled 시간으로 돈다.
    /// 누구의 어떤 기술인지는 부르는 쪽이 정하고, 여기는 받은 걸 보여주고 끝났다고 알릴 뿐이다.
    ///
    /// 연출은 세 박자다. 밴드가 열리며 들어오고(In) — 다 열리는 순간 번쩍이며 흔들리고(임팩트) —
    /// 머무는 동안 초상과 이름이 엇갈려 흐르다(Hold) 밴드가 닫히며 나간다(Out).
    /// </summary>
    public class UltimateCutscenePanel : MonoBehaviour
    {
        private const float DimAlpha = 0.6f;
        private const float DimDeepAlpha = 0.72f;
        private const float InSeconds = 0.22f;
        private const float HoldSeconds = 0.75f;
        private const float OutSeconds = 0.15f;

        /// <summary>초상과 이름이 화면 밖에서 미끄러져 들어오는 거리.</summary>
        private const float SlideDistance = 1400f;

        /// <summary>자리를 잡은 뒤에도 멈추지 않고 천천히 흐르는 거리. 멈춰 있으면 정지 화면처럼 보인다.</summary>
        private const float DriftDistance = 50f;

        /// <summary>초상이 크게 들어왔다가 제자리로 줄어드는 배율. 밴드 마스크 안이라 넘친 만큼은 잘린다.</summary>
        private const float PortraitPunchScale = 1.16f;

        /// <summary>머무는 동안 초상이 아주 천천히 밀고 들어오는 배율. 카메라가 붙는 느낌을 낸다.</summary>
        private const float PortraitHoldScale = 1.06f;

        /// <summary>밴드가 기운 각도에서 더 젖혔다가 제자리로 돌아오는 양(도).</summary>
        private const float TiltOvershoot = 5f;

        /// <summary>기술명이 자간을 넓게 벌린 채 들어왔다가 조여든다.</summary>
        private const float NameSpacingSpread = 24f;

        private const float ImpactFlashAlpha = 0.5f;
        private const float ImpactFlashSeconds = 0.07f;
        private const float ImpactShakeSeconds = 0.32f;
        private const float ImpactShakeStrength = 26f;
        private const int ImpactShakeVibrato = 14;

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _dim;
        [SerializeField] private Image _flash;
        [SerializeField] private RectTransform _topBar;
        [SerializeField] private RectTransform _bottomBar;
        [SerializeField] private RectTransform _band;
        [SerializeField] private Image _bandFill;
        [SerializeField] private RectTransform _portraitRoot;
        [SerializeField] private Image _portrait;
        [SerializeField] private RectTransform _titleRoot;
        [SerializeField] private TextMeshProUGUI _heroName;
        [SerializeField] private TextMeshProUGUI _skillName;

        public UniTask PlayAsync(Sprite portrait, string heroName, string skillName, Color themeColor,
            CancellationToken token)
        {
            _portrait.sprite = portrait;
            _heroName.text = heroName;
            _skillName.text = skillName;
            _bandFill.color = themeColor;

            var portraitRest = _portraitRoot.anchoredPosition;
            var titleRest = _titleRoot.anchoredPosition;
            var portraitTransform = _portrait.rectTransform;
            var barHeight = _topBar.rect.height;
            var bandOpen = _band.sizeDelta;
            var bandClosed = new Vector2(bandOpen.x, 0f);
            var bandTilt = _band.localEulerAngles.z;
            var bandGlow = Color.Lerp(themeColor, Color.white, 0.35f);

            // 시작 자세: 전부 화면 밖·투명.
            _group.alpha = 1f;
            _dim.color = new Color(0f, 0f, 0f, 0f);
            _flash.color = new Color(1f, 1f, 1f, 0.7f);
            _topBar.anchoredPosition = new Vector2(0f, barHeight);
            _bottomBar.anchoredPosition = new Vector2(0f, -barHeight);
            // 스케일이 아니라 높이로 닫는다 — 스케일이면 마스크 안의 초상까지 같이 찌그러진다.
            _band.sizeDelta = bandClosed;
            _band.localRotation = Quaternion.Euler(0f, 0f, bandTilt + TiltOvershoot);
            _portraitRoot.anchoredPosition = portraitRest + Vector2.left * SlideDistance;
            _titleRoot.anchoredPosition = titleRest + Vector2.right * SlideDistance;
            portraitTransform.localScale = Vector3.one * PortraitPunchScale;
            _skillName.characterSpacing = NameSpacingSpread;
            _heroName.alpha = 0f;

            var done = new UniTaskCompletionSource();

            var sequence = DOTween.Sequence()
                // 들어온다
                .Append(_dim.DOFade(DimAlpha, InSeconds))
                .Join(_flash.DOFade(0f, InSeconds))
                .Join(_topBar.DOAnchorPosY(0f, InSeconds).SetEase(Ease.OutCubic))
                .Join(_bottomBar.DOAnchorPosY(0f, InSeconds).SetEase(Ease.OutCubic))
                .Join(_band.DOSizeDelta(bandOpen, InSeconds).SetEase(Ease.OutBack))
                .Join(_band.DOLocalRotate(new Vector3(0f, 0f, bandTilt), InSeconds).SetEase(Ease.OutBack))
                .Join(_portraitRoot.DOAnchorPos(portraitRest, InSeconds).SetEase(Ease.OutCubic))
                .Join(_titleRoot.DOAnchorPos(titleRest, InSeconds).SetEase(Ease.OutCubic))
                .Join(portraitTransform.DOScale(1f, InSeconds).SetEase(Ease.OutBack))
                // 머문다 — 서로 엇갈려 천천히 흐르고, 초상은 아주 느리게 밀고 들어온다
                .Append(_portraitRoot.DOAnchorPos(portraitRest + Vector2.right * DriftDistance, HoldSeconds)
                    .SetEase(Ease.Linear))
                .Join(_titleRoot.DOAnchorPos(titleRest + Vector2.left * DriftDistance, HoldSeconds)
                    .SetEase(Ease.Linear))
                .Join(portraitTransform.DOScale(PortraitHoldScale, HoldSeconds).SetEase(Ease.Linear))
                .Join(_dim.DOFade(DimDeepAlpha, HoldSeconds).SetEase(Ease.Linear))
                .Join(_bandFill.DOColor(bandGlow, HoldSeconds * 0.5f).SetLoops(2, LoopType.Yoyo))
                // 나간다 — 닫히는 순간 한 번 더 밝아지면서 사라진다
                .Append(_flash.DOFade(0.35f, OutSeconds * 0.4f))
                .Join(_group.DOFade(0f, OutSeconds))
                .Join(_band.DOSizeDelta(bandClosed, OutSeconds).SetEase(Ease.InCubic))
                .Join(_topBar.DOAnchorPosY(barHeight, OutSeconds).SetEase(Ease.InCubic))
                .Join(_bottomBar.DOAnchorPosY(-barHeight, OutSeconds).SetEase(Ease.InCubic));

            // 이름은 밴드가 열리는 도중에 뒤따라 붙는다 — 전부 같이 들어오면 한 덩어리로 보인다.
            sequence.Insert(InSeconds * 0.45f,
                DOTween.To(() => _heroName.alpha, value => _heroName.alpha = value, 1f, InSeconds));
            sequence.Insert(InSeconds * 0.45f,
                DOTween.To(() => _skillName.characterSpacing, value => _skillName.characterSpacing = value, 0f,
                    InSeconds * 1.8f).SetEase(Ease.OutCubic));

            // 임팩트: 밴드가 다 열리는 그 순간 번쩍이고 흔들린다.
            sequence.Insert(InSeconds, _flash.DOFade(ImpactFlashAlpha, ImpactFlashSeconds)
                .SetLoops(2, LoopType.Yoyo));
            sequence.Insert(InSeconds, _band.DOShakeAnchorPos(ImpactShakeSeconds, ImpactShakeStrength,
                ImpactShakeVibrato, 90f, false, true));

            sequence.SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() => done.TrySetResult());

            return done.Task.AttachExternalCancellation(token);
        }
    }
}
