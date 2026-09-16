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
        private readonly WaveManager _waveManager;

        public BattleManager(PhaseManager phaseManager, MapManager mapManager,
            HeroManager heroManager, EnemyManager enemyManager, WaveManager waveManager)
        {
            _phaseManager = phaseManager;
            _mapManager = mapManager;
            _heroManager = heroManager;
            _enemyManager = enemyManager;
            _waveManager = waveManager;
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
            // 토큰을 미리 잡아둔다. Release 뒤에는 LifeToken 자체를 물어볼 수 없다.
            var token = LifeToken;

            await _phaseManager.RunAsync(new PreparePhase(_mapManager, _heroManager));

            var wave = 1;
            while (!token.IsCancellationRequested)
            {
                _waveManager.BeginWave(wave);

                await _phaseManager.RunAsync(new CombatPhase(_enemyManager, wave));

                if (token.IsCancellationRequested)
                {
                    return;
                }

                _waveManager.EndWave();

                await _phaseManager.RunAsync(new ResultPhase(wave));
                wave++;
            }
        }

    }
}
