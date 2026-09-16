using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>좌상단 히어로 상태: 초상화, 이름, 레벨, HP바, EXP바.</summary>
    public class HeroStatusWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private Image _hpFill;
        [SerializeField] private TextMeshProUGUI _hpLabel;
        [SerializeField] private Image _expFill;

        public void SetName(string heroName)
        {
            _nameText.text = heroName;
        }

        public void SetLevel(int level)
        {
            _levelText.text = $"Lv.{level}";
        }

        public void SetHP(int current, int max)
        {
            var ratio = max > 0 ? (float)current / max : 0f;
            _hpFill.fillAmount = ratio;
            _hpLabel.text = $"{current}({Mathf.RoundToInt(ratio * 100f)}%)";
        }

        public void SetEXPRatio(float ratio)
        {
            _expFill.fillAmount = Mathf.Clamp01(ratio);
        }
    }
}
