using System.Collections.Generic;
using R3;

namespace Sayne
{
    /// <summary>재화의 주인. 적을 잡으면 골드가 들어온다. 젬은 아직 얻을 길이 없다.</summary>
    public class CurrencyManager : ManagerBase
    {
        /// <summary>적 하나를 잡으면 주는 골드.</summary>
        private static readonly Dictionary<string, long> GoldPerKill = new Dictionary<string, long>
        {
            [EnemyID.Goblin] = 12,
            [EnemyID.Ogre] = 40,
        };

        private readonly EnemyManager _enemyManager;

        private readonly ReactiveProperty<long> _gold = new ReactiveProperty<long>();
        private readonly ReactiveProperty<long> _gem = new ReactiveProperty<long>();

        public ReadOnlyReactiveProperty<long> Gold => _gold;
        public ReadOnlyReactiveProperty<long> Gem => _gem;

        public CurrencyManager(EnemyManager enemyManager)
        {
            _enemyManager = enemyManager;
        }

        protected override void OnInit()
        {
            _enemyManager.Died
                .Subscribe(this, (enemy, self) => self._gold.Value += GoldPerKill[enemy.ID])
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            _gold.Dispose();
            _gem.Dispose();
        }
    }
}
