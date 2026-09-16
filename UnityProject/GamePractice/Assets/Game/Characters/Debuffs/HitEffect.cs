namespace Sayne
{
    /// <summary>이 무기에 맞은 쪽이 받는 효과. 어떤 상태이상을 몇 초 동안 거는가.</summary>
    public readonly struct HitEffect
    {
        public readonly Debuff Debuff;
        public readonly float Seconds;

        public HitEffect(Debuff debuff, float seconds)
        {
            Debuff = debuff;
            Seconds = seconds;
        }
    }
}
