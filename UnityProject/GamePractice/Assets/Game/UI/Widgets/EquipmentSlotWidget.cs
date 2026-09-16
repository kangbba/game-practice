using R3;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sayne
{
    /// <summary>장비창 좌측의 장착 슬롯. 부위가 같은 후보만 받아서 장비 ID를 흘린다.</summary>
    public class EquipmentSlotWidget : MonoBehaviour, IDropHandler
    {
        [SerializeField] private BodyPart _bodyPart;
        [SerializeField] private TextMeshProUGUI _slotName;
        [SerializeField] private TextMeshProUGUI _itemName;
        [SerializeField] private UnityEngine.UI.Image _icon;

        private readonly Subject<string> _dropped = new Subject<string>();

        public BodyPart BodyPart => _bodyPart;

        /// <summary>드랍된 후보의 장비 ID. 빈 문자열 = 벗기.</summary>
        public Observable<string> Dropped => _dropped;

        public void Setup(BodyPart bodyPart, string slotName)
        {
            _bodyPart = bodyPart;
            _slotName.text = slotName;
        }

        public void SetItem(string displayName, Sprite icon)
        {
            _itemName.text = displayName;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
        }

        /// <summary>착용 조건은 부위 일치뿐. 누구 것이었는지는 따지지 않는다.</summary>
        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag != null
                && eventData.pointerDrag.TryGetComponent<EquipmentCandidateWidget>(out var candidate)
                && candidate.BodyPart == _bodyPart)
            {
                _dropped.OnNext(candidate.EquipmentID);
            }
        }

        private void OnDestroy()
        {
            _dropped.Dispose();
        }
    }
}
