using Cysharp.Threading.Tasks;
using R3;

namespace Sayne
{
    /// <summary>언제 어떤 대사를 틀지 정하는 정책. 지금은 샘플 하나뿐이다 — 1스테이지 보스가 나타나면 히어로와 보스가 한마디씩 한다.</summary>
    public class TutorialDirector : ManagerBase
    {
        private const int TestBossTalkStage = 1;
        private const string TestHeroLine = "땅이 울린다... 큰 놈이 온다!";
        private const string TestBossLine = "크아아! 여기가 네 무덤이다!";

        private readonly TutorialManager _tutorialManager;
        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;
        private readonly WaveManager _waveManager;

        public TutorialDirector(TutorialManager tutorialManager, HeroManager heroManager,
            EnemyManager enemyManager, WaveManager waveManager)
        {
            _tutorialManager = tutorialManager;
            _heroManager = heroManager;
            _enemyManager = enemyManager;
            _waveManager = waveManager;
        }

        protected override void OnInit()
        {
            _enemyManager.Spawned
                .Where(this, (enemy, self) =>
                {
                    var wave = self._waveManager.CurrentWave.CurrentValue;
                    return wave.IsBoss && wave.Stage == TestBossTalkStage && enemy.ID == EnemyID.OgreBoss;
                })
                .Subscribe(this, (boss, self) => self.PlayBossTalkAsync(boss).Forget())
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
        }

        private async UniTaskVoid PlayBossTalkAsync(Character boss)
        {
            await _tutorialManager.PlayAsync(_heroManager.CurrentHeroes[0].transform, TestHeroLine);
            await _tutorialManager.PlayAsync(boss.transform, TestBossLine);
        }
    }
}
