using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 성장 모달의 항목 한 줄. "공격력 Lv.3 / 기본 5 (+ 성장 4) / [120 G]" 를 그리고 강화 버튼을 흘린다.
    /// 계산하지 않는다 — 살 수 있는지조차 창이 받아서 넣어 준다.
    /// </summary>
    public class GrowthStatWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private Button _upgradeBtn;
        [SerializeField] private TextMeshProUGUI _costText;

        /// <summary>강화 버튼을 눌렀다. 살 수 있을 때만 눌린다.</summary>
        public Observable<Unit> Clicked => _upgradeBtn.onClick.AsObservable();

        public void SetLevel(string displayName, int level)
        {
            _nameText.text = $"{displayName}  <color=#F0C776>Lv.{level}</color>";
        }

        /// <summary>성장은 캐릭터 자체의 이야기라 장비 몫은 여기 없다 — 기본과 성장을 나눠 보여준다.</summary>
        public void SetValue(int baseValue, int growth)
        {
            _valueText.text = $"기본 {baseValue} <color=#78CC4D>(+ 성장 {growth})</color>";
        }

        /// <summary>값과 살 수 있는지를 한 번에 받는다. 못 사면 버튼이 죽고 값이 흐려진다.</summary>
        public void SetCost(long cost, bool affordable, bool maxed)
        {
            _upgradeBtn.interactable = !maxed && affordable;
            _costText.text = maxed ? "MAX" : $"{cost:N0} G";
            _costText.color = maxed || affordable
                ? new Color(0.94f, 0.78f, 0.46f)
                : new Color(0.75f, 0.4f, 0.4f);
        }
    }
}
