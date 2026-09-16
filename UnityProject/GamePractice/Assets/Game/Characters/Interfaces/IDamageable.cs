using R3;

namespace Sayne
{
    public interface IDamageable
    {
        ReadOnlyReactiveProperty<int> CurrentHP { get; }
        bool IsAlive { get; }
        Observable<Character> Died { get; }
        Observable<int> Damaged { get; }

        void TakeDamage(int amount);
    }
}
