using Cysharp.Threading.Tasks;
using R3;

namespace Sayne
{
    /// <summary>
    /// 언제 어떤 대사를 틀지 정하는 정책. 대사는 두 가지다.
    /// 게임을 멈추고 읽히는 대사(튜토리얼 매니저)와, 게임을 멈추지 않는 머리 위 혼잣말(월드 말풍선).
    /// 지금은 샘플뿐이다 — 첫 웨이브를 넘기면 히어로가 혼잣말을 하고, 1스테이지 보스가 나타나면 히어로와 보스가 한마디씩 한다.
    /// </summary>
    public class TutorialDirector : ManagerBase
    {
        private const int TestBossTalkStage = 1;
        private const string TestHeroLine = "땅이 울린다... 큰 놈이 온다!";
        private const string TestBossLine = "크아아! 여기가 네 무덤이다!";

        /// <summary>1스테이지 두 번째 웨이브가 시작될 때 — 곧 첫 웨이브를 넘긴 순간이다.</summary>
        private const int TestIncomingWave = 2;
        private const string TestIncomingLine = "적들이 몰려오고 있어!";

        private readonly TutorialManager _tutorialManager;
        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;
        private readonly WaveManager _waveManager;
        private readonly WorldSpeechBubble _bubblePrefab;

        /// <summary>지금 히어로 머리 위의 풍선. 히어로의 자식이라 히어로가 죽어 사라지면 같이 사라지고, 새로 태어나면 새로 붙인다.</summary>
        private WorldSpeechBubble _heroBubble;

        public TutorialDirector(TutorialManager tutorialManager, HeroManager heroManager,
            EnemyManager enemyManager, WaveManager waveManager, WorldSpeechBubble bubblePrefab)
        {
            _tutorialManager = tutorialManager;
            _heroManager = heroManager;
            _enemyManager = enemyManager;
            _waveManager = waveManager;
            _bubblePrefab = bubblePrefab;
        }

        protected override void OnInit()
        {
            _heroManager.Spawned
                .Subscribe(this, (hero, self) =>
                    self._heroBubble = WorldSpeechBubble.Create(self._bubblePrefab, hero.transform, hero.GetHeight()))
                .RegisterTo(LifeToken);

            _waveManager.WaveStarted
                .Where(number => number.stage == TestBossTalkStage && number.wave == TestIncomingWave)
                .Subscribe(this, (_, self) => self._heroBubble.Play(TestIncomingLine))
                .RegisterTo(LifeToken);

            _enemyManager.Spawned
                .Where(this, (enemy, self) =>
                {
                    var stage = self._waveManager.CurrentStageNumber.CurrentValue;
                    var wave = self._waveManager.CurrentWaveNumber.CurrentValue;
                    var isBossWave = self._waveManager.GetStagePlan(stage).IsBossWave(wave);

                    return isBossWave && stage == TestBossTalkStage && enemy.ID == EnemyID.OgreBoss;
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
