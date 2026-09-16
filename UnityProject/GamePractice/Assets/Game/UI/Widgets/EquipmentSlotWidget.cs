using R3;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sayne
{
    /// <summary>장비창 좌측의 장착 슬롯. 후보를 드랍하면 무기 ID를 흘린다.</summary>
    public class EquipmentSlotWidget : MonoBehaviour, IDropHandler
    {
        [SerializeField] private TextMeshProUGUI _slotName;
        [SerializeField] private TextMeshProUGUI _itemName;
        [SerializeField] private UnityEngine.UI.Image _icon;

        private readonly Subject<string> _dropped = new Subject<string>();

        /// <summary>드랍된 후보의 무기 ID. 빈 문자열 = 맨손.</summary>
        public Observable<string> Dropped => _dropped;

        public void SetSlotName(string slotName)
        {
            _slotName.text = slotName;
        }

        public void SetItem(string displayName, Sprite icon)
        {
            _itemName.text = displayName;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag != null
                && eventData.pointerDrag.TryGetComponent<EquipmentCandidateWidget>(out var candidate))
            {
                _dropped.OnNext(candidate.WeaponID);
            }
        }

        private void OnDestroy()
        {
            _dropped.Dispose();
        }
    }
}
