namespace Sayne
{
    public class GameContext
    {
        public GameContext(MapManager mapManager, HeroManager heroManager)
        {
            MapManager = mapManager;
            HeroManager = heroManager;
        }

        public MapManager MapManager { get; }
        public HeroManager HeroManager { get; }
    }
}
