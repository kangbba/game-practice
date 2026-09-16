using R3;

namespace Sayne
{
    public interface IAttacker
    {
        int AttackPower { get; }
        float AttackRange { get; }
        bool CanAttack { get; }

        Observable<Unit> Attacked { get; }

        void Attack();
    }
}
