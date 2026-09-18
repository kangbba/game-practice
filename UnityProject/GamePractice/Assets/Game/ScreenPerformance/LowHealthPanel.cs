using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 피가 얼마 안 남았을 때 화면 가장자리가 붉어지며 울렁이는 막.
    /// 켜지면 스스로 숨을 쉬고, 꺼지면 멈춘다 — 언제 켤지는 ScreenPerformanceManager 가 정한다.
    /// 연출은 unscaled 시간으로 돈다. 창을 열어 게임이 멈춰도 위급한 건 그대로 위급해 보인다.
    /// </summary>
    public class LowHealthPanel : MonoBehaviour
    {
        /// <summary>한 번 울렁이는 데 걸리는 시간. 심장 박동보다 조금 느리게 둔다.</summary>
        private const float PulseSeconds = 0.85f;

        /// <summary>가장 옅을 때와 가장 짙을 때. 시야를 가리지 않을 만큼만 짙어진다.</summary>
        private const float MinAlpha = 0.22f;
        private const float MaxAlpha = 0.55f;

        /// <summary>붉은 막이 안쪽으로 밀려들었다 물러나는 정도.</summary>
        private const float PulseScale = 1.06f;

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _edge;

        private Sequence _pulse;

        /// <summary>켜고 끄는 문. 켜질 때마다 연출을 처음부터 다시 튼다.</summary>
        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);

            _pulse?.Kill();
            _pulse = null;

            if (isVisible)
            {
                _pulse = Pulse();
            }
        }

        /// <summary>두 트윈이 같이 돈다 — 투명도가 오르내리고, 막이 안팎으로 숨을 쉰다.</summary>
        private Sequence Pulse()
        {
            _group.alpha = MinAlpha;
            _edge.localScale = Vector3.one;

            return DOTween.Sequence()
                .Join(_group.DOFade(MaxAlpha, PulseSeconds).SetEase(Ease.InOutSine))
                .Join(_edge.DOScale(PulseScale, PulseSeconds).SetEase(Ease.InOutSine))
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(gameObject);
        }
    }
}
