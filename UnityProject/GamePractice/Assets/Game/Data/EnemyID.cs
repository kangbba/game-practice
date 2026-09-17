namespace Sayne
{
    public static class EnemyID
    {
        public const string Goblin = "Goblin";

        /// <summary>맨손 오우거. 초반 웨이브에 나온다.</summary>
        public const string Ogre = "Ogre";

        /// <summary>몽둥이를 든 오우거. 몸은 Ogre 프리팹을 그대로 쓰는 변종이다.</summary>
        public const string ArmedOgre = "ArmedOgre";

        /// <summary>스테이지 보스 오우거. 몸은 Ogre 프리팹을 쓰고 체력·공격만 보스급인 변종이다.</summary>
        public const string OgreBoss = "OgreBoss";
    }
}
