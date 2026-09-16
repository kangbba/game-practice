using TMPro;
using UnityEngine;

namespace Sayne
{
    /// <summary>재화 표시 알약. 골드·젬 등 수치 하나를 보여준다.</summary>
    public class CurrencyWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _amountText;

        public void SetAmount(long amount)
        {
            _amountText.text = amount.ToString("N0");
        }

        public void SetColor(Color color)
        {
            _amountText.color = color;
        }
    }
}
