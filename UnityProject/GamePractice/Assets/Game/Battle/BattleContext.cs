namespace Sayne
{
    /// <summary>
    /// 전투 동안 사는 매니저 중 여러 곳이 두루 보는 것들을 한데 묶은 것. BattleScope 가 이들을 세운 직후 만든다.
    /// GameContext 와 같은 절충이다 — UI·연출 대본·전투 페이즈만 받고, 게임 규칙을 가진 매니저는 받지 않는다.
    /// 이보다 먼저 서야 하는 매니저(화면 연출처럼 궁극기 연출이 기대는 것)는 이걸 받을 수 없다 — 아직 없기 때문이다.
    /// </summary>
    public sealed class BattleContext
    {
        public MapManager MapManager { get; }
        public HeroManager HeroManager { get; }
        public EnemyManager EnemyManager { get; }
        public StageManager StageManager { get; }
        public UltimateDirector UltimateDirector { get; }

        public BattleContext(MapManager mapManager, HeroManager heroManager, EnemyManager enemyManager,
            StageManager stageManager, UltimateDirector ultimateDirector)
        {
            MapManager = mapManager;
            HeroManager = heroManager;
            EnemyManager = enemyManager;
            StageManager = stageManager;
            UltimateDirector = ultimateDirector;
        }
    }
}
