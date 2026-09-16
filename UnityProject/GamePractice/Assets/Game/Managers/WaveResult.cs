namespace Sayne
{
    /// <summary>한 웨이브를 끝낸 결과. 정산 화면이 그대로 그린다.</summary>
    public readonly struct WaveResult
    {
        public readonly int Wave;
        public readonly int Kills;
        public readonly float ClearSeconds;

        public WaveResult(int wave, int kills, float clearSeconds)
        {
            Wave = wave;
            Kills = kills;
            ClearSeconds = clearSeconds;
        }
    }
}
