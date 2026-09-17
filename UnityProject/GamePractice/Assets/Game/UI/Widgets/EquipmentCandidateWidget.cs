using R3;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 장비창 우측의 아이템 후보 한 칸. 누르면 선택되어 설명·장착 버튼이 이 장비를 향한다.
    /// 끌어서 같은 자리의 슬롯에 떨어뜨리면 바로 장착이다.
    /// </summary>
    public class EquipmentCandidateWidget : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private const float GhostAlpha = 0.85f;
        private const float DraggedAlpha = 0.35f;

        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private Image _icon;
        [SerializeField] private CanvasGroup _canvasGroup;

        /// <summary>장착 중 표식. 창이 장비 상태를 보고 켜고 끈다.</summary>
        [SerializeField] private GameObject _equippedMark;

        /// <summary>선택 테두리. 창이 선택 상태를 보고 켜고 끈다.</summary>
        [SerializeField] private GameObject _selectedMark;

        private readonly Subject<string> _clicked = new Subject<string>();

        /// <summary>끌려다니는 사본. 원본은 목록에 그대로 남아 있어서 목록이 출렁이지 않는다.</summary>
        private EquipmentCandidateWidget _ghost;

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
            _selectedMark.SetActive(false);
        }

        public void SetEquipped(bool isEquipped)
        {
            _equippedMark.SetActive(isEquipped);
        }

        public void SetSelected(bool isSelected)
        {
            _selectedMark.SetActive(isSelected);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;

            // 사본은 캔버스 맨 위에 올려 어디서든 보이게 하고, 레이캐스트를 꺼서 아래의 슬롯이 드랍을 받게 한다.
            _ghost = Instantiate(this, GetComponentInParent<Canvas>().transform);
            _ghost._canvasGroup.blocksRaycasts = false;
            _ghost._canvasGroup.alpha = GhostAlpha;
            _ghost.transform.position = eventData.position;

            _canvasGroup.alpha = DraggedAlpha;
        }

        public void OnDrag(PointerEventData eventData)
        {
            _ghost.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;

            Destroy(_ghost.gameObject);
            _ghost = null;
            _canvasGroup.alpha = 1f;
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
