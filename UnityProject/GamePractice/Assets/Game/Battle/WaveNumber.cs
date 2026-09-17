namespace Sayne
{
    /// <summary>화면에 보이는 웨이브 좌표. 스테이지 안의 몇 번째 웨이브인지, 아니면 그 스테이지의 보스인지.</summary>
    public readonly struct WaveNumber
    {
        public readonly int Stage;

        /// <summary>스테이지 안에서 몇 번째 웨이브인지. 보스는 0 이다.</summary>
        public readonly int Step;

        public readonly bool IsBoss;

        public WaveNumber(int stage, int step) : this(stage, step, false)
        {
        }

        private WaveNumber(int stage, int step, bool isBoss)
        {
            Stage = stage;
            Step = step;
            IsBoss = isBoss;
        }

        public static WaveNumber Boss(int stage)
        {
            return new WaveNumber(stage, 0, true);
        }

        /// <summary>화면에 그대로 쓰는 표기. "1스테이지 3웨이브" 또는 "1스테이지 보스".</summary>
        public string Label => IsBoss ? $"{Stage}스테이지 보스" : $"{Stage}스테이지 {Step}웨이브";

        public override string ToString()
        {
            return Label;
        }
    }
}
