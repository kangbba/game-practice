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
            var token = destroyCancellationToken;

            // 1단계: 에셋 매니저를 만들고 전부 한꺼번에 로드한다.
            var mapAssetManager = AddManager(new MapAssetManager());
            var heroAssetManager = AddManager(new HeroAssetManager());
            var enemyAssetManager = AddManager(new EnemyAssetManager());
            var particleAssetManager = AddManager(new ParticleAssetManager());
            var equipmentAssetManager = AddManager(new EquipmentAssetManager());
            var uiAssetManager = AddManager(new UIAssetManager());
            var profileAssetManager = AddManager(new ProfileAssetManager());
            var enemyPlanAssetManager = AddManager(new EnemyPlanAssetManager());
            var heroPlanAssetManager = AddManager(new HeroPlanAssetManager());
            var equipmentPlanAssetManager = AddManager(new EquipmentPlanAssetManager());
            var dropPortraitAssetManager = AddManager(new DropPortraitAssetManager());

            await LoadAllAsync();

            // 2단계: 게임플레이 매니저 조립. 로드 전에 Get 을 부르면 에셋 매니저가 에러 로그로 알려준다.
            // 중단이 맨 앞이다 — 창·컷씬·조작·AI 가 전부 이걸 본다.
            var pauseManager = AddManager(new PauseManager());
            var phaseManager = AddManager(new PhaseManager("RootPhase", token));
            var mapManager = AddManager(new MapManager(mapAssetManager));
            var equipmentManager = AddManager(new EquipmentManager(equipmentAssetManager, equipmentPlanAssetManager));
            var enemyManager = AddManager(new EnemyManager(enemyAssetManager, enemyPlanAssetManager, equipmentManager));
            var currencyManager = AddManager(new CurrencyManager());
            // 성장이 히어로보다 먼저다 — 히어로는 태어날 때 성장 레벨이 얹힌 스탯으로 만들어진다.
            // 성장은 골드를 내고 사는 것이라 재화가 그보다 먼저다.
            var growthManager = AddManager(new GrowthManager(enemyManager, currencyManager));
            var heroManager = AddManager(new HeroManager(heroAssetManager, heroPlanAssetManager, equipmentManager, growthManager));
            var particleManager = AddManager(new ParticleManager(particleAssetManager, heroManager, enemyManager));
            var dropManager = AddManager(new DropManager(enemyManager, heroManager, currencyManager,
                equipmentManager, dropPortraitAssetManager, uiAssetManager.DropItemPrefab));
            var waveManager = AddManager(new WaveManager(enemyManager));
            var questManager = AddManager(new QuestManager(enemyManager, growthManager, currencyManager));

            var cameraManager = AddManager(new CameraManager());
            var cameraDirector = AddManager(new CameraDirector(cameraManager, heroManager));

            var screenUIManager = AddManager(new ScreenUIManager(pauseManager, cameraManager,
                heroManager, enemyManager, waveManager, questManager, currencyManager, growthManager,
                equipmentManager, profileAssetManager, heroAssetManager,
                uiAssetManager.BattlePanelPrefab,
                uiAssetManager.OverlayHPBarPrefab, uiAssetManager.DamageTextPrefab));

            var uiDirectionManager = AddManager(new UIDirectionManager(pauseManager, heroManager, waveManager,
                profileAssetManager, uiAssetManager.UltimateCutscenePanelPrefab, uiAssetManager.WaveStartPanelPrefab,
                uiAssetManager.LowHealthPanelPrefab));

            var tutorialManager = AddManager(new TutorialManager(pauseManager, cameraManager,
                uiAssetManager.TutorialWidgetPrefab, uiAssetManager.OverlaySpeechBubblePrefab));

            var heroControlManager = AddManager(new HeroControlManager(pauseManager, heroManager, enemyManager, screenUIManager.BattlePanel));
            var enemyAIManager = AddManager(new EnemyAIManager(pauseManager, heroManager, enemyManager));

            var tutorialDirector = AddManager(new TutorialDirector(tutorialManager, heroManager, enemyManager, waveManager));

            var inGamePhase = new InGamePhase(mapManager, heroManager, enemyManager, waveManager);
            phaseManager.RunAsync(new LoadingPhase(inGamePhase)).Forget();
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

        private UniTask LoadAllAsync()
        {
            var loads = new List<UniTask>(_loadables.Count);
            foreach (var loadable in _loadables)
            {
                loads.Add(loadable.LoadAsync());
            }

            return UniTask.WhenAll(loads);
        }
    }
}
