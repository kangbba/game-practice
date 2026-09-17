namespace Sayne
{
    /// <summary>캐릭터 몸의 스탯. 무기 공격력은 여기가 아니라 무기(Weapon)가 가지고, 몸의 기본 공격력이 그 위에 합쳐진다.</summary>
    public readonly struct CharacterStats
    {
        public readonly int MaxHP;
        public readonly float MoveSpeed;

        /// <summary>맨몸의 공격력. 실제 피해는 여기에 무기 공격력을 더해 계산한다.</summary>
        public readonly int AttackPower;

        public CharacterStats(int maxHP, float moveSpeed, int attackPower = 0)
        {
            MaxHP = maxHP;
            MoveSpeed = moveSpeed;
            AttackPower = attackPower;
        }

        /// <summary>두 스탯의 합. 최종 스탯 = 기본 + 성장 합성이 이걸 쓴다.</summary>
        public CharacterStats Add(CharacterStats other)
        {
            return new CharacterStats(
                maxHP: MaxHP + other.MaxHP,
                moveSpeed: MoveSpeed + other.MoveSpeed,
                attackPower: AttackPower + other.AttackPower);
        }
    }
}
