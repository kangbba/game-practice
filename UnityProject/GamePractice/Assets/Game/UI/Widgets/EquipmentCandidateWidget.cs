using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sayne
{
    /// <summary>장비창 우측의 아이템 후보 한 칸. 드래그해서 같은 부위의 슬롯에 떨어뜨린다.</summary>
    public class EquipmentCandidateWidget : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private UnityEngine.UI.Image _icon;
        [SerializeField] private CanvasGroup _canvasGroup;

        private Transform _homeParent;
        private int _homeSiblingIndex;

        /// <summary>빈 문자열 = 그 부위를 벗는다.</summary>
        public string EquipmentID { get; private set; }

        /// <summary>이 파츠가 들어갈 부위. 착용 조건은 이것뿐이라 캐릭터는 가리지 않는다.</summary>
        public BodyPart BodyPart { get; private set; }

        public void Setup(string equipmentID, BodyPart bodyPart, string displayName, Sprite icon)
        {
            EquipmentID = equipmentID;
            BodyPart = bodyPart;
            _label.text = displayName;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _homeParent = transform.parent;
            _homeSiblingIndex = transform.GetSiblingIndex();

            // 드래그 중엔 최상위로 올려서 다른 UI 위에 그려지고, 레이캐스트를 꺼서 슬롯이 드랍을 받게 한다.
            transform.SetParent(GetComponentInParent<Canvas>().transform, true);
            _canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            transform.SetParent(_homeParent, false);
            transform.SetSiblingIndex(_homeSiblingIndex);
            _canvasGroup.blocksRaycasts = true;
        }
    }
}
