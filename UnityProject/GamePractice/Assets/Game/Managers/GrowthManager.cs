using System.Collections.Generic;
using R3;

namespace Sayne
{
    /// <summary>히어로 성장의 주인. 적을 잡으면 경험치가 쌓이고, 다 채우면 레벨이 오른다.</summary>
    public class GrowthManager : ManagerBase
    {
        /// <summary>적 하나를 잡으면 주는 경험치.</summary>
        private static readonly Dictionary<string, int> ExpPerKill = new Dictionary<string, int>
        {
            [EnemyID.Goblin] = 8,
            [EnemyID.Ogre] = 30,
        };

        private const int BaseRequiredExp = 40;
        private const int RequiredExpPerLevel = 20;

        private readonly EnemyManager _enemyManager;

        private readonly ReactiveProperty<int> _level = new ReactiveProperty<int>(1);
        private readonly ReactiveProperty<int> _exp = new ReactiveProperty<int>();

        public ReadOnlyReactiveProperty<int> Level => _level;

        /// <summary>다음 레벨까지 채운 비율 0~1. 경험치 바가 이걸 본다.</summary>
        public Observable<float> ExpRatio =>
            _exp.Select(this, (exp, self) => (float)exp / self.RequiredExp);

        /// <summary>지금 레벨에서 다음 레벨까지 필요한 경험치.</summary>
        public int RequiredExp => BaseRequiredExp + (_level.Value - 1) * RequiredExpPerLevel;

        public GrowthManager(EnemyManager enemyManager)
        {
            _enemyManager = enemyManager;
        }

        protected override void OnInit()
        {
            _enemyManager.Died
                .Subscribe(this, (enemy, self) => self.GainExp(ExpPerKill[enemy.ID]))
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            _level.Dispose();
            _exp.Dispose();
        }

        private void GainExp(int amount)
        {
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
