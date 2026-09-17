namespace Sayne
{
    /// <summary>
    /// 상태이상 한 건. 무엇을 몇 초 동안 거는가 — 이 둘이 곧 상태이상이다.
    /// 누가 걸든(무기·함정·스킬) 같은 물건이라 거는 쪽에 매이지 않는다.
    /// </summary>
    public readonly struct Debuff
    {
        public readonly DebuffType Type;

        /// <summary>지속 시간(초). 0 이하면 스스로 풀리지 않는다 — Clear 로만 벗겨진다.</summary>
        public readonly float Seconds;

        public Debuff(DebuffType type, float seconds)
        {
            Type = type;
            Seconds = seconds;
        }
    }
}
