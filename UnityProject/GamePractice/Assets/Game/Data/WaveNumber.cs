namespace Sayne
{
    /// <summary>
    /// 스테이지-웨이브 좌표. 1-1 부터 1-9 까지 일반 웨이브를 돌고, 열 번째는 항상 보스다.
    /// 보스를 잡으면 다음 스테이지의 첫 웨이브(2-1)로 넘어간다.
    /// </summary>
    public readonly struct WaveNumber
    {
        /// <summary>한 스테이지의 일반 웨이브 수. 이걸 다 돌면 보스가 나온다.</summary>
        public const int NormalWavesPerStage = 9;

        public readonly int Stage;

        /// <summary>스테이지 안에서 몇 번째 웨이브인지. 1~9는 일반, 10은 보스.</summary>
        public readonly int Step;

        public WaveNumber(int stage, int step)
        {
            Stage = stage;
            Step = step;
        }

        public static WaveNumber First => new WaveNumber(1, 1);

        public bool IsBoss => Step > NormalWavesPerStage;

        /// <summary>화면에 그대로 쓰는 표기. 일반은 "1-3", 보스는 "1-보스".</summary>
        public string Label => IsBoss ? $"{Stage}-보스" : $"{Stage}-{Step}";

        /// <summary>보스를 빼고 처음부터 센 일반 웨이브 통산 번호. 난이도 테이블이 이 번호로 찾는다.</summary>
        public int NormalCount => (Stage - 1) * NormalWavesPerStage + Step;

        public WaveNumber Next()
        {
            return IsBoss ? new WaveNumber(Stage + 1, 1) : new WaveNumber(Stage, Step + 1);
        }

        public override string ToString()
        {
            return Label;
        }
    }
}
