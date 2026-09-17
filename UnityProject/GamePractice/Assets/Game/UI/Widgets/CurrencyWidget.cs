using R3;
using TMPro;
using UnityEngine;

namespace Sayne
{
    /// <summary>재화 표시 알약. 수치 하나를 구독해서 보여준다 — 그게 골드인지 젬인지는 모른다.</summary>
    public class CurrencyWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _amountText;

        public void Init(Observable<long> amount)
        {
            amount
                .Subscribe(this, (value, self) => self._amountText.text = value.ToString("N0"))
                .AddTo(this);
        }

        /// <summary>구독 없이 숫자만 넣는다. 프리팹을 구울 때 쓰는 문이다.</summary>
        public void Preview(long amount)
        {
            _amountText.text = amount.ToString("N0");
        }

        public void SetColor(Color color)
        {
            _amountText.color = color;
        }
    }
}
