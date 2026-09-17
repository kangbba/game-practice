namespace Sayne
{
    /// <summary>퀘스트가 무엇을 세는가. 진행도를 어디서 읽을지가 이걸로 갈린다.</summary>
    public enum QuestType
    {
        /// <summary>성장 레벨이 목표에 닿으면 완료.</summary>
        LevelReach,

        /// <summary>적을 목표 수만큼 잡으면 완료. 판이 시작된 뒤로 통산해서 센다.</summary>
        EnemyKill,
    }
}
