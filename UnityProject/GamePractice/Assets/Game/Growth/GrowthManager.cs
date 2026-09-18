using System.Collections.Generic;
using R3;

namespace Sayne
{
    /// <summary>
    /// 히어로 성장의 주인. 성장은 저절로 오르지 않는다 — 골드를 내고 항목별 레벨을 하나씩 산다.
    /// 스탯에 얹는 수치와 값은 GrowthPlan 이 레벨에서 결정론적으로 만든다 —
    /// 그래서 나중에 저장을 붙일 때도 항목별 레벨만 남기면 된다. (지금은 저장 없음)
    ///
    /// 경험치·전투 레벨은 성장과 별개 트랙이다. 적을 잡으면 오르지만 스탯은 주지 않는다 —
    /// HUD 레벨 표시와 가이드가 그걸 본다.
    /// </summary>
    public class GrowthManager : ManagerBase
    {
        private const int BaseRequiredExp = 40;
        private const int RequiredExpPerLevel = 20;

        private readonly EnemyManager _enemyManager;
        private readonly CurrencyManager _currencyManager;

        private readonly ReactiveProperty<int> _level = new ReactiveProperty<int>(1);
        private readonly ReactiveProperty<int> _exp = new ReactiveProperty<int>();
        private readonly Subject<int> _expGained = new Subject<int>();

        /// <summary>항목별 성장 레벨. 1 = 아직 아무것도 안 산 상태.</summary>
        private readonly Dictionary<StatType, ReactiveProperty<int>> _statLevels =
            new Dictionary<StatType, ReactiveProperty<int>>();

        private readonly ReactiveProperty<StatGroup> _bonus = new ReactiveProperty<StatGroup>();

        public ReadOnlyReactiveProperty<int> Level => _level;

        /// <summary>지금 레벨에서 쌓은 경험치. HUD 가 "쌓은/필요" 를 그릴 때 본다.</summary>
        public ReadOnlyReactiveProperty<int> Exp => _exp;

        /// <summary>경험치가 들어온 순간과 그 양. 정산 화면이 "이번 웨이브에 얼마 벌었나"를 여기서 센다.</summary>
        public Observable<int> ExpGained => _expGained;

        /// <summary>다음 레벨까지 채운 비율 0~1. 경험치 바가 이걸 본다.</summary>
        public Observable<float> ExpRatio =>
            _exp.Select(this, (exp, self) => (float)exp / self.RequiredExp);

        /// <summary>지금 레벨에서 다음 레벨까지 필요한 경험치.</summary>
        public int RequiredExp => BaseRequiredExp + (_level.Value - 1) * RequiredExpPerLevel;

        /// <summary>산 성장 전부를 합친 몫. 히어로가 이걸 구독해 기본 스탯 위에 얹는다.</summary>
        public ReadOnlyReactiveProperty<StatGroup> Bonus => _bonus;

        public GrowthManager(EnemyManager enemyManager, CurrencyManager currencyManager)
        {
            _enemyManager = enemyManager;
            _currencyManager = currencyManager;

            foreach (var stat in GrowthPlan.All)
            {
                _statLevels[stat] = new ReactiveProperty<int>(1);
            }
        }

        protected override void OnInit()
        {
            _enemyManager.Died
                .Subscribe(this, (enemy, self) => self.GainExp(enemy.Data.ExpReward))
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            _level.Dispose();
            _exp.Dispose();
            _expGained.Dispose();
            _bonus.Dispose();

            foreach (var statLevel in _statLevels.Values)
            {
                statLevel.Dispose();
            }
        }

        public ReadOnlyReactiveProperty<int> StatLevel(StatType stat) => _statLevels[stat];

        /// <summary>지금 이 항목을 한 칸 올리는 값. 레벨이 오를수록 비싸진다 — 수식은 GrowthPlan 이 가진다.</summary>
        public long CostToUpgrade(StatType stat) => GrowthPlan.CostToUpgrade(stat, _statLevels[stat].Value);

        public bool IsMaxLevel(StatType stat) => _statLevels[stat].Value >= GrowthPlan.MaxLevel(stat);

        /// <summary>버튼을 켤지 끌지 정할 때 쓴다. 실제로 사는 건 TryUpgrade 뿐이다.</summary>
        public bool CanUpgrade(StatType stat) =>
            !IsMaxLevel(stat) && _currencyManager.Gold.CurrentValue >= CostToUpgrade(stat);

        /// <summary>골드가 모자라거나 만렙이면 아무 일도 없이 false. 성공하면 골드가 빠지고 레벨이 한 칸 오른다.</summary>
        public bool TryUpgrade(StatType stat)
        {
            if (IsMaxLevel(stat) || !_currencyManager.TrySpendGold(CostToUpgrade(stat)))
            {
                return false;
            }

            _statLevels[stat].Value++;
            _bonus.Value = ComposeBonus();
            return true;
        }

        /// <summary>항목별 보너스를 전부 합친 값. 성장 몫의 진실은 늘 레벨에서 다시 계산된다.</summary>
        private StatGroup ComposeBonus()
        {
            var bonus = default(StatGroup);

            foreach (var stat in GrowthPlan.All)
            {
                bonus = bonus.Add(GrowthPlan.BonusFor(stat, _statLevels[stat].Value));
            }

            return bonus;
        }

        private void GainExp(int amount)
        {
            _expGained.OnNext(amount);

            var total = _exp.Value + amount;

            while (total >= RequiredExp)
            {
                total -= RequiredExp;
                _level.Value++;
            }

            _exp.Value = total;
        }
    }
}
