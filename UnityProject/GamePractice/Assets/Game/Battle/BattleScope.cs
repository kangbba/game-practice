using System;
using System.Collections.Generic;

namespace Sayne
{
    /// <summary>
    /// 전투 동안만 사는 매니저들의 조립처. 전투가 시작될 때 만들어져 매니저를 조립하고, 끝날 때 태어난 역순으로 해제한다.
    /// 에셋은 따로 산다 — 로드는 전투에 들어가기 전에 끝나 있고(BattleAssets), 놓는 건 이 스코프를 연 InGamePhase 가 한다.
    /// 게임 쪽은 GameContext 로 통째로 받는다. 의존은 전투에서 게임 쪽으로만 흐르고, 게임 쪽은 이 안의 누구도 모른다.
    ///
    /// 매니저에 넘기는 법은 둘로 나뉜다. 게임 규칙을 가진 매니저에는 필요한 것만 하나씩 꺼내 주고,
    /// UI·연출 대본에는 컨텍스트(GameContext·BattleContext)를 통째로 준다.
    /// </summary>
    public sealed class BattleScope : ManagerScope
    {
        /// <summary>게임 쪽에 걸어 둔 것(팝업 등록 등). 전투 매니저보다 먼저 풀어야 창이 치워진 매니저를 보지 않는다.</summary>
        private readonly List<IDisposable> _bindings = new List<IDisposable>();

        /// <summary>전투에서 두루 보는 매니저 묶음. 전투 페이즈도 이걸로 판을 굴린다.</summary>
        public BattleContext Context { get; }

        public BattleScope(GameContext game, BattleAssets assets)
        {
            // 게임 규칙 — 필요한 것만 하나씩 받는다.
            var mapManager = Add(new MapManager(assets.MainMap));
            var enemyManager = Add(new EnemyManager(assets.Enemies, assets.EnemyData, game.EquipmentManager));
            var heroManager = Add(new HeroManager(game.Assets.Heroes, assets.HeroData, game.EquipmentManager,
                game.GrowthManager, game.PartyManager));

            // 구슬을 뿌리는 쪽과, 무엇을 주울 때 무슨 일이 나는지 정하는 쪽을 나눈다.
            var dropManager = Add(new DropManager(heroManager, assets.Items));
            Add(new DropDirector(enemyManager, dropManager, game.CurrencyManager, game.PartyManager,
                game.EquipmentManager, assets.Items));
            Add(new ParticleManager(assets.Particles, heroManager, enemyManager, dropManager));

            var stageManager = Add(new StageManager(enemyManager));
            Add(new BattleReportDirector(enemyManager, stageManager, game.GrowthManager, game.RecordManager));

            Add(new CameraDirector(heroManager));

            // 궁극기 연출이 기대는 화면 연출은 그보다 먼저 서야 해서 컨텍스트를 받지 못한다.
            var screenPerformanceManager = Add(new ScreenPerformanceManager(heroManager, stageManager,
                game.Assets.Profiles, assets.UI));
            // 궁극기 연출은 컷씬(UI 연출)을 부르고, HP바·적 AI·웨이브가 이걸 본다 — 그 사이에 선다.
            var ultimateDirector = Add(new UltimateDirector(enemyManager, screenPerformanceManager));

            Context = new BattleContext(mapManager, heroManager, enemyManager, stageManager, ultimateDirector);

            // UI·연출 대본 — 컨텍스트를 통째로 받는다.
            // 창은 전투 매니저를 보므로 등록도 전투와 함께 풀린다. 팝업 매니저는 창의 속사정을 모른다.
            var popups = game.PopupManager;
            _bindings.Add(popups.Bind<GrowthWindow>(PopupType.Growth, window => window.Init(game, Context)));
            _bindings.Add(popups.Bind<EquipmentWindow>(PopupType.Equipment, window => window.Init(game, Context)));
            _bindings.Add(popups.Bind<FormationWindow>(PopupType.Formation, window => window.Init(game, Context)));

            var screenUIManager = Add(new ScreenUIManager(assets.UI.BattlePanelPrefab));
            screenUIManager.BattlePanel.Init(game, Context);
            // HP 바·데미지 숫자는 캐릭터마다 태어나고 죽는다. 한 벌뿐인 HUD 와 수명이 달라 따로 선다.
            Add(new CharacterUIManager(game, Context, assets.UI));
            Add(new TutorialDirector(game, Context));

            // 조작·AI 는 맨 뒤 — 판과 화면이 다 선 뒤에 움직이기 시작하고, 접을 때는 제일 먼저 멈춘다.
            Add(new HeroControlManager(heroManager, enemyManager, ultimateDirector, screenUIManager.BattlePanel));
            Add(new EnemyAIManager(heroManager, enemyManager, ultimateDirector));
        }

        /// <summary>전투를 접는다. 게임 쪽에 건 것부터 풀고, 매니저는 태어난 역순으로 — 조작·AI 가 먼저 멈추고 몸과 판이 나중에 치워진다.</summary>
        public override void Release()
        {
            foreach (var binding in _bindings)
            {
                binding.Dispose();
            }

            _bindings.Clear();

            base.Release();
        }
    }
}
