using UnityEngine;

namespace Sayne
{
    public class BattlePhase : PhaseBase
    {
        private readonly PhaseManager _subPhaseManager = new PhaseManager();
        private readonly GameContext _context;

        public override string Key => PhaseID.Battle;

        public BattlePhase(GameContext context)
        {
            _context = context;
        }

        public override void OnEnter()
        {
            _context.MapManager.CreateWorldMap();
            _context.HeroManager.SpawnHero(HeroID.Aldric, Vector3.zero);

            _subPhaseManager.Init();
            _subPhaseManager.ChangePhase(new PreparePhase());
        }

        public override void OnExit()
        {
            _subPhaseManager.Release();

            _context.HeroManager.DespawnAll();
            _context.MapManager.ClearWorldMap();
        }
    }
}
