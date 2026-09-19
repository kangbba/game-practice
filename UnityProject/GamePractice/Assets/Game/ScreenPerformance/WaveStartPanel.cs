using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 웨이브의 시작을 알리는 화면. 몇 스테이지 몇 웨이브인지 크게 한 번 보여준다.
    /// 만들고 치우는 건 ScreenPerformanceManager 가 한다 — 여기는 받은 표기를 연출과 함께 그리기만 한다.
    /// 연출은 unscaled 시간으로 돈다. 무엇이 게임을 멈춰도 화면은 끝까지 흐른다.
    /// </summary>
    public class WaveStartPanel : MonoBehaviour
    {
        // ---- 연출 시간표. 숫자를 여기서만 고치면 전체 호흡이 바뀐다. ----
        private const float DimSeconds = 0.2f;
        private const float BoardSeconds = 0.38f;
        private const float TitleAt = 0.16f;
        private const float DividerAt = 0.34f;
        private const float RowSeconds = 0.3f;
        private const float RowRise = 26f;

        [Header("바탕")]
        [SerializeField] private CanvasGroup _dimGroup;
        [SerializeField] private RectTransform _board;
        [SerializeField] private CanvasGroup _boardGroup;

        [Header("머리말")]
        [SerializeField] private TextMeshProUGUI _waveText;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private RectTransform _divider;

        /// <summary>
        /// 어느 웨이브가 시작되는지 보여준다. 만들어지자마자 연출이 돈다.
        /// 표기는 받아서 그대로 쓴다 — 보스웨이브를 뭐라 부를지는 스테이지 매니저가 정한다.
        /// </summary>
        public void Init(string waveLabel)
        {
            _waveText.text = waveLabel;
            _titleText.text = "전투 개시";

            Play();
        }

        /// <summary>바탕이 깔리고, 웨이브 이름이 자리를 잡고, "전투 개시" 가 크게 내려앉는다.</summary>
        private void Play()
        {
            _dimGroup.alpha = 0f;
            _boardGroup.alpha = 0f;
            _divider.localScale = new Vector3(0f, 1f, 1f);

            DOTween.Sequence()
                .Insert(0f, _dimGroup.DOFade(1f, DimSeconds))
                .Insert(0f, _boardGroup.DOFade(1f, BoardSeconds))
                .Insert(0f, _board.DOScale(1f, BoardSeconds).From(Vector3.one * 0.9f).SetEase(Ease.OutBack))
                .Insert(0.08f, Rise((RectTransform)_waveText.transform, RowSeconds))
                .Insert(TitleAt, ((RectTransform)_titleText.transform).DOScale(1f, 0.42f)
                    .From(Vector3.one * 1.55f).SetEase(Ease.OutBack))
                .Insert(DividerAt, _divider.DOScaleX(1f, 0.32f).SetEase(Ease.OutCubic))
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        /// <summary>제자리에서 살짝 아래로 내려놓았다가 올라오게 한다.</summary>
        private static Tween Rise(RectTransform rect, float seconds)
        {
            return rect.DOAnchorPos(new Vector2(0f, -RowRise), seconds).From(true).SetEase(Ease.OutCubic);
        }
    }
}
