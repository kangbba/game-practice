using R3;

namespace Sayne
{
    /// <summary>
    /// 재화의 주인. 들어오고 나가는 문만 지킨다 — 누가 왜 주는지는 알지 않는다.
    /// 처치 보상은 이제 적의 설계값(EnemyData)이 정하고 드랍 구슬을 거쳐 들어온다.
    /// </summary>
    public class CurrencyManager : ManagerBase
    {
        private readonly ReactiveProperty<long> _gold = new ReactiveProperty<long>();
        private readonly ReactiveProperty<long> _gem = new ReactiveProperty<long>();
        private readonly Subject<long> _goldGained = new Subject<long>();

        public ReadOnlyReactiveProperty<long> Gold => _gold;
        public ReadOnlyReactiveProperty<long> Gem => _gem;

        /// <summary>골드가 들어온 순간과 그 양. 잔액만 보면 "이번 웨이브에 얼마 벌었나"를 알 수 없어서 따로 흘린다.</summary>
        public Observable<long> GoldGained => _goldGained;

        protected override void OnInit()
        {
        }

        /// <summary>골드가 들어온다. 주워 든 동전이든 다른 무엇이든 들어오는 문은 이것 하나다.</summary>
        public void AddGold(long amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _gold.Value += amount;
            _goldGained.OnNext(amount);
        }

        /// <summary>모자라면 아무것도 건드리지 않고 false. 성장 같은 "사는" 쪽은 이 문 하나로만 골드를 쓴다.</summary>
        public bool TrySpendGold(long amount)
        {
            if (amount < 0 || _gold.Value < amount)
            {
                return false;
            }

            _gold.Value -= amount;
            return true;
        }

        protected override void OnRelease()
        {
            _gold.Dispose();
            _gem.Dispose();
            _goldGained.Dispose();
        }
    }
}
