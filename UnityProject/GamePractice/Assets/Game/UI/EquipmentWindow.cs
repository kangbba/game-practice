using System.Collections.Generic;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 좌우 반반 장비창. 좌측 = 캐릭터 프리뷰 + 스탯 줄 + 자리별 장착 슬롯, 우측 = 설명 칸 + 탭 + 칸 수가 정해진 가방.
    /// 장착 표시의 진실의 원천은 캐릭터의 CharacterEquipment 이고, 이 창은 그걸 구독해 따라 그릴 뿐이다.
    /// 히어로가 바뀌는 것도 스스로 듣는다 — 부르는 쪽은 Init 으로 "이 매니저들을 봐라" 만 알려준다.
    ///
    /// 상태는 둘뿐이다 — 무엇을 골랐나(_selected), 무엇을 입고 있나(_equipped).
    /// 설명 칸·장착 버튼·장착중 표식·슬롯 표시는 전부 이 둘에서 파생되어 리액티브로 따라간다.
    /// 후보·슬롯을 누르면 선택만 바뀌고, 장착 버튼을 누르는 순간 즉시 장착 요청이 나간다. 드랍도 즉시 장착이다.
    /// </summary>
    public class EquipmentWindow : PopupWindow
    {
        /// <summary>가방 칸 수. 창은 이만큼의 고정 칸만 그린다 — 스크롤은 없다.</summary>
        public const int BagCapacity = 24;

        [SerializeField] private EquipmentSlotWidget[] _slots;
        [SerializeField] private Button[] _tabBtns;
        [SerializeField] private RectTransform _candidatesRoot;
        [SerializeField] private EquipmentCandidateWidget _candidatePrefab;
        [SerializeField] private Button _closeBtn;

        [Header("캐릭터 프리뷰")]
        [SerializeField] private RawImage _preview;

        [Header("스탯 줄")]
        [SerializeField] private EquipmentStatRowWidget _statAttack;
        [SerializeField] private EquipmentStatRowWidget _statHP;
        [SerializeField] private EquipmentStatRowWidget _statMoveSpeed;

        [Header("설명 칸")]
        [SerializeField] private Image _descriptionIcon;
        [SerializeField] private TextMeshProUGUI _descriptionName;
        [SerializeField] private TextMeshProUGUI _descriptionStats;
        [SerializeField] private TextMeshProUGUI _descriptionText;

        [Header("장착 버튼")]
        [SerializeField] private Button _actionBtn;
        [SerializeField] private TextMeshProUGUI _actionBtnLabel;

        /// <summary>후보의 설계값 사본. 자리·이름·그림·설명·스탯은 전부 장비 설계값 에셋에서 흘러들어온다.</summary>
        private readonly Dictionary<string, (EquipmentSlot slot, string name, Sprite icon, string description,
            CharacterStats stats)> _catalog
            = new Dictionary<string, (EquipmentSlot, string, Sprite, string, CharacterStats)>();

        /// <summary>몸 스탯 = 기본 + 성장 합. 여기선 그 분해가 중요하지 않아서 합쳐 받는다. 장비 몫은 입은 것을 보고 창이 얹는다.</summary>
        private CharacterStats _bodyStats;

        private readonly List<EquipmentCandidateWidget> _candidates = new List<EquipmentCandidateWidget>();

        /// <summary>무엇을 골랐나. 빈 문자열 = 아무것도 안 골랐다.</summary>
        private readonly ReactiveProperty<string> _selected = new ReactiveProperty<string>(string.Empty);

        /// <summary>무엇을 입고 있나. 진실의 원천(캐릭터)을 따라 적는 사본이다.</summary>
        private readonly Dictionary<EquipmentSlot, string> _equipped = new Dictionary<EquipmentSlot, string>();

        /// <summary>자리별로 실제 얹히고 있는 스탯. 빈 자리도 0 이 아닐 수 있다 — 무기 자리가 비면 맨손 몫이 얹힌다.</summary>
        private readonly Dictionary<EquipmentSlot, CharacterStats> _equippedStats =
            new Dictionary<EquipmentSlot, CharacterStats>();
        private readonly Subject<Unit> _equippedChanged = new Subject<Unit>();

        /// <summary>보고 있는 탭. -1 = 전체, 그 외 = EquipmentSlots.All 의 인덱스.</summary>
        private readonly ReactiveProperty<int> _tab = new ReactiveProperty<int>(-1);

        private readonly Subject<string> _equipRequested = new Subject<string>();
        private readonly Subject<EquipmentSlot> _unequipRequested = new Subject<EquipmentSlot>();

        private EquipmentManager _equipmentManager;
        private HeroManager _heroManager;
        private IAssets<Hero> _heroAssets;

        /// <summary>창 안에서 장비를 입혀 보는 인형 무대. 창이 만들고 창이 치운다.</summary>
        private CharacterPreviewStage _previewStage;

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
                .Subscribe(this, (_, self) => self.Close())
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

        public void Init(EquipmentManager equipmentManager, HeroManager heroManager, IAssets<Hero> heroAssets)
        {
            _equipmentManager = equipmentManager;
            _heroManager = heroManager;
            _heroAssets = heroAssets;

            _previewStage = new CharacterPreviewStage();
            SetPreview(_previewStage.Texture);

            // 인형은 창이 열려 있는 동안만 돌린다.
            IsOpen
                .Subscribe(this, (isOpen, self) => self._previewStage.SetActive(isOpen))
                .AddTo(this);

            _equipRequested
                .Subscribe(this, (equipmentID, self) => self.Equip(equipmentID))
                .AddTo(this);

            _unequipRequested
                .Subscribe(this, (slot, self) => self.Unequip(slot))
                .AddTo(this);

            // 가방은 영웅들이 함께 쓰는 하나다. 바뀔 때마다 후보를 다시 그린다. 시작 장비는 이미 들어 있으므로 한 번 그려 두고 시작한다.
            heroManager.Inventory.Changed
                .Subscribe(this, (_, self) => self.DrawCandidates())
                .AddTo(this);

            DrawCandidates();

            heroManager.Spawned
                .Subscribe(this, (character, self) =>
                {
                    if (character is Hero hero)
                    {
                        self.SetHero(hero);
                    }
                })
                .AddTo(this);
        }

        private void SetHero(Hero hero)
        {
            // 인형은 같은 프리팹의 빈 몸이다. 아래 구독이 즉시 한 번 돌면서 지금 입은 한 벌이 그대로 입혀진다.
            _previewStage.SetDoll(_heroAssets.Get(hero.ID));

            // 장비 상태가 후보보다 먼저다. 부활하면 창에는 죽은 히어로가 입던 게 남아 있다.
            foreach (var slot in EquipmentSlots.All)
            {
                hero.Equipment.Observe(slot)
                    .Subscribe((self: this, slot), (part, state) => state.self.DrawPart(state.slot, part))
                    .AddTo(hero);
            }

            // 창엔 몸 스탯(기본 + 성장 합)만 준다. 장비 몫은 창이 입은 것·고른 것을 보고 직접 얹어 비교한다.
            hero.CurrentStats
                .Subscribe((self: this, hero), (_, state) =>
                    state.self.SetBodyStats(state.hero.BaseStats.Add(state.hero.GrowthBonus)))
                .AddTo(hero);
        }

        private void DrawPart(EquipmentSlot slot, EquipmentPart part)
        {
            // 맨손은 싸움에선 무기지만 창에선 빈 자리다. 스탯만은 실제로 얹히는 값이라 그대로 넘긴다.
            var isEmpty = part == null || part is WeaponPart { IsBareHands: true };

            SetEquipped(slot, isEmpty ? string.Empty : part.ID, part?.Stats ?? default);
            _previewStage.Wear(slot, part?.Visual);
        }

        /// <summary>
        /// 가방에 든 것만 후보다 — 드랍으로 주운 장비가 그대로 여기 뜬다.
        /// 순서는 가방이 아니라 설계값 에셋 테이블을 따른다. 줍는 순서대로 목록이 뒤섞이면 안 된다.
        /// 벗기는 후보가 아니라 해제 버튼이 맡는다.
        /// </summary>
        private void DrawCandidates()
        {
            var candidates = new List<(string, EquipmentSlot, string, Sprite, string, CharacterStats)>();

            foreach (var equipmentID in _equipmentManager.IDs)
            {
                if (!_heroManager.Inventory.Contains(equipmentID))
                {
                    continue;
                }

                var plan = _equipmentManager.GetPlan(equipmentID);
                candidates.Add((equipmentID, _equipmentManager.GetSlot(equipmentID), plan.DisplayName, _equipmentManager.GetIcon(equipmentID),
                    plan.Description, plan.Stats));
            }

            SetCandidates(candidates);
        }

        private void Equip(string equipmentID)
        {
            var hero = FirstAliveHero();

            if (hero != null)
            {
                _equipmentManager.Wear(hero, equipmentID);
            }
        }

        private void Unequip(EquipmentSlot slot)
        {
            var hero = FirstAliveHero();

            if (hero != null)
            {
                _equipmentManager.TakeOff(hero, slot);
            }
        }

        private Hero FirstAliveHero()
        {
            foreach (var hero in _heroManager.CurrentHeroes)
            {
                if (hero != null && hero.IsAlive)
                {
                    return hero;
                }
            }

            return null;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _previewStage?.Dispose();

            _selected.Dispose();
            _equippedChanged.Dispose();
            _tab.Dispose();
            _equipRequested.Dispose();
            _unequipRequested.Dispose();
        }

        protected override void OnShow()
        {
            _tab.Value = -1;
            SelectEquippedOf(EquipmentSlot.MainHand);
        }

        /// <summary>장비 상태(진실의 원천)가 바뀌면 여기로 들어온다. 빈 ID = 벗은 자리. stats = 그 자리가 지금 얹는 스탯.</summary>
        public void SetEquipped(EquipmentSlot slot, string equipmentID, CharacterStats stats)
        {
            _equipped[slot] = equipmentID;
            _equippedStats[slot] = stats;
            _equippedChanged.OnNext(Unit.Default);
        }

        /// <summary>캐릭터 프리뷰가 그려지는 텍스처. 무엇을 어떻게 찍는지는 바깥(프리뷰 무대)의 일이다.</summary>
        public void SetPreview(Texture texture)
        {
            _preview.texture = texture;
            _preview.enabled = true;
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
            RenderSelection();
        }

        /// <summary>
        /// 에디터 미리보기 전용. 플레이 중이 아니면 Awake 의 구독이 없으므로 상태를 직접 채우고 한 번 그린다.
        /// </summary>
        public void Preview(
            IReadOnlyList<(string equipmentID, EquipmentSlot slot, string displayName, Sprite icon, string description,
                CharacterStats stats)> items,
            IReadOnlyDictionary<EquipmentSlot, string> equipped, CharacterStats bodyStats, string selectedID)
        {
            _bodyStats = bodyStats;
            _selected.Value = selectedID;

            foreach (var (equipmentID, slot, _, _, _, stats) in items)
            {
                if (equipped.TryGetValue(slot, out var worn) && worn == equipmentID)
                {
                    _equipped[slot] = equipmentID;
                    _equippedStats[slot] = stats;
                }
            }

            SetCandidates(items);
        }

        // ---- 파생 렌더링 ----

        /// <summary>선택이 바뀌었다 → 선택 테두리·설명 칸·장착 버튼, 그리고 스탯 줄의 비교 표시.</summary>
        private void RenderSelection()
        {
            RenderStats();

            var id = _selected.Value;

            foreach (var candidate in _candidates)
            {
                candidate.SetSelected(candidate.EquipmentID == id);
            }

            if (!_catalog.TryGetValue(id, out var entry))
            {
                _descriptionName.text = "-";
                _descriptionStats.text = string.Empty;
                _descriptionText.text = string.Empty;
                _descriptionIcon.enabled = false;
                _actionBtn.gameObject.SetActive(false);
                return;
            }

            _descriptionName.text = $"{entry.name}  <size=70%><color=#A6A6B3>{EquipmentSlots.DisplayName(entry.slot)}</color></size>";
            _descriptionStats.text = StatSummary(entry.stats);
            _descriptionText.text = entry.description;
            _descriptionIcon.sprite = entry.icon;
            _descriptionIcon.enabled = entry.icon != null;

            RenderActionButton();
        }

        /// <summary>
        /// 스탯 줄 = 지금의 최종 스탯(몸 + 입은 장비). 미장착 후보를 고른 동안은 "그걸 끼면" 의 값과 나란히 비교된다 —
        /// 같은 자리에 끼고 있던 것이 빠지고 고른 것이 들어간 결과라서, 오르는 스탯과 내리는 스탯이 같이 나올 수 있다.
        /// </summary>
        private void RenderStats()
        {
            var current = _bodyStats.Add(EquippedStats(null, default));
            var after = current;
            var id = _selected.Value;

            if (_catalog.TryGetValue(id, out var entry) && !IsEquipped(id, entry.slot))
            {
                after = _bodyStats.Add(EquippedStats(entry.slot, entry.stats));
            }

            _statAttack.Set(current.AttackPower, after.AttackPower);
            _statHP.Set(current.MaxHP, after.MaxHP);
            _statMoveSpeed.Set(current.MoveSpeed, after.MoveSpeed);
        }

        /// <summary>입은 장비의 스탯 합. swapSlot 을 주면 그 자리만 swapStats 로 바꿔 끼운 셈으로 합산한다.</summary>
        private CharacterStats EquippedStats(EquipmentSlot? swapSlot, CharacterStats swapStats)
        {
            var total = default(CharacterStats);

            foreach (var (slot, stats) in _equippedStats)
            {
                if (slot != swapSlot)
                {
                    total = total.Add(stats);
                }
            }

            return swapSlot.HasValue ? total.Add(swapStats) : total;
        }

        /// <summary>설명 칸에 붙는 "이 장비가 얹는 스탯" 한 줄. 0 인 스탯은 적지 않는다.</summary>
        private static string StatSummary(CharacterStats stats)
        {
            var parts = new List<string>();

            if (stats.AttackPower != 0) parts.Add($"공격력 {stats.AttackPower:+0;-0}");
            if (stats.MaxHP != 0) parts.Add($"체력 {stats.MaxHP:+0;-0}");
            if (stats.MoveSpeed != 0f) parts.Add($"이동속도 {stats.MoveSpeed:+0.#;-0.#}");

            return string.Join("   ", parts);
        }

        /// <summary>장비 상태가 바뀌었다 → 슬롯 표시·장착중 표식·장착 버튼, 그리고 스탯 줄.</summary>
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

        /// <summary>장착 버튼은 선택·장비 상태의 파생값이다: 미장착 → 장착, 장착중 → 해제.</summary>
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
