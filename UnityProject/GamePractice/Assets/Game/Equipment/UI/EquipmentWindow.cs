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
    /// 창은 상태를 들지 않는다. 상태는 전부 EquipmentSelection 에 있고, 창은 그 스트림에 그리기를 걸어 둘 뿐이라
    /// "다시 그려라" 를 손으로 부를 자리가 없다. 후보·슬롯을 누르면 선택만 바뀌고,
    /// 장착 버튼은 누른 그 순간의 판단(CurrentAction)만 보고 움직인다. 드랍은 즉시 장착이다.
    /// </summary>
    public class EquipmentWindow : PopupWindow
    {
        /// <summary>가방 칸 수. 창은 이만큼의 고정 칸만 그린다 — 스크롤은 없다.</summary>
        public const int BagCapacity = 24;

        private static readonly Color TabOnColor = new Color(0.94f, 0.78f, 0.46f);
        private static readonly Color TabOffColor = new Color(0.65f, 0.65f, 0.7f);

        [SerializeField] private EquipmentSlotWidget[] _slots;
        [SerializeField] private Button[] _tabBtns;
        [SerializeField] private RectTransform _candidatesRoot;
        [SerializeField] private EquipmentCandidateWidget _candidatePrefab;
        [SerializeField] private Button _closeBtn;

        [Header("캐릭터 프리뷰")]
        [SerializeField] private RawImage _preview;

        [Header("스탯 줄")]
        [SerializeField] private EquipmentStatRowWidget[] _statRows;

        [Header("설명 칸")]
        [SerializeField] private Image _descriptionIcon;
        [SerializeField] private TextMeshProUGUI _descriptionName;
        [SerializeField] private TextMeshProUGUI _descriptionStats;
        [SerializeField] private TextMeshProUGUI _descriptionText;

        [Header("장착 버튼")]
        [SerializeField] private Button _actionBtn;
        [SerializeField] private TextMeshProUGUI _actionBtnLabel;

        /// <summary>창이 보는 상태의 전부. 화면은 전부 여기서 파생된다.</summary>
        private readonly EquipmentSelection _selection = new EquipmentSelection();

        private readonly Subject<string> _equipRequested = new Subject<string>();
        private readonly Subject<EquipmentSlot> _unequipRequested = new Subject<EquipmentSlot>();

        private EquipmentManager _equipmentManager;
        private HeroManager _heroManager;
        private IAssets<Hero> _heroAssets;

        /// <summary>창 안에서 장비를 입혀 보는 인형 무대.</summary>
        private CharacterPreviewStage _previewStage;

        public void Init(EquipmentManager equipmentManager, HeroManager heroManager, IAssets<Hero> heroAssets)
        {
            _equipmentManager = equipmentManager;
            _heroManager = heroManager;
            _heroAssets = heroAssets;

            Bind();

            _previewStage = new CharacterPreviewStage(_preview);

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

            // 가방은 영웅들이 함께 쓰는 하나다. 바뀔 때마다 후보를 다시 담는다. 시작 장비는 이미 들어 있으므로 한 번 담아 두고 시작한다.
            heroManager.Inventory.Changed
                .Subscribe(this, (_, self) => self.CollectCandidates())
                .AddTo(this);

            CollectCandidates();

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

        /// <summary>
        /// 에디터 미리보기 전용. 플레이 중이 아니면 Init 이 불리지 않으므로 배선을 직접 걸고 상태를 채운다.
        /// </summary>
        public void Preview(IReadOnlyList<EquipmentEntry> entries, IReadOnlyDictionary<EquipmentSlot, string> worn,
            StatGroup bodyStats, string selectedID)
        {
            Bind();

            _selection.BodyStats.Value = bodyStats;

            foreach (var entry in entries)
            {
                if (worn.TryGetValue(entry.Slot, out var equipmentID) && equipmentID == entry.EquipmentID)
                {
                    _selection.SetWorn(entry.Slot, equipmentID, entry.Stats);
                }
            }

            _selection.SetCandidates(entries);
            _selection.Selected.Value = selectedID;
        }

        protected override void OnShow()
        {
            _selection.Tab.Value = EquipmentSelection.AllTab;
            _selection.SelectWorn(EquipmentSlot.MainHand);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _selection.Dispose();
            _equipRequested.Dispose();
            _unequipRequested.Dispose();
        }

        // ---- 배선 ----

        /// <summary>상태 → 화면의 배선을 전부 건다. 한 번만 불린다 — 게임에선 Init, 에디터 미리보기에선 Preview 가 부른다.</summary>
        private void Bind()
        {
            BindSlots();
            BindTabs();

            _selection.Candidates
                .Subscribe(this, (entries, self) => self.DrawCandidates(entries))
                .AddTo(this);

            _selection.SelectedEntry
                .Subscribe(this, (entry, self) => self.DrawDescription(entry))
                .AddTo(this);

            _selection.Stats
                .Subscribe(this, (stats, self) => self.DrawStats(stats))
                .AddTo(this);

            _selection.Action
                .Subscribe(this, (action, self) => self.DrawActionBtn(action))
                .AddTo(this);

            // 버튼은 상태를 따로 들지 않는다 — 누른 그 순간의 판단만 집어 온다.
            _actionBtn.onClick.AsObservable()
                .Select(_selection, (_, selection) => selection.CurrentAction)
                .Subscribe(this, (action, self) => self.Request(action))
                .AddTo(this);

            _closeBtn.onClick.AsObservable()
                .Subscribe(this, (_, self) => self.Close())
                .AddTo(this);
        }

        private void BindSlots()
        {
            foreach (var slotWidget in _slots)
            {
                slotWidget.Dropped
                    .Subscribe(this, (equipmentID, self) => self._equipRequested.OnNext(equipmentID))
                    .AddTo(this);

                slotWidget.Clicked
                    .Subscribe(this, (slot, self) => self._selection.SelectWorn(slot))
                    .AddTo(this);

                // 낀 것이 바뀌어도, 후보 목록이 늦게 들어와 이름·그림을 그제야 알게 되어도 다시 그린다.
                Observable
                    .CombineLatest(_selection.Observe(slotWidget.Slot), _selection.Candidates, (worn, _) => worn)
                    .Subscribe((self: this, slotWidget), (worn, state) => state.self.DrawSlot(state.slotWidget, worn))
                    .AddTo(this);
            }
        }

        private void BindTabs()
        {
            for (var i = 0; i < _tabBtns.Length; i++)
            {
                var tabIndex = i - 1; // 0번 버튼 = 전체
                _tabBtns[i].onClick.AsObservable()
                    .Subscribe((self: this, tabIndex), (_, state) => state.self._selection.Tab.Value = state.tabIndex)
                    .AddTo(this);

                var label = _tabBtns[i].GetComponentInChildren<TextMeshProUGUI>();

                if (label != null)
                {
                    _selection.Tab
                        .Subscribe((label, tabIndex),
                            (tab, state) => state.label.color = tab == state.tabIndex ? TabOnColor : TabOffColor)
                        .AddTo(this);
                }
            }
        }

        // ---- 그리기 ----

        private void DrawSlot(EquipmentSlotWidget slotWidget, WornEquipment worn)
        {
            if (_selection.TryGetEntry(worn.EquipmentID, out var entry))
            {
                slotWidget.SetItem(entry.DisplayName, entry.Icon);
                return;
            }

            slotWidget.SetItem(string.IsNullOrEmpty(worn.EquipmentID) ? "없음" : worn.EquipmentID, null);
        }

        /// <summary>가방 칸을 다시 깐다. 각 칸은 태어나면서 자기 반응을 스스로 걸고, 사라질 때 그 구독도 함께 사라진다.</summary>
        private void DrawCandidates(IReadOnlyList<EquipmentEntry> entries)
        {
            for (var i = _candidatesRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_candidatesRoot.GetChild(i).gameObject);
            }

            foreach (var entry in entries)
            {
                var candidate = Instantiate(_candidatePrefab, _candidatesRoot);
                candidate.Setup(entry.EquipmentID, entry.Slot, entry.DisplayName, entry.Icon);

                candidate.Clicked
                    .Subscribe(this, (equipmentID, self) => self._selection.Selected.Value = equipmentID)
                    .AddTo(candidate);

                _selection.Selected
                    .Subscribe(candidate, (equipmentID, widget) => widget.SetSelected(widget.EquipmentID == equipmentID))
                    .AddTo(candidate);

                _selection.Observe(entry.Slot)
                    .Subscribe(candidate, (worn, widget) => widget.SetEquipped(worn.EquipmentID == widget.EquipmentID))
                    .AddTo(candidate);

                _selection.Tab
                    .Subscribe((self: this, candidate), (_, state) =>
                        state.candidate.gameObject.SetActive(state.self._selection.IsVisible(state.candidate.Slot)))
                    .AddTo(candidate);
            }
        }

        private void DrawDescription(EquipmentEntry? selected)
        {
            if (selected == null)
            {
                _descriptionName.text = "-";
                _descriptionStats.text = string.Empty;
                _descriptionText.text = string.Empty;
                _descriptionIcon.enabled = false;
                return;
            }

            var entry = selected.Value;

            _descriptionName.text =
                $"{entry.DisplayName}  <size=70%><color=#A6A6B3>{EquipmentSlots.DisplayName(entry.Slot)}</color></size>";
            _descriptionStats.text = StatSummary(entry.Stats);
            _descriptionText.text = entry.Description;
            _descriptionIcon.sprite = entry.Icon;
            _descriptionIcon.enabled = entry.Icon != null;
        }

        private void DrawStats(StatComparison stats)
        {
            foreach (var row in _statRows)
            {
                row.Set(stats.Current.Get(row.StatType), stats.After.Get(row.StatType));
            }
        }

        private void DrawActionBtn(EquipmentAction action)
        {
            _actionBtn.gameObject.SetActive(action.Type != EquipmentActionType.None);

            if (action.Type != EquipmentActionType.None)
            {
                _actionBtnLabel.text = action.Type == EquipmentActionType.Unequip ? "장비 해제" : "장착";
            }
        }

        /// <summary>설명 칸에 붙는 "이 장비가 얹는 스탯" 한 줄. 0 인 스탯은 적지 않는다.</summary>
        private static string StatSummary(StatGroup stats)
        {
            var parts = new List<string>();

            foreach (var type in StatTypes.All)
            {
                var value = stats.Get(type);

                if (value != 0f)
                {
                    parts.Add($"{StatTypes.DisplayName(type)} {value:+0.#;-0.#}");
                }
            }

            return string.Join("   ", parts);
        }

        // ---- 동작 ----

        private void Request(EquipmentAction action)
        {
            switch (action.Type)
            {
                case EquipmentActionType.Equip:
                    _equipRequested.OnNext(action.EquipmentID);
                    break;

                case EquipmentActionType.Unequip:
                    _unequipRequested.OnNext(action.Slot);
                    break;
            }
        }

        private void SetHero(Hero hero)
        {
            // 인형은 같은 프리팹의 빈 몸이다. 아래 구독이 즉시 한 번 돌면서 지금 입은 한 벌이 그대로 입혀진다.
            _previewStage.SetDoll(_heroAssets.Get(hero.ID));

            // 장비 상태가 후보보다 먼저다. 부활하면 창에는 죽은 히어로가 입던 게 남아 있다.
            foreach (var slot in EquipmentSlots.All)
            {
                hero.Equipment.Observe(slot)
                    .Subscribe((self: this, slot), (part, state) => state.self.WearPart(state.slot, part))
                    .AddTo(hero);
            }

            // 창엔 몸 스탯(기본 + 성장 합)만 준다. 장비 몫은 창이 입은 것·고른 것을 보고 직접 얹어 비교한다.
            hero.CurrentStats
                .Subscribe((self: this, hero), (_, state) =>
                    state.self._selection.BodyStats.Value = state.hero.BaseStats.Add(state.hero.GrowthBonus))
                .AddTo(hero);
        }

        private void WearPart(EquipmentSlot slot, EquipmentPart part)
        {
            // 맨손은 싸움에선 무기지만 창에선 빈 자리다. 스탯만은 실제로 얹히는 값이라 그대로 넘긴다.
            var isEmpty = part == null || part is WeaponPart { IsBareHands: true };

            _selection.SetWorn(slot, isEmpty ? string.Empty : part.ID, part?.Stats ?? default);
            _previewStage.Wear(slot, part?.Visual);
        }

        /// <summary>
        /// 가방에 든 것만 후보다 — 드랍으로 주운 장비가 그대로 여기 뜬다.
        /// 순서는 가방이 아니라 설계값 에셋 테이블을 따른다. 줍는 순서대로 목록이 뒤섞이면 안 된다.
        /// 벗기는 후보가 아니라 장착 버튼이 맡는다.
        /// </summary>
        private void CollectCandidates()
        {
            var entries = new List<EquipmentEntry>();

            foreach (var equipmentID in _equipmentManager.IDs)
            {
                if (!_heroManager.Inventory.Contains(equipmentID))
                {
                    continue;
                }

                var plan = _equipmentManager.GetPlan(equipmentID);
                entries.Add(new EquipmentEntry(equipmentID, _equipmentManager.GetSlot(equipmentID), plan.DisplayName,
                    _equipmentManager.GetIcon(equipmentID), plan.Description, plan.Stats));
            }

            _selection.SetCandidates(entries);
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
    }
}
