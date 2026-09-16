using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 디아블로식 장비창. 좌측 = 부위별 장착 슬롯, 우측 = 후보 목록.
    /// 후보는 부위만 맞으면 어느 슬롯에든 들어간다 — 누구 것이었는지는 따지지 않는다.
    /// 장착 표시의 진실의 원천은 캐릭터의 CharacterEquipment 이고, 이 창은 SetEquipped 로 따라 그릴 뿐이다.
    /// 드랍은 대기 상태만 만들고, 적용 버튼을 눌러야 장착 요청(Applied)이 나간다.
    /// </summary>
    public class EquipmentWindow : MonoBehaviour
    {
        [SerializeField] private EquipmentSlotWidget[] _slots;
        [SerializeField] private RectTransform _candidatesRoot;
        [SerializeField] private EquipmentCandidateWidget _candidatePrefab;
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _closeButton;

        private readonly Dictionary<string, (string name, Sprite icon)> _catalog
            = new Dictionary<string, (string name, Sprite icon)>();

        private readonly Dictionary<BodyPart, string> _equipped = new Dictionary<BodyPart, string>();
        private readonly Dictionary<BodyPart, string> _pending = new Dictionary<BodyPart, string>();

        private readonly Subject<(BodyPart BodyPart, string EquipmentID)> _applied
            = new Subject<(BodyPart, string)>();

        /// <summary>적용이 확정된 (부위, 장비 ID). 빈 ID = 그 부위를 벗는다.</summary>
        public Observable<(BodyPart BodyPart, string EquipmentID)> Applied => _applied;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            foreach (var slot in _slots)
            {
                slot.Dropped
                    .Subscribe((self: this, slot), (equipmentID, state) => state.self.SetPending(state.slot.BodyPart, equipmentID))
                    .AddTo(this);
            }

            _applyButton.onClick.AsObservable()
                .Subscribe(this, (_, self) => self.Apply())
                .AddTo(this);

            _closeButton.onClick.AsObservable()
                .Subscribe(this, (_, self) => self.Hide())
                .AddTo(this);
        }

        private void OnDestroy()
        {
            _applied.Dispose();
        }

        public void Show()
        {
            _pending.Clear();
            RefreshAll();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>장비 상태(진실의 원천)가 바뀌면 여기로 들어온다. 빈 ID = 벗은 부위.</summary>
        public void SetEquipped(BodyPart bodyPart, string equipmentID)
        {
            _equipped[bodyPart] = equipmentID ?? string.Empty;
            _pending.Remove(bodyPart);
            RefreshAll();
        }

        public void SetCandidates(IReadOnlyList<(string equipmentID, BodyPart bodyPart, string displayName, Sprite icon)> items)
        {
            _catalog.Clear();

            for (var i = _candidatesRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_candidatesRoot.GetChild(i).gameObject);
            }

            foreach (var (equipmentID, bodyPart, displayName, icon) in items)
            {
                _catalog[equipmentID] = (displayName, icon);
                Instantiate(_candidatePrefab, _candidatesRoot).Setup(equipmentID, bodyPart, displayName, icon);
            }
        }

        private void SetPending(BodyPart bodyPart, string equipmentID)
        {
            _pending[bodyPart] = equipmentID;
            RefreshAll();
        }

        private void Apply()
        {
            // 요청이 나가면 장비 상태가 되돌아와 _pending 을 건드리므로, 먼저 비우고 들고 있던 것만 흘린다.
            var requests = new List<(BodyPart, string)>();
            foreach (var pending in _pending)
            {
                requests.Add((pending.Key, pending.Value));
            }

            _pending.Clear();

            foreach (var (bodyPart, equipmentID) in requests)
            {
                _applied.OnNext((bodyPart, equipmentID));
            }
        }

        private void RefreshAll()
        {
            foreach (var widget in _slots)
            {
                var isPending = _pending.TryGetValue(widget.BodyPart, out var id);
                if (!isPending)
                {
                    _equipped.TryGetValue(widget.BodyPart, out id);
                }

                var (name, icon) = Lookup(id);
                widget.SetItem(isPending ? $"{name} (대기)" : name, icon);
            }
        }

        private (string name, Sprite icon) Lookup(string equipmentID)
        {
            var key = equipmentID ?? string.Empty;
            return _catalog.TryGetValue(key, out var entry)
                ? entry
                : (string.IsNullOrEmpty(key) ? "없음" : key, null);
        }
    }
}
