namespace Sayne
{
    /// <summary>캐릭터 몸의 스탯. 공격 관련 수치는 여기가 아니라 무기(Weapon)가 가진다.</summary>
    public readonly struct CharacterStats
    {
        public readonly int MaxHP;
        public readonly float MoveSpeed;

        public CharacterStats(int maxHP, float moveSpeed)
        {
            MaxHP = maxHP;
            MoveSpeed = moveSpeed;
        }
    }
}
