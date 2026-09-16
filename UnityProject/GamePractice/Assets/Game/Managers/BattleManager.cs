using Cysharp.Threading.Tasks;

namespace Sayne
{
    /// <summary>전투 흐름의 주인. Init 되면 준비→(전투→정산) 웨이브 루프를 스스로 돌린다.</summary>
    public class BattleManager : ManagerBase
    {
        private readonly PhaseManager _phaseManager;
        private readonly MapManager _mapManager;
        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;

        public BattleManager(PhaseManager phaseManager, MapManager mapManager,
            HeroManager heroManager, EnemyManager enemyManager)
        {
            _phaseManager = phaseManager;
            _mapManager = mapManager;
            _heroManager = heroManager;
            _enemyManager = enemyManager;
        }

        protected override void OnInit()
        {
            RunAsync().Forget();
        }

        protected override void OnRelease()
        {
        }

        private async UniTaskVoid RunAsync()
        {
            await _phaseManager.RunAsync(new PreparePhase(_mapManager, _heroManager));

            var wave = 1;
            while (!LifeToken.IsCancellationRequested)
            {
                await _phaseManager.RunAsync(new CombatPhase(_enemyManager, wave));
                await _phaseManager.RunAsync(new ResultPhase(wave));
                wave++;
            }
        }
    }
}
