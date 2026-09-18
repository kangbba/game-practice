namespace Sayne
{
    /// <summary>게임이 남기는 기록의 종류. 퀘스트·업적은 전부 "이 기록이 얼마인가" 로만 말한다.</summary>
    public enum RecordType
    {
        /// <summary>플레이한 시간(초). 중단 중에는 세지 않는다.</summary>
        PlayTime,

        /// <summary>잡은 적 수. 열쇠에 적 ID 를 주면 그 적만, 비우면 통산이다.</summary>
        EnemyKill,

        /// <summary>찍은 성장 레벨. 쌓이는 게 아니라 지금까지의 최고값이다.</summary>
        LevelReach,

        /// <summary>통산 번 골드. 쓴 건 빠지지 않는다 — 잔액은 CurrencyManager 가 안다.</summary>
        GoldEarned,

        /// <summary>통산 얻은 경험치.</summary>
        ExpEarned,

        /// <summary>도달한 웨이브. 스테이지가 넘어가도 이어서 센다 — 그래야 "N 이상" 이 말이 된다.</summary>
        WaveReach,
    }
}
