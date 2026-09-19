using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 좌상단 퀘스트 박스: 몇 번째 퀘스트인지, 할 일, 보상, 진행도.
    /// 다 채우면 박스가 금빛으로 달아오르고 진행바 자리에 받기 버튼이 뜬다. 누르면 onClaim 을 부른다 — 받는 판단은 부르는 쪽 몫이다.
    /// 연출은 unscaled 시간으로 돈다.
    /// </summary>
    public class QuestWidget : MonoBehaviour
    {
        /// <summary>받을 때 튀어 오르는 동전 수. 프리팹을 구울 때 이만큼 만들어 둔다.</summary>
        public const int BurstCoinCount = 7;

        [SerializeField] private TextMeshProUGUI _tagText;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descText;
        [SerializeField] private TextMeshProUGUI _rewardText;
        [SerializeField] private SlicedFillBar _progressFill;
        [SerializeField] private TextMeshProUGUI _progressLabel;

        [Header("받을 수 있음")]
        [SerializeField] private GameObject _claimOverlay;
        [SerializeField] private Button _claimBtn;
        [SerializeField] private Image _claimGlow;
        [SerializeField] private RectTransform _claimPill;
        [SerializeField] private RectTransform _claimShine;
        [SerializeField] private TextMeshProUGUI _claimText;

        [Header("받는 순간")]
        [SerializeField] private RectTransform _body;
        [SerializeField] private CanvasGroup _content;
        [SerializeField] private RectTransform _rewardIcon;
        [SerializeField] private Image _flash;
        [SerializeField] private Image[] _coins;
        [SerializeField] private TextMeshProUGUI _gainText;

        private Sequence _claimableLoop;
        private Sequence _claimSequence;

        public void Init(Observable<QuestPlan> quest, Observable<int> index, Observable<int> progress,
            Observable<bool> isClaimable, Observable<QuestPlan> claimed, System.Action onClaim)
        {
            quest
                .CombineLatest(index, progress, (current, number, count) => (current, number, count))
                .Subscribe(this, (state, self) => self.Draw(state.current, state.number, state.count))
                .AddTo(this);

            isClaimable
                .Subscribe(this, (value, self) => self.SetClaimable(value))
                .AddTo(this);

            claimed
                .Subscribe(this, (current, self) => self.PlayClaim(current))
                .AddTo(this);

            _claimBtn.onClick.AsObservable()
                .Subscribe(onClaim, (_, action) => action())
                .AddTo(this);
        }

        /// <summary>구독 없이 최종 모습만 그린다. 프리팹을 구울 때 쓰는 문이다.</summary>
        public void Preview(QuestPlan quest, int index, int progress, bool isComplete)
        {
            Draw(quest, index, progress);

            _claimOverlay.SetActive(isComplete);
            _claimGlow.gameObject.SetActive(isComplete);
        }

        private void Draw(QuestPlan quest, int index, int progress)
        {
            if (quest == null)
            {
                DrawAllDone();
                return;
            }

            _tagText.text = $"퀘스트 {index + 1}";
            _titleText.text = quest.Title;
            _descText.text = quest.Description;
            _rewardText.text = $"{quest.GoldReward:N0}";

            // 막대는 목표를 넘겨 그리지 않는다. 이미 넘긴 채로 받은 퀘스트는 가득 찬 채로 뜬다.
            var current = Mathf.Min(progress, quest.Goal);

            _progressFill.FillAmount = (float)current / quest.Goal;
            _progressLabel.text = $"{current}/{quest.Goal}";
            _claimText.text = $"{current}/{quest.Goal}  보상 받기";
        }

        /// <summary>줄 게 없을 때. 목록을 다 끝낸 상태다.</summary>
        private void DrawAllDone()
        {
            _tagText.text = "퀘스트";
            _titleText.text = "모두 완료";
            _descText.text = string.Empty;
            _rewardText.text = "-";

            _progressFill.FillAmount = 1f;
            _progressLabel.text = "완료";
        }

        /// <summary>
        /// 받을 수 있게 되면 받기 버튼이 튀어나오고, 받을 때까지 후광이 숨 쉬고 버튼에 빛이 훑고 지나간다.
        /// </summary>
        private void SetClaimable(bool isClaimable)
        {
            _claimableLoop.Kill();
            _claimOverlay.SetActive(isClaimable);
            _claimGlow.gameObject.SetActive(isClaimable);

            if (!isClaimable)
            {
                _rewardIcon.localScale = Vector3.one;
                return;
            }

            var pillWidth = _claimPill.rect.width;

            _claimPill.DOScale(1f, 0.4f).From(Vector3.one * 0.6f).SetEase(Ease.OutBack)
                .SetUpdate(true)
                .SetLink(gameObject);

            var glow = _claimGlow.color;
            glow.a = 1f;
            _claimGlow.color = glow;
            _rewardIcon.localScale = Vector3.one;
            _claimShine.anchoredPosition = new Vector2(-40f, 0f);

            _claimableLoop = DOTween.Sequence()
                .Insert(0f, _claimGlow.DOFade(0.35f, 0.7f).SetEase(Ease.InOutSine))
                .Insert(0.7f, _claimGlow.DOFade(1f, 0.7f).SetEase(Ease.InOutSine))
                .Insert(0f, _rewardIcon.DOScale(1.14f, 0.7f).SetEase(Ease.InOutSine))
                .Insert(0.7f, _rewardIcon.DOScale(1f, 0.7f).SetEase(Ease.InOutSine))
                .Insert(0.2f, _claimShine.DOAnchorPosX(pillWidth + 40f, 0.55f).SetEase(Ease.InOutQuad))
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        /// <summary>
        /// 받은 순간. 박스가 번쩍이며 튕기고, 동전이 튀어 오르고, 받은 골드가 떠오른다.
        /// 그 사이 다음 퀘스트가 옆에서 밀려 들어온다 — 내용은 매니저가 이미 다음 장으로 바꿔 둔 뒤다.
        /// </summary>
        private void PlayClaim(QuestPlan quest)
        {
            _claimSequence.Kill(true);

            var content = (RectTransform)_content.transform;

            _body.localScale = Vector3.one;
            _content.alpha = 0f;
            content.anchoredPosition = new Vector2(36f, 0f);
            _gainText.text = $"+{quest.GoldReward:N0}";
            _gainText.gameObject.SetActive(true);

            var gain = _gainText.rectTransform;

            _claimSequence = DOTween.Sequence()
                .Insert(0f, _body.DOPunchScale(Vector3.one * 0.09f, 0.4f, 7, 0.5f))
                .Insert(0f, _flash.DOFade(0.85f, 0.05f))
                .Insert(0.05f, _flash.DOFade(0f, 0.4f).SetEase(Ease.OutQuad))
                .Insert(0.18f, _content.DOFade(1f, 0.3f))
                .Insert(0.18f, content.DOAnchorPos(Vector2.zero, 0.38f).SetEase(Ease.OutCubic))
                .Insert(0f, gain.DOAnchorPos(new Vector2(0f, 62f), 0.8f).From(new Vector2(0f, 10f)).SetEase(Ease.OutCubic))
                .Insert(0f, gain.DOScale(1f, 0.3f).From(Vector3.one * 1.6f).SetEase(Ease.OutBack))
                .Insert(0.55f, _gainText.DOFade(0f, 0.35f).From(1f))
                .OnComplete(() => _gainText.gameObject.SetActive(false))
                .SetUpdate(true)
                .SetLink(gameObject);

            for (var i = 0; i < _coins.Length; i++)
            {
                _claimSequence.Insert(0f, Burst(_coins[i], i));
            }
        }

        /// <summary>동전 하나가 위쪽 부채꼴로 튀어 올랐다가 떨어지며 사라진다.</summary>
        private Sequence Burst(Image coin, int order)
        {
            var rect = coin.rectTransform;
            var angle = Mathf.Lerp(-70f, 70f, (order + Random.Range(-0.3f, 0.3f)) / (_coins.Length - 1)) * Mathf.Deg2Rad;
            var distance = Random.Range(70f, 110f);
            var peak = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * distance;
            var land = peak + new Vector2(peak.x * 0.25f, -34f);

            coin.gameObject.SetActive(true);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one * 0.5f;
            rect.localRotation = Quaternion.identity;
            coin.color = Color.white;

            return DOTween.Sequence()
                .Insert(0f, rect.DOAnchorPos(peak, 0.32f).SetEase(Ease.OutCubic))
                .Insert(0f, rect.DOScale(1f, 0.2f).SetEase(Ease.OutBack))
                .Insert(0f, rect.DOLocalRotate(new Vector3(0f, 0f, Random.Range(-240f, 240f)), 0.62f, RotateMode.FastBeyond360))
                .Insert(0.32f, rect.DOAnchorPos(land, 0.3f).SetEase(Ease.InQuad))
                .Insert(0.4f, coin.DOFade(0f, 0.22f))
                .OnComplete(() => coin.gameObject.SetActive(false));
        }
    }
}
