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

        /// <summary>보스가 영웅 앞에 닿았다고 보는 거리. 보스 평타 사거리의 이 배수 안이면 도착이다 — 영웅은 화면 가운데에 선다.</summary>
        private const float BossArrivalRangeFactor = 1.2f;

        /// <summary>1스테이지 두 번째 웨이브가 시작될 때 — 곧 첫 웨이브를 넘긴 순간이다.</summary>
        private const int TestIncomingWave = 2;
        private const string TestIncomingLine = "적들이 몰려오고 있어!";

        private readonly TutorialManager _tutorialManager;
        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;
        private readonly StageManager _stageManager;
        private readonly WorldSpeechBubble _bubblePrefab;
        private readonly IAssets<CharacterProfile> _profiles;

        /// <summary>지금 히어로 머리 위의 풍선. 히어로의 자식이라 히어로가 죽어 사라지면 같이 사라지고, 새로 태어나면 새로 붙인다.</summary>
        private WorldSpeechBubble _heroBubble;

        public TutorialDirector(TutorialManager tutorialManager, HeroManager heroManager,
            EnemyManager enemyManager, StageManager stageManager, WorldSpeechBubble bubblePrefab,
            IAssets<CharacterProfile> profiles)
        {
            _tutorialManager = tutorialManager;
            _heroManager = heroManager;
            _enemyManager = enemyManager;
            _stageManager = stageManager;
            _bubblePrefab = bubblePrefab;
            _profiles = profiles;
        }

        protected override void OnInit()
        {
            _heroManager.Spawned
                .Subscribe(this, (hero, self) =>
                    self._heroBubble = WorldSpeechBubble.Create(self._bubblePrefab, hero.transform, hero.GetHeight()))
                .RegisterTo(LifeToken);

            _stageManager.WaveStarted
                .Where(number => number.stage == TestBossTalkStage && number.wave == TestIncomingWave)
                .Subscribe(this, (_, self) => self._heroBubble.Play(TestIncomingLine))
                .RegisterTo(LifeToken);

            _enemyManager.Spawned
                .Where(this, (enemy, self) =>
                {
                    return self._stageManager.IsBossWave
                        && self._stageManager.CurrentStageNumber == TestBossTalkStage
                        && enemy.ID == EnemyID.OgreBoss;
                })
                .Subscribe(this, (boss, self) => self.PlayBossTalkAsync((Enemy)boss).Forget())
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
        }

        /// <summary>
        /// 보스가 걸어와 영웅 앞(화면 가운데)에 닿은 뒤에야 대화가 시작된다. 태어나자마자 말하면 화면 끝에 있어서 누가 말하는지 안 보인다.
        /// 닿기 전에 쓰러지면 대화는 없다.
        /// </summary>
        private async UniTaskVoid PlayBossTalkAsync(Enemy boss)
        {
            await UniTask.WaitUntil((self: this, boss), state => !state.boss.IsAlive || state.self.HasArrived(state.boss),
                cancellationToken: LifeToken);

            if (!boss.IsAlive)
            {
                return;
            }

            var hero = _heroManager.FindFirstAliveHero();
            await _tutorialManager.PlayAsync(hero, _profiles.Get(hero.ID).Portrait, TestHeroLine);
            await _tutorialManager.PlayAsync(boss, _profiles.Get(boss.ID).Portrait, TestBossLine);
        }

        /// <summary>보스가 살아 있는 영웅 앞에 닿았나. 거리는 사거리와 같은 기준(깊이를 좁게)으로 잰다.</summary>
        private bool HasArrived(Enemy boss)
        {
            var hero = _heroManager.FindFirstAliveHero();
            if (hero == null)
            {
                return false;
            }

            var offset = hero.transform.position - boss.transform.position;
            return CharacterCombat.DistanceOf(offset) <= boss.Combat.AttackRange * BossArrivalRangeFactor;
        }
    }
}
