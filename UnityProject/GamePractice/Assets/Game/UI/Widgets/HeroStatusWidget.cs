using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>좌상단 히어로 상태: 초상화, 이름, 레벨, HP바, EXP바.</summary>
    public class HeroStatusWidget : MonoBehaviour
    {
        [SerializeField] private Image _portrait;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private SlicedFillBar _hpFill;
        [SerializeField] private TextMeshProUGUI _hpLabel;
        [SerializeField] private SlicedFillBar _expFill;

        public void SetPortrait(Sprite portrait)
        {
            _portrait.sprite = portrait;
        }

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
            _hpFill.FillAmount = ratio;
            _hpLabel.text = $"{current}({Mathf.RoundToInt(ratio * 100f)}%)";
        }

        public void SetEXPRatio(float ratio)
        {
            _expFill.FillAmount = ratio;
        }
    }
}
