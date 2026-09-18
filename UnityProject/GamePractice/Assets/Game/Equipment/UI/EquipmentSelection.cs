using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>장착 버튼이 지금 무슨 버튼인가.</summary>
    public enum EquipmentActionType
    {
        None,
        Equip,
        Unequip
    }

    /// <summary>후보 한 건의 설계값 사본. 자리·이름·그림·설명·스탯은 전부 장비 설계값 에셋에서 흘러들어온다.</summary>
    public readonly struct EquipmentEntry
    {
        public readonly string EquipmentID;
        public readonly EquipmentSlot Slot;
        public readonly string DisplayName;
        public readonly Sprite Icon;
        public readonly string Description;
        public readonly StatGroup Stats;

        public EquipmentEntry(string equipmentID, EquipmentSlot slot, string displayName, Sprite icon,
            string description, StatGroup stats)
        {
            EquipmentID = equipmentID;
            Slot = slot;
            DisplayName = displayName;
            Icon = icon;
            Description = description;
            Stats = stats;
        }
    }

    /// <summary>한 자리에 지금 낀 것. 빈 ID = 빈 자리지만 스탯은 0 이 아닐 수 있다 — 무기 자리가 비면 맨손 몫이 얹힌다.</summary>
    public readonly struct WornEquipment
    {
        public readonly string EquipmentID;
        public readonly StatGroup Stats;

        public WornEquipment(string equipmentID, StatGroup stats)
        {
            EquipmentID = equipmentID;
            Stats = stats;
        }
    }

    /// <summary>장착 버튼을 누르면 벌어질 일. 고른 것과 입은 것에서 파생된다.</summary>
    public readonly struct EquipmentAction
    {
        public readonly EquipmentActionType Type;
        public readonly string EquipmentID;
        public readonly EquipmentSlot Slot;

        public EquipmentAction(EquipmentActionType type, string equipmentID, EquipmentSlot slot)
        {
            Type = type;
            EquipmentID = equipmentID;
            Slot = slot;
        }
    }

    /// <summary>스탯 줄이 보여 줄 짝. after 는 "지금 고른 걸 끼면" 의 값이고, 끼울 게 없으면 current 와 같다.</summary>
    public readonly struct StatComparison
    {
        public readonly StatGroup Current;
        public readonly StatGroup After;

        public StatComparison(StatGroup current, StatGroup after)
        {
            Current = current;
            After = after;
        }
    }

    /// <summary>
    /// 장비창이 보는 상태의 전부 — 무엇이 후보인가(Candidates), 무엇을 입었나(Observe), 무엇을 골랐나(Selected),
    /// 어느 탭인가(Tab), 몸 스탯은 얼마인가(BodyStats). 그리는 일은 하지 않는다.
    ///
    /// 설명 칸·장착 버튼·스탯 비교는 전부 이 다섯에서 파생된 스트림이다 — 창은 그걸 구독해 따라 그릴 뿐이고,
    /// "다시 그려라" 를 손으로 부를 자리가 없다.
    /// </summary>
    public sealed class EquipmentSelection : IDisposable
    {
        /// <summary>전체 탭. 자리로 거르지 않는다.</summary>
        public const int AllTab = -1;

        private readonly Dictionary<EquipmentSlot, ReactiveProperty<WornEquipment>> _worn =
            new Dictionary<EquipmentSlot, ReactiveProperty<WornEquipment>>();

        /// <summary>후보의 ID 색인. Candidates 와 항상 같은 내용이다.</summary>
        private readonly Dictionary<string, EquipmentEntry> _entries = new Dictionary<string, EquipmentEntry>();

        private readonly ReactiveProperty<IReadOnlyList<EquipmentEntry>> _candidates =
            new ReactiveProperty<IReadOnlyList<EquipmentEntry>>(Array.Empty<EquipmentEntry>());

        /// <summary>무엇을 골랐나. 빈 문자열 = 아무것도 안 골랐다.</summary>
        public ReactiveProperty<string> Selected { get; } = new ReactiveProperty<string>(string.Empty);

        /// <summary>보고 있는 탭. AllTab = 전체, 그 외 = EquipmentSlots.All 의 인덱스.</summary>
        public ReactiveProperty<int> Tab { get; } = new ReactiveProperty<int>(AllTab);

        /// <summary>몸 스탯 = 기본 + 성장 합. 장비 몫은 여기 없다 — 입은 것을 보고 얹는다.</summary>
        public ReactiveProperty<StatGroup> BodyStats { get; } = new ReactiveProperty<StatGroup>(default);

        public ReadOnlyReactiveProperty<IReadOnlyList<EquipmentEntry>> Candidates => _candidates;

        /// <summary>어느 자리든 바뀌었다.</summary>
        public Observable<Unit> WornChanged { get; }

        /// <summary>지금 고른 것의 설계값. 아무것도 못 골랐거나 후보에 없으면 null.</summary>
        public Observable<EquipmentEntry?> SelectedEntry { get; }

        /// <summary>장착 버튼이 지금 무엇인가.</summary>
        public Observable<EquipmentAction> Action { get; }

        /// <summary>스탯 줄의 지금 값과 비교 값.</summary>
        public Observable<StatComparison> Stats { get; }

        public EquipmentSelection()
        {
            var wornStreams = new Observable<Unit>[EquipmentSlots.All.Length];

            for (var i = 0; i < EquipmentSlots.All.Length; i++)
            {
                var worn = new ReactiveProperty<WornEquipment>(default);
                _worn[EquipmentSlots.All[i]] = worn;
                wornStreams[i] = worn.AsUnitObservable();
            }

            WornChanged = Observable.Merge(wornStreams);

            // 고른 것의 설계값은 선택과 후보 목록 둘 다에 달렸다 — 후보가 늦게 들어와도 설명 칸이 그때 채워진다.
            SelectedEntry = Observable.CombineLatest(_candidates, Selected, (_, id) => Find(id));

            Action = Observable.Merge(SelectedEntry.AsUnitObservable(), WornChanged)
                .Select(this, (_, self) => self.CurrentAction);

            Stats = Observable.Merge(SelectedEntry.AsUnitObservable(), WornChanged, BodyStats.AsUnitObservable())
                .Select(this, (_, self) => self.CurrentStats);
        }

        /// <summary>한 자리가 지금 무엇을 끼고 있나.</summary>
        public ReadOnlyReactiveProperty<WornEquipment> Observe(EquipmentSlot slot)
        {
            return _worn[slot];
        }

        /// <summary>가방이 바뀌었다. 순서는 부르는 쪽이 정한 그대로 쓴다.</summary>
        public void SetCandidates(IReadOnlyList<EquipmentEntry> entries)
        {
            _entries.Clear();

            foreach (var entry in entries)
            {
                _entries[entry.EquipmentID] = entry;
            }

            _candidates.Value = entries;
        }

        /// <summary>장비 상태(진실의 원천)가 바뀌면 여기로 들어온다. 빈 ID = 벗은 자리.</summary>
        public void SetWorn(EquipmentSlot slot, string equipmentID, StatGroup stats)
        {
            _worn[slot].Value = new WornEquipment(equipmentID ?? string.Empty, stats);
        }

        /// <summary>그 자리에 낀 것을 고른다. 빈 자리면 선택은 그대로 둔다.</summary>
        public void SelectWorn(EquipmentSlot slot)
        {
            var equipmentID = _worn[slot].Value.EquipmentID;

            if (!string.IsNullOrEmpty(equipmentID))
            {
                Selected.Value = equipmentID;
            }
        }

        public bool TryGetEntry(string equipmentID, out EquipmentEntry entry)
        {
            return _entries.TryGetValue(equipmentID ?? string.Empty, out entry);
        }

        public bool IsWorn(string equipmentID, EquipmentSlot slot)
        {
            return !string.IsNullOrEmpty(equipmentID) && _worn[slot].Value.EquipmentID == equipmentID;
        }

        /// <summary>그 자리가 지금 탭에 보이나.</summary>
        public bool IsVisible(EquipmentSlot slot)
        {
            return Tab.Value == AllTab || EquipmentSlots.All[Tab.Value] == slot;
        }

        /// <summary>장착 버튼을 누른 그 순간의 판단. 버튼은 이것만 보고 움직인다.</summary>
        public EquipmentAction CurrentAction
        {
            get
            {
                if (!TryGetEntry(Selected.Value, out var entry))
                {
                    return default;
                }

                var type = IsWorn(entry.EquipmentID, entry.Slot)
                    ? EquipmentActionType.Unequip
                    : EquipmentActionType.Equip;

                return new EquipmentAction(type, entry.EquipmentID, entry.Slot);
            }
        }

        /// <summary>
        /// 지금의 최종 스탯(몸 + 입은 장비)과, 미장착 후보를 고른 동안의 "그걸 끼면" 값.
        /// 같은 자리에 끼고 있던 것이 빠지고 고른 것이 들어간 결과라서 오르는 스탯과 내리는 스탯이 같이 나올 수 있다.
        /// </summary>
        public StatComparison CurrentStats
        {
            get
            {
                var current = BodyStats.Value.Add(WornStats(null, default));

                if (TryGetEntry(Selected.Value, out var entry) && !IsWorn(entry.EquipmentID, entry.Slot))
                {
                    return new StatComparison(current, BodyStats.Value.Add(WornStats(entry.Slot, entry.Stats)));
                }

                return new StatComparison(current, current);
            }
        }

        public void Dispose()
        {
            foreach (var worn in _worn.Values)
            {
                worn.Dispose();
            }

            _candidates.Dispose();
            Selected.Dispose();
            Tab.Dispose();
            BodyStats.Dispose();
        }

        /// <summary>입은 장비의 스탯 합. swapSlot 을 주면 그 자리만 swapStats 로 바꿔 끼운 셈으로 합산한다.</summary>
        private StatGroup WornStats(EquipmentSlot? swapSlot, StatGroup swapStats)
        {
            var total = default(StatGroup);

            foreach (var (slot, worn) in _worn)
            {
                if (slot != swapSlot)
                {
                    total = total.Add(worn.Value.Stats);
                }
            }

            return swapSlot.HasValue ? total.Add(swapStats) : total;
        }

        private EquipmentEntry? Find(string equipmentID)
        {
            return TryGetEntry(equipmentID, out var entry) ? entry : null;
        }
    }
}
