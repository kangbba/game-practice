using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Sayne
{
    /// <summary>맞은 자리에 떠오르는 데미지 숫자(스크린 스페이스). 값 해석은 부르는 쪽이 끝내고, 여기는 받은 문자열을 띄우고 사라지는 연출만 한다.</summary>
    public class DamageText : MonoBehaviour
    {
        private const float PopSeconds = 0.12f;
        private const float PopScale = 1.2f;
        private const float RiseSeconds = 0.8f;
        private const float RiseDistance = 100f;
        private const float FadeDelay = 0.3f;

        [SerializeField] private TextMeshProUGUI _label;

        public RectTransform RectTransform => (RectTransform)transform;

        public void Show(string text)
        {
            _label.text = text;

            var rect = RectTransform;
            rect.localScale = Vector3.zero;

            DOTween.Sequence()
                .Append(rect.DOScale(PopScale, PopSeconds).SetEase(Ease.OutBack))
                .Append(rect.DOScale(1f, PopSeconds))
                .Insert(0f, DOTween.To(() => rect.anchoredPosition, position => rect.anchoredPosition = position,
                        rect.anchoredPosition + Vector2.up * RiseDistance, RiseSeconds)
                    .SetEase(Ease.OutCubic))
                .Insert(FadeDelay, DOTween.To(() => _label.alpha, alpha => _label.alpha = alpha, 0f,
                    RiseSeconds - FadeDelay))
                .SetLink(gameObject)
                .OnComplete(() => Destroy(gameObject));
        }
    }
}
