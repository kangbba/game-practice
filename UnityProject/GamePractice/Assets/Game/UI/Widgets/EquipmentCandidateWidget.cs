using R3;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sayne
{
    /// <summary>
    /// 장비창 우측의 아이템 후보 한 칸. 누르면 선택되어 설명·장착 버튼이 이 장비를 향한다.
    /// 드래그해서 같은 자리의 슬롯에 떨어뜨리면 바로 장착이다.
    /// </summary>
    public class EquipmentCandidateWidget : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private UnityEngine.UI.Image _icon;
        [SerializeField] private CanvasGroup _canvasGroup;

        /// <summary>장착 중 표식. 창이 장비 상태를 보고 켜고 끈다.</summary>
        [SerializeField] private GameObject _equippedMark;

        private readonly Subject<string> _clicked = new Subject<string>();

        private Transform _homeParent;
        private int _homeSiblingIndex;
        private bool _isDragging;

        public string EquipmentID { get; private set; }

        /// <summary>이 파츠가 들어갈 자리. 착용 조건은 이것뿐이라 캐릭터는 가리지 않는다.</summary>
        public EquipmentSlot Slot { get; private set; }

        /// <summary>이 칸을 눌렀다. 장비 ID 를 흘린다.</summary>
        public Observable<string> Clicked => _clicked;

        public void Setup(string equipmentID, EquipmentSlot slot, string displayName, Sprite icon)
        {
            EquipmentID = equipmentID;
            Slot = slot;
            _label.text = displayName;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _equippedMark.SetActive(false);
        }

        public void SetEquipped(bool isEquipped)
        {
            _equippedMark.SetActive(isEquipped);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
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
            _isDragging = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 드래그가 끝난 손 떼기도 클릭으로 들어오므로 걸러낸다.
            if (_isDragging)
            {
                return;
            }

            _clicked.OnNext(EquipmentID);
        }

        private void OnDestroy()
        {
            _clicked.Dispose();
        }
    }
}
