using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>누를 수 있는 원형 스킬 버튼. 쿨타임은 남은 초와 비율만 받아서 그린다.</summary>
    public class SkillButtonWidget : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private Image _cooldownFill;
        [SerializeField] private TextMeshProUGUI _cooldownText;

        public Observable<Unit> Clicked => _button.onClick.AsObservable();

        public void SetText(string text)
        {
            _label.text = text;
        }

        /// <summary>남은 비율 0~1. 0 이면 쿨타임이 안 보인다.</summary>
        public void SetCooldownRatio(float ratio)
        {
            _cooldownFill.fillAmount = ratio;
        }

        /// <summary>남은 초. 0 이면 숫자를 감춘다.</summary>
        public void SetCooldownRemain(float remainSeconds)
        {
            _cooldownText.text = remainSeconds > 0f ? $"{remainSeconds:0.0}" : string.Empty;
        }
    }
}
