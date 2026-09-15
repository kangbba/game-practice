namespace Sayne
{
    public readonly struct CharacterStats
    {
        public readonly int MaxHP;
        public readonly float MoveSpeed;
        public readonly int AttackPower;
        public readonly float AttackRange;
        public readonly float AttackInterval;

        public CharacterStats(int maxHP, float moveSpeed, int attackPower, float attackRange, float attackInterval)
        {
            MaxHP = maxHP;
            MoveSpeed = moveSpeed;
            AttackPower = attackPower;
            AttackRange = attackRange;
            AttackInterval = attackInterval;
        }
    }
}
