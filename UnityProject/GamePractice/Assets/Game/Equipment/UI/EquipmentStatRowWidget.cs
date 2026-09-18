using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 장비창 스탯 한 줄. 평소엔 "이름 — 지금 값" 만 보이고,
    /// 고른 장비를 끼면 값이 달라질 때만 "지금 → 바뀔 값" 과 증감 칩이 초록(오름)·빨강(내림)으로 붙는다.
    /// </summary>
    public class EquipmentStatRowWidget : MonoBehaviour
    {
        private static readonly Color UpColor = new Color(0.36f, 0.86f, 0.38f);
        private static readonly Color DownColor = new Color(0.95f, 0.30f, 0.28f);

        /// <summary>이 줄이 맡은 스탯. 창은 줄 순서가 아니라 이걸 보고 값을 넣는다.</summary>
        [SerializeField] private StatType _statType;

        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private TextMeshProUGUI _value;

        /// <summary>증감 칩. 변화가 없으면 통째로 꺼진다.</summary>
        [SerializeField] private Image _deltaChip;
        [SerializeField] private TextMeshProUGUI _deltaText;

        public StatType StatType => _statType;

        /// <summary>맡을 스탯을 정한다. 이름은 스탯 종류에서 그대로 따온다.</summary>
        public void Setup(StatType statType)
        {
            _statType = statType;
            _label.text = StatTypes.DisplayName(statType);
        }

        /// <summary>current = 지금 값, after = 고른 장비를 꼈을 때의 값. 같으면 비교 표시는 없다.</summary>
        public void Set(float current, float after)
        {
            var delta = after - current;

            if (Mathf.Approximately(delta, 0f))
            {
                _value.text = Format(current);
                _deltaChip.gameObject.SetActive(false);
                return;
            }

            var color = delta > 0f ? UpColor : DownColor;
            var hex = ColorUtility.ToHtmlStringRGB(color);

            _value.text = $"{Format(current)} > <color=#{hex}>{Format(after)}</color>";
            _deltaChip.color = color;
            _deltaText.text = delta > 0f ? $"+{Format(delta)}" : $"-{Format(-delta)}";
            _deltaChip.gameObject.SetActive(true);
        }

        private static string Format(float value)
        {
            return value.ToString("0.#");
        }
    }
}
