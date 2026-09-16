using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>원형 버튼 자리: 배속, 스킬, 하단 중앙 슬롯 등 텍스트 하나를 보여준다.</summary>
    public class CircleButtonWidget : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private TextMeshProUGUI _label;

        public void SetText(string text)
        {
            _label.text = text;
        }

        public void SetBackgroundColor(Color color)
        {
            _background.color = color;
        }
    }
}
