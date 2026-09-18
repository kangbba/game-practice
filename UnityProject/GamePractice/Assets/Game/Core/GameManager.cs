using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>매니저를 만들고 Init/Release 만 책임진다. 각 매니저의 동작은 각자 스스로 돌린다.</summary>
    public class GameManager : MonoBehaviour
    {
        private readonly List<ManagerBase> _managers = new List<ManagerBase>();
        private readonly List<ILoadable> _loadables = new List<ILoadable>();

        private void Start()
        {
            StartAsync().Forget();
        }

        private async UniTaskVoid StartAsync()
        {
            // 0단계: 로딩 화면부터 띄운다. 나머지 로드가 도는 동안 보여야 하니 혼자 먼저 불린다.
            var loadingScreenManager = AddManager(new LoadingScreenManager());
            await loadingScreenManager.ShowAsync();

            // 1단계: 에셋 매니저를 만들고 전부 한꺼번에 로드한다.
            var mapAssetManager = AddManager(new MapAssetManager());
            var heroAssetManager = AddManager(new HeroAssetManager());
            var enemyAssetManager = AddManager(new EnemyAssetManager());
            var particleAssetManager = AddManager(new ParticleAssetManager());
            var equipmentAssetManager = AddManager(new EquipmentAssetManager());
            var uiAssetManager = AddManager(new UIAssetManager());
            var profileAssetManager = AddManager(new CharacterProfileAssetManager());
            var enemyDataAssetManager = AddManager(new EnemyDataAssetManager());
            var heroDataAssetManager = AddManager(new HeroDataAssetManager());
            var equipmentPlanAssetManager = AddManager(new EquipmentPlanAssetManager());
            var itemAssetManager = AddManager(new ItemAssetManager());

            await LoadAllAsync(loadingScreenManager);

            // 2단계: 게임플레이 매니저 조립. 로드 전에 Get 을 부르면 에셋 매니저가 에러 로그로 알려준다.
            // 중단이 맨 앞이다 — 창·컷씬·조작·AI 가 전부 이걸 본다.
            var pauseManager = AddManager(new PauseManager());
            var mapManager = AddManager(new MapManager(mapAssetManager.MainMap));
            var equipmentManager = AddManager(new EquipmentManager(equipmentAssetManager, equipmentPlanAssetManager));
            var enemyManager = AddManager(new EnemyManager(enemyAssetManager, enemyDataAssetManager, equipmentManager));
            var currencyManager = AddManager(new CurrencyManager());
            // 성장이 히어로보다 먼저다 — 히어로는 태어날 때 성장 레벨이 얹힌 스탯으로 만들어진다.
            // 성장은 골드를 내고 사는 것이라 재화가 그보다 먼저다.
            var growthManager = AddManager(new GrowthManager(enemyManager, currencyManager));
            var heroManager = AddManager(new HeroManager(heroAssetManager, heroDataAssetManager, equipmentManager, growthManager));
            // 구슬을 뿌리는 쪽과, 무엇을 주울 때 무슨 일이 나는지 정하는 쪽을 나눈다.
            var dropManager = AddManager(new DropManager(heroManager, itemAssetManager));
            var dropDirector = AddManager(new DropDirector(enemyManager, dropManager, currencyManager,
                heroManager, equipmentManager, itemAssetManager));
            var particleManager = AddManager(new ParticleManager(particleAssetManager, heroManager, enemyManager, dropManager));
            var stageManager = AddManager(new StageManager(enemyManager));
            // 기록은 보기만 하는 놈이라 볼 대상이 다 태어난 뒤에 선다. 퀘스트는 이제 기록만 본다.
            var recordManager = AddManager(new RecordManager(enemyManager, growthManager, currencyManager, stageManager));
            var questManager = AddManager(new QuestManager(recordManager, currencyManager));

            var cameraManager = AddManager(new CameraManager());
            var cameraDirector = AddManager(new CameraDirector(cameraManager, heroManager));

            var screenPerformanceManager = AddManager(new ScreenPerformanceManager(pauseManager, heroManager, stageManager,
                profileAssetManager, uiAssetManager.UltimateCutscenePanelPrefab, uiAssetManager.WaveStartPanelPrefab,
                uiAssetManager.LowHealthPanelPrefab));

            // 궁극기 연출은 컷씬(UI 연출)을 부르고, HP바·적 AI·웨이브가 이걸 본다 — 그 사이에 선다.
            var ultimateDirector = AddManager(new UltimateDirector(enemyManager, cameraManager,
                screenPerformanceManager));

            var screenBlurManager = AddManager(new ScreenBlurManager());
            var popupManager = AddManager(new PopupManager(pauseManager, screenBlurManager, uiAssetManager));
            var screenUIManager = AddManager(new ScreenUIManager(pauseManager, cameraManager,
                heroManager, enemyManager, stageManager, questManager, currencyManager, growthManager,
                equipmentManager, ultimateDirector, popupManager, profileAssetManager, heroAssetManager,
                uiAssetManager));

            var tutorialManager = AddManager(new TutorialManager(pauseManager, cameraManager,
                uiAssetManager.SpeechBubbleWidgetPrefab, uiAssetManager.OverlaySpeechBubblePrefab));

            var heroControlManager = AddManager(new HeroControlManager(pauseManager, heroManager, enemyManager, ultimateDirector,
                screenUIManager.BattlePanel));
            var enemyAIManager = AddManager(new EnemyAIManager(pauseManager, heroManager, enemyManager, ultimateDirector));

            var tutorialDirector = AddManager(new TutorialDirector(tutorialManager, heroManager, enemyManager, stageManager,
                uiAssetManager.WorldSpeechBubblePrefab, profileAssetManager));

            // 게임을 굴리는 놈이라 맨 마지막에 태어난다 — 역순 해제에서 제일 먼저 멈춰야 한다.
            // 부품(적·히어로·UI)을 뜯기 전에 엔진이 꺼지는 순서다.
            var phaseManager = AddManager(new PhaseManager());

            var inGamePhase = new InGamePhase(mapManager, heroManager, enemyManager, stageManager, ultimateDirector);
            phaseManager.RunAsync(new LoadingPhase(loadingScreenManager, inGamePhase)).Forget();
        }

        private void OnDestroy()
        {
            for (var i = _managers.Count - 1; i >= 0; i--)
            {
                _managers[i].Release();
            }

            _managers.Clear();
            _loadables.Clear();
        }

        private T AddManager<T>(T manager) where T : ManagerBase
        {
            manager.Init();
            _managers.Add(manager);

            if (manager is ILoadable loadable)
            {
                _loadables.Add(loadable);
            }

            return manager;
        }

        /// <summary>
        /// 전부 한꺼번에 돌리고, 끝날 때까지 매 프레임 각 매니저의 어드레서블 진행도 평균을 로딩 화면 게이지로 보낸다.
        /// </summary>
        private async UniTask LoadAllAsync(LoadingScreenManager loadingScreenManager)
        {
            var loads = new List<UniTask>(_loadables.Count);
            foreach (var loadable in _loadables)
            {
                loads.Add(loadable.LoadAsync());
            }

            var loading = UniTask.WhenAll(loads).Preserve();

            while (!loading.Status.IsCompleted())
            {
                loadingScreenManager.SetProgress(AverageProgress());
                await UniTask.Yield();
            }

            // 어느 로드가 터졌으면 여기서 그대로 올라온다.
            await loading;

            loadingScreenManager.SetProgress(1f);
        }

        private float AverageProgress()
        {
            var sum = 0f;
            foreach (var loadable in _loadables)
            {
                sum += loadable.Progress;
            }

            return sum / _loadables.Count;
        }
    }
}
