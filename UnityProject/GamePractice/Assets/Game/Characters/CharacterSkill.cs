namespace Sayne
{
    /// <summary>
    /// 캐릭터 스킬. 평타에 쿨타임이 더 붙은 것이다.
    /// 쿨타임이 0이면 사이클 마지막에 나가는 고유스킬, 0보다 크면 사이클 밖에서 도는 궁극기다.
    /// </summary>
    public class CharacterSkill : BasicAttack
    {
        public float Cooldown { get; }

        public CharacterSkill(string name, string animation, float powerMultiplier, float cooldown = 0f,
            float hitTime = 0.12f, float staggerSeconds = 0.4f)
            : base(name, animation, powerMultiplier, hitTime, staggerSeconds)
        {
            Cooldown = cooldown;
        }
    }
}
