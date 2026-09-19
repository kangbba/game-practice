using System;
using Cysharp.Threading.Tasks;
using R3;

namespace Sayne
{
    /// <summary>
    /// 언제 어떤 대사를 틀지 정하는 정책. 튜토리얼 상황을 구독하고, 상황마다 짝지은 대화 작업을 튼다.
    /// 대사는 두 가지다. 게임을 멈추고 읽히는 대사(튜토리얼 매니저)와, 게임을 멈추지 않는 머리 위 혼잣말.
    /// 지금 상황들은 전부 혼잣말이다 — 첫 웨이브를 넘기면 히어로가 혼잣말을 하고, 첫 보스가 나타나면 히어로와 보스가 한마디씩 한다.
    /// </summary>
    public class TutorialDirector : ManagerBase
    {
        private const int TestBossTalkStage = 1;
        private const string TestHeroLine = "땅이 울린다... 큰 놈이 온다!";
        private const string TestBossLine = "크아아! 여기가 네 무덤이다!";

        /// <summary>보스가 영웅 앞에 닿았다고 보는 거리. 보스 평타 사거리의 이 배수 안이면 도착이다 — 영웅은 화면 가운데에 선다.</summary>
        private const float BossArrivalRangeFactor = 1.2f;

        /// <summary>대화에서 한 줄이 뜨고 다음 줄이 뜨기까지. 넘기지 않고 이 간격으로 이어진다.</summary>
        private const float LineSeconds = 2.5f;

        /// <summary>1스테이지 두 번째 웨이브가 시작될 때 — 곧 첫 웨이브를 넘긴 순간이다.</summary>
        private const int TestIncomingWave = 2;
        private const string TestIncomingLine = "적들이 몰려오고 있어!";

        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;
        private readonly StageManager _stageManager;
        private readonly TutorialManager _tutorialManager;
        private readonly IAssets<CharacterProfile> _profiles;

        /// <summary>연출 대본이라 컨텍스트를 받는다 — 무엇을 보고 말할지는 대본이 늘 때마다 바뀐다.</summary>
        public TutorialDirector(GameContext game, BattleContext battle)
        {
            _heroManager = battle.HeroManager;
            _enemyManager = battle.EnemyManager;
            _stageManager = battle.StageManager;
            _tutorialManager = game.TutorialManager;
            _profiles = game.Assets.Profiles;
        }

        protected override void OnInit()
        {
            _stageManager.WaveStarted
                .Where(number => number.stage == TestBossTalkStage && number.wave == TestIncomingWave)
                .Subscribe(this, (_, self) => self.HeroSay(TestIncomingLine))
                .RegisterTo(LifeToken);

            _enemyManager.Spawned
                .Where(this, (enemy, self) =>
                {
                    return self._stageManager.IsBossWave
                        && self._stageManager.CurrentStageNumber == TestBossTalkStage
                        && enemy.ID == EnemyID.OgreBoss;
                })
                .Take(1)
                .Subscribe(this, (boss, self) => self.PlayFirstBossTalkAsync((Enemy)boss).Forget())
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
        }

        /// <summary>
        /// 첫 보스 등장 대화. 게임을 멈추지 않는다 — 싸움이 이어지는 동안 히어로와 보스 머리 위에 한 줄씩 뜨고 알아서 사라진다.
        /// 보스가 걸어와 영웅 앞(화면 가운데)에 닿은 뒤에야 시작한다. 태어나자마자 말하면 화면 끝에 있어서 누가 말하는지 안 보인다.
        /// 도중에 보스가 쓰러지면 거기서 그친다.
        /// </summary>
        private async UniTaskVoid PlayFirstBossTalkAsync(Enemy boss)
        {
            await UniTask.WaitUntil((self: this, boss), state => !state.boss.IsAlive || state.self.HasArrived(state.boss),
                cancellationToken: LifeToken);

            if (!boss.IsAlive)
            {
                return;
            }

            HeroSay(TestHeroLine);
            await UniTask.Delay(TimeSpan.FromSeconds(LineSeconds), cancellationToken: LifeToken);

            if (!boss.IsAlive)
            {
                return;
            }

            Say(boss, TestBossLine);
        }

        /// <summary>지금 싸우는 히어로가 말한다. 모두 쓰러져 다시 태어나길 기다리는 중이면 말할 이가 없으니 넘어간다.</summary>
        private void HeroSay(string text)
        {
            var hero = _heroManager.FindFirstAliveHero();
            if (hero == null)
            {
                return;
            }

            Say(hero, text);
        }

        private void Say(Character speaker, string text)
        {
            _tutorialManager.SayAsync(speaker, _profiles.Get(speaker.ID).Portrait, text).Forget();
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
