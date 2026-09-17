using R3;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sayne
{
    /// <summary>
    /// 장비창 좌측의 장착 슬롯. 자리가 같은 후보만 드랍으로 받고,
    /// 누르면 그 자리에 낀 장비가 선택된다.
    /// </summary>
    public class EquipmentSlotWidget : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        [SerializeField] private EquipmentSlot _slot;
        [SerializeField] private TextMeshProUGUI _slotName;
        [SerializeField] private TextMeshProUGUI _itemName;
        [SerializeField] private UnityEngine.UI.Image _icon;

        private readonly Subject<string> _dropped = new Subject<string>();
        private readonly Subject<EquipmentSlot> _clicked = new Subject<EquipmentSlot>();

        public EquipmentSlot Slot => _slot;

        /// <summary>드랍된 후보의 장비 ID.</summary>
        public Observable<string> Dropped => _dropped;

        /// <summary>슬롯을 눌렀다. 자리를 흘린다.</summary>
        public Observable<EquipmentSlot> Clicked => _clicked;

        public void Setup(EquipmentSlot slot, string slotName)
        {
            _slot = slot;
            _slotName.text = slotName;
        }

        public void SetItem(string displayName, Sprite icon)
        {
            _itemName.text = displayName;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
        }

        /// <summary>착용 조건은 자리 일치뿐. 누구 것이었는지는 따지지 않는다.</summary>
        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag != null
                && eventData.pointerDrag.TryGetComponent<EquipmentCandidateWidget>(out var candidate)
                && candidate.Slot == _slot)
            {
                _dropped.OnNext(candidate.EquipmentID);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _clicked.OnNext(_slot);
        }

        private void OnDestroy()
        {
            _dropped.Dispose();
            _clicked.Dispose();
        }
    }
}
