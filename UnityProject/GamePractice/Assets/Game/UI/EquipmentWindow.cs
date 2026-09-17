using System.Collections.Generic;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 디아블로식 장비창. 좌측 = 자리별 장착 슬롯 + 설명 칸, 우측 = 탭 + 후보 목록.
    /// 장착 표시의 진실의 원천은 캐릭터의 CharacterEquipment 이고, 이 창은 SetEquipped 로 따라 그릴 뿐이다.
    ///
    /// 상태는 둘뿐이다 — 무엇을 골랐나(_selected), 무엇을 입고 있나(_equipped).
    /// 설명 칸·장착 버튼·장착중 표식·슬롯 표시는 전부 이 둘에서 파생되어 리액티브로 따라간다.
    /// 후보·슬롯을 누르면 선택만 바뀌고, 장착 버튼을 누르는 순간 즉시 장착 요청이 나간다. 드랍도 즉시 장착이다.
    /// </summary>
    public class EquipmentWindow : MonoBehaviour
    {
        [SerializeField] private EquipmentSlotWidget[] _slots;
        [SerializeField] private Button[] _tabBtns;
        [SerializeField] private RectTransform _candidatesRoot;
        [SerializeField] private EquipmentCandidateWidget _candidatePrefab;
        [SerializeField] private Button _closeBtn;

        [Header("몸 스탯 줄")]
        [SerializeField] private TextMeshProUGUI _statAttackText;
        [SerializeField] private TextMeshProUGUI _statHPText;

        [Header("설명 칸")]
        [SerializeField] private Image _descriptionIcon;
        [SerializeField] private TextMeshProUGUI _descriptionName;
        [SerializeField] private TextMeshProUGUI _descriptionText;

        [Header("장착 버튼")]
        [SerializeField] private Button _actionBtn;
        [SerializeField] private TextMeshProUGUI _actionBtnLabel;

        /// <summary>후보의 설계값 사본. 자리·이름·그림·설명·스탯은 전부 장비 설계값 에셋에서 흘러들어온다.</summary>
        private readonly Dictionary<string, (EquipmentSlot slot, string name, Sprite icon, string description,
            CharacterStats stats)> _catalog
            = new Dictionary<string, (EquipmentSlot, string, Sprite, string, CharacterStats)>();

        /// <summary>몸 스탯 = 기본 + 성장 합. 여기선 그 분해가 중요하지 않아서 합쳐 받는다. 장비 몫은 영향치로만 보여준다.</summary>
        private CharacterStats _bodyStats;

        private readonly List<EquipmentCandidateWidget> _candidates = new List<EquipmentCandidateWidget>();

        /// <summary>무엇을 골랐나. 빈 문자열 = 아무것도 안 골랐다.</summary>
        private readonly ReactiveProperty<string> _selected = new ReactiveProperty<string>(string.Empty);

        /// <summary>무엇을 입고 있나. 진실의 원천(캐릭터)을 따라 적는 사본이다.</summary>
        private readonly Dictionary<EquipmentSlot, string> _equipped = new Dictionary<EquipmentSlot, string>();
        private readonly Subject<Unit> _equippedChanged = new Subject<Unit>();

        /// <summary>보고 있는 탭. -1 = 전체, 그 외 = EquipmentSlots.All 의 인덱스.</summary>
        private readonly ReactiveProperty<int> _tab = new ReactiveProperty<int>(-1);

        private readonly Subject<string> _equipRequested = new Subject<string>();
        private readonly Subject<EquipmentSlot> _unequipRequested = new Subject<EquipmentSlot>();

        /// <summary>이 장비를 입혀 달라. 즉시 적용된다.</summary>
        public Observable<string> EquipRequested => _equipRequested;

        /// <summary>이 자리를 벗겨 달라. 무기 자리면 정책(맨손 장착)은 매니저가 정한다.</summary>
        public Observable<EquipmentSlot> UnequipRequested => _unequipRequested;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            foreach (var slotWidget in _slots)
            {
                slotWidget.Dropped
                    .Subscribe(this, (equipmentID, self) => self._equipRequested.OnNext(equipmentID))
                    .AddTo(this);

                slotWidget.Clicked
                    .Subscribe(this, (slot, self) => self.SelectEquippedOf(slot))
                    .AddTo(this);
            }

            for (var i = 0; i < _tabBtns.Length; i++)
            {
                var tabIndex = i - 1; // 0번 버튼 = 전체(-1)
                _tabBtns[i].onClick.AsObservable()
                    .Subscribe((self: this, tabIndex), (_, state) => state.self._tab.Value = state.tabIndex)
                    .AddTo(this);
            }

            _closeBtn.onClick.AsObservable()
                .Subscribe(this, (_, self) => self.Hide())
                .AddTo(this);

            _actionBtn.onClick.AsObservable()
                .Subscribe(this, (_, self) => self.RequestAction())
                .AddTo(this);

            // 선택·장비 상태가 바뀌면 파생 UI(설명·버튼·표식·슬롯)가 따라간다.
            _selected
                .Subscribe(this, (_, self) => self.RenderSelection())
                .AddTo(this);

            _equippedChanged
                .Subscribe(this, (_, self) => self.RenderEquipped())
                .AddTo(this);

            _tab
                .Subscribe(this, (_, self) => self.RenderTab())
                .AddTo(this);
        }

        private void OnDestroy()
        {
            _selected.Dispose();
            _equippedChanged.Dispose();
            _tab.Dispose();
            _equipRequested.Dispose();
            _unequipRequested.Dispose();
        }

        public void Show()
        {
            _tab.Value = -1;
            SelectEquippedOf(EquipmentSlot.MainHand);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>장비 상태(진실의 원천)가 바뀌면 여기로 들어온다. 빈 ID = 벗은 자리.</summary>
        public void SetEquipped(EquipmentSlot slot, string equipmentID)
        {
            _equipped[slot] = equipmentID ?? string.Empty;
            _equippedChanged.OnNext(Unit.Default);
        }

        /// <summary>몸 스탯(기본+성장 합)이 바뀌면 여기로 들어온다. 스탯 줄이 다시 그려진다.</summary>
        public void SetBodyStats(CharacterStats bodyStats)
        {
            _bodyStats = bodyStats;
            RenderStats();
        }

        public void SetCandidates(
            IReadOnlyList<(string equipmentID, EquipmentSlot slot, string displayName, Sprite icon, string description,
                CharacterStats stats)> items)
        {
            _catalog.Clear();
            _candidates.Clear();

            for (var i = _candidatesRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_candidatesRoot.GetChild(i).gameObject);
            }

            foreach (var (equipmentID, slot, displayName, icon, description, stats) in items)
            {
                _catalog[equipmentID] = (slot, displayName, icon, description, stats);

                var candidate = Instantiate(_candidatePrefab, _candidatesRoot);
                candidate.Setup(equipmentID, slot, displayName, icon);
                candidate.Clicked
                    .Subscribe(this, (id, self) => self._selected.Value = id)
                    .AddTo(candidate);

                _candidates.Add(candidate);
            }

            RenderTab();
            RenderEquipped();
        }

        // ---- 파생 렌더링 ----

        /// <summary>선택이 바뀌었다 → 설명 칸과 장착 버튼, 그리고 스탯 줄의 영향치.</summary>
        private void RenderSelection()
        {
            RenderStats();

            var id = _selected.Value;

            if (!_catalog.TryGetValue(id, out var entry))
            {
                _descriptionName.text = "-";
                _descriptionText.text = string.Empty;
                _descriptionIcon.enabled = false;
                _actionBtn.gameObject.SetActive(false);
                return;
            }

            _descriptionName.text = entry.name;
            _descriptionText.text = entry.description;
            _descriptionIcon.sprite = entry.icon;
            _descriptionIcon.enabled = entry.icon != null;

            RenderActionButton();
        }

        /// <summary>
        /// 스탯 줄 = 몸 스탯(기본+성장 합)이 바탕이고, 미장착 후보를 포커스한 동안만
        /// 그 장비가 미칠 영향이 (+x) 로 붙는다. 장착중인 걸 고르면 영향치는 없다.
        /// </summary>
        private void RenderStats()
        {
            var delta = default(CharacterStats);
            var id = _selected.Value;

            if (_catalog.TryGetValue(id, out var entry) && !IsEquipped(id, entry.slot))
            {
                delta = entry.stats;
            }

            _statAttackText.text = StatLine("공격력", _bodyStats.AttackPower, delta.AttackPower);
            _statHPText.text = StatLine("체력", _bodyStats.MaxHP, delta.MaxHP);
        }

        private static string StatLine(string label, int value, int delta)
        {
            return delta != 0 ? $"{label} {value} <color=#78CC4D>(+{delta})</color>" : $"{label} {value}";
        }

        /// <summary>장비 상태가 바뀌었다 → 슬롯 표시·장착중 표식·장착 버튼, 그리고 스탯 줄의 영향치.</summary>
        private void RenderEquipped()
        {
            RenderStats();

            foreach (var slotWidget in _slots)
            {
                _equipped.TryGetValue(slotWidget.Slot, out var id);
                var (name, icon) = Lookup(id);
                slotWidget.SetItem(name, icon);
            }

            foreach (var candidate in _candidates)
            {
                candidate.SetEquipped(IsEquipped(candidate.EquipmentID, candidate.Slot));
            }

            RenderActionButton();
        }

        /// <summary>탭이 바뀌었다 → 후보 노출 필터와 탭 강조.</summary>
        private void RenderTab()
        {
            var tab = _tab.Value;

            foreach (var candidate in _candidates)
            {
                var visible = tab < 0 || candidate.Slot == EquipmentSlots.All[tab];
                candidate.gameObject.SetActive(visible);
            }

            for (var i = 0; i < _tabBtns.Length; i++)
            {
                var label = _tabBtns[i].GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.color = (i - 1 == tab) ? new Color(0.94f, 0.78f, 0.46f) : new Color(0.65f, 0.65f, 0.7f);
                }
            }
        }

        /// <summary>장착 버튼은 선택·장비 상태의 파생값이다: 미장착 → 장착, 장착중 → 해제(맨손은 해제 불가).</summary>
        private void RenderActionButton()
        {
            var id = _selected.Value;

            if (!_catalog.TryGetValue(id, out var entry))
            {
                _actionBtn.gameObject.SetActive(false);
                return;
            }

            _actionBtn.gameObject.SetActive(true);

            if (!IsEquipped(id, entry.slot))
            {
                _actionBtnLabel.text = "장착";
                _actionBtn.interactable = true;
                return;
            }

            // 맨손은 무기의 바닥 상태라 벗을 수 없다.
            if (id == EquipmentID.Weapon.BareHands)
            {
                _actionBtnLabel.text = "장착중";
                _actionBtn.interactable = false;
                return;
            }

            _actionBtnLabel.text = "장비 해제";
            _actionBtn.interactable = true;
        }

        // ---- 동작 ----

        private void RequestAction()
        {
            var id = _selected.Value;
            if (!_catalog.TryGetValue(id, out var entry))
            {
                return;
            }

            if (IsEquipped(id, entry.slot))
            {
                _unequipRequested.OnNext(entry.slot);
            }
            else
            {
                _equipRequested.OnNext(id);
            }
        }

        private void SelectEquippedOf(EquipmentSlot slot)
        {
            if (_equipped.TryGetValue(slot, out var id) && !string.IsNullOrEmpty(id))
            {
                _selected.Value = id;
            }
        }

        private bool IsEquipped(string equipmentID, EquipmentSlot slot)
        {
            return _equipped.TryGetValue(slot, out var worn) && worn == equipmentID;
        }

        private (string name, Sprite icon) Lookup(string equipmentID)
        {
            var key = equipmentID ?? string.Empty;
            return _catalog.TryGetValue(key, out var entry)
                ? (entry.name, entry.icon)
                : (string.IsNullOrEmpty(key) ? "없음" : key, null);
        }
    }
}
