using TMPro;
using UnityEngine;

namespace Sayne
{
    /// <summary>아이콘 + 라벨 메뉴 버튼 자리. 우상단 메뉴와 하단 바에서 재사용한다.</summary>
    public class IconMenuWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;

        public void SetLabel(string text)
        {
            _label.text = text;
        }
    }
}
