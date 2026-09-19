using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 게임 내내 사는 에셋·매니저를 만들고, 꺼질 때 연 역순으로 놓는다. 각 매니저의 동작은 각자 스스로 돌린다.
    /// 전투에서만 사는 매니저는 여기서 만들지 않는다 — 페이즈가 전투에 들어갈 때 BattleScope 로 열고 나올 때 접는다.
    /// 의존은 전투에서 이쪽으로만 흐른다. 여기 매니저들은 전투 쪽을 아무도 모른다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        /// <summary>연 순서대로 쌓인다. 꺼질 때 거꾸로 놓는다.</summary>
        private readonly List<Action> _releases = new List<Action>();

        private void Start()
        {
            StartAsync().Forget();
        }

        private async UniTaskVoid StartAsync()
        {
            // 0단계: UI 입력(EventSystem)이 맨 앞이다 — 로딩 화면의 시작 탭부터 이걸 거친다.
            // 그다음 로딩 화면을 띄운다. 공용 에셋을 로드하는 동안 보여야 하니 혼자 먼저 선다.
            AddManager(new UIInputManager());
            var loadingScreenManager = AddManager(new LoadingScreenManager());
            await loadingScreenManager.ShowAsync();

            // 1단계: 메인 로딩 — 게임 내내 쓰는 에셋. 로드를 마친 묶음만 손에 들어오므로 아래 조립은 전부 로드 뒤다.
            // 전투에서만 쓰는 에셋은 여기서 로드하지 않는다 — 전투 페이즈가 들어가며 스스로 로드한다.
            var gameAssets = await GameAssets.LoadAsync(loadingScreenManager.SetProgress, destroyCancellationToken);
            _releases.Add(gameAssets.Release);

            // 2단계: 게임 내내 사는 것들 — 전투가 끝나도 남는다.
            // 중단(Pause)은 static 이라 세울 게 없다. 카메라는 static 이지만 씬 카메라를 잡고 놓는 수명은 여기서 쥔다.
            _releases.Add(GameCamera.Attach().Dispose);

            // 화면의 공용 틀
            var equipmentManager = AddManager(new EquipmentManager(gameAssets.EquipmentVisuals, gameAssets.EquipmentPlans));
            var screenBlurManager = AddManager(new ScreenBlurManager());
            var popupManager = AddManager(new PopupManager(screenBlurManager, gameAssets.UI));
            var tutorialManager = AddManager(new TutorialManager(gameAssets.UI));

            // 플레이 데이터. 성장은 골드를 내고 사는 것이라 재화가 그보다 먼저다.
            // 전투에서 난 일(경험치·처치·웨이브)은 전투 쪽이 입구(GainExp·AddEnemyKill 등)로 밀어 넣는다.
            var currencyManager = AddManager(new CurrencyManager());
            var growthManager = AddManager(new GrowthManager(currencyManager));
            var partyManager = AddManager(new PartyManager(equipmentManager));
            var recordManager = AddManager(new RecordManager(growthManager, currencyManager));
            var questManager = AddManager(new QuestManager(recordManager, currencyManager));

            // 전투 쪽에 한 번에 넘길 묶음.
            var game = new GameContext(gameAssets, equipmentManager, popupManager, tutorialManager,
                currencyManager, growthManager, partyManager, recordManager, questManager);

            // 메인 로딩 끝 — 시작 탭을 받고 화면을 걷는다. 화면이 옅어지는 사이 전투 페이즈가 제 에셋을 로드하고 판을 세운다.
            await loadingScreenManager.WaitStartAsync(destroyCancellationToken);
            loadingScreenManager.Hide();

            // 3단계: 게임을 굴리는 놈이라 맨 마지막에 선다 — 꺼질 때 제일 먼저 멈춰, 전투를 접은 뒤에 나머지가 내려간다.
            var phaseManager = AddManager(new PhaseManager());
            phaseManager.RunAsync(new InGamePhase(game)).Forget();
        }

        private void OnDestroy()
        {
            for (var i = _releases.Count - 1; i >= 0; i--)
            {
                _releases[i]();
            }

            _releases.Clear();
        }

        private T AddManager<T>(T manager) where T : ManagerBase
        {
            manager.Init();
            _releases.Add(manager.Release);
            return manager;
        }
    }
}
