using UnityEngine;

namespace Sayne
{
    public class BattlePhase : PhaseBase
    {
        private readonly GameContext _context;

        public BattlePhase(GameContext context)
        {
            _context = context;
        }

        public override void OnEnter()
        {
            _context.MapManager.CreateWorldMap();
            _context.HeroManager.SpawnHero(HeroID.Aldric, Vector3.zero);
        }

        public override void OnExit()
        {
            _context.HeroManager.DespawnAll();
            _context.MapManager.ClearWorldMap();
        }
    }
}
