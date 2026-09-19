using R3;

namespace Sayne
{
    /// <summary>
    /// 전투에서 난 일을 게임 내내 사는 쪽(성장·기록)에 올려 보낸다. 적을 잡으면 경험치와 처치 기록, 웨이브가 시작되면 도달 기록.
    /// 전투와 함께 태어나고 사라진다 — 오래 사는 쪽이 전투 매니저를 구독하지 않게 방향을 여기서 뒤집는다.
    /// </summary>
    public class BattleReportDirector : ManagerBase
    {
        private readonly EnemyManager _enemyManager;
        private readonly StageManager _stageManager;
        private readonly GrowthManager _growthManager;
        private readonly RecordManager _recordManager;

        public BattleReportDirector(EnemyManager enemyManager, StageManager stageManager,
            GrowthManager growthManager, RecordManager recordManager)
        {
            _enemyManager = enemyManager;
            _stageManager = stageManager;
            _growthManager = growthManager;
            _recordManager = recordManager;
        }

        protected override void OnInit()
        {
            _enemyManager.Died
                .Subscribe(this, (enemy, self) =>
                {
                    self._growthManager.GainExp(enemy.Data.ExpReward);
                    self._recordManager.AddEnemyKill(enemy.ID);
                })
                .RegisterTo(LifeToken);

            _stageManager.WaveStarted
                .Subscribe(this, (_, self) => self._recordManager.AddWaveReach())
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
        }
    }
}
