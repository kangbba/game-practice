namespace Sayne
{
    /// <summary>장착 무기. 무기가 없으면 캐릭터는 맨손 상태로 자기 맨손 공격 명세를 쓴다.</summary>
    public class Weapon
    {
        public string Name { get; }
        public AttackProfile Attack { get; }

        /// <summary>공격 모션 이름. 비어 있으면 기본 공격 모션을 쓴다.</summary>
        public string AttackAnimation { get; }

        public Weapon(string name, AttackProfile attack, string attackAnimation = "")
        {
            Name = name;
            Attack = attack;
            AttackAnimation = attackAnimation;
        }
    }
}
