using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>누를 수 있는 원형 스킬 버튼. 클릭 스트림을 노출한다.</summary>
    public class SkillButtonWidget : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _label;

        public Observable<Unit> Clicked => _button.onClick.AsObservable();

        public void SetText(string text)
        {
            _label.text = text;
        }
    }
}
