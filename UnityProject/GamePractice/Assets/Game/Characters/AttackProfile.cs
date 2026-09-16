namespace Sayne
{
    /// <summary>공격 하나의 명세: 위력, 사거리, 쿨타임, 넉백 여부.</summary>
    public readonly struct AttackProfile
    {
        public readonly int Power;
        public readonly float Range;
        public readonly float Interval;
        public readonly bool HasKnockback;

        public AttackProfile(int power, float range, float interval, bool hasKnockback = false)
        {
            Power = power;
            Range = range;
            Interval = interval;
            HasKnockback = hasKnockback;
        }
    }
}
