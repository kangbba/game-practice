namespace Sayne
{
    public class GameContext
    {
        public GameContext(MapManager mapManager, HeroManager heroManager, EnemyManager enemyManager,
            CameraManager cameraManager, WorldUIManager worldUIManager)
        {
            MapManager = mapManager;
            HeroManager = heroManager;
            EnemyManager = enemyManager;
            CameraManager = cameraManager;
            WorldUIManager = worldUIManager;
        }

        public MapManager MapManager { get; }
        public HeroManager HeroManager { get; }
        public EnemyManager EnemyManager { get; }
        public CameraManager CameraManager { get; }
        public WorldUIManager WorldUIManager { get; }
    }
}
