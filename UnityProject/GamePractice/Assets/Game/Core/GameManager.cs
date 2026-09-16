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

            await LoadAllAsync();

            // 2단계: 게임플레이 매니저 조립. 로드 전에 Get 을 부르면 에셋 매니저가 에러 로그로 알려준다.
            var phaseManager = AddManager(new PhaseManager("RootPhase", token));
            var mapManager = AddManager(new MapManager(mapAssetManager));
            var equipmentManager = AddManager(new EquipmentManager(equipmentAssetManager));
            var attackManager = AddManager(new AttackManager());
            var skillManager = AddManager(new SkillManager());
            var ultimateManager = AddManager(new UltimateManager());
            var heroManager = AddManager(new HeroManager(heroAssetManager, equipmentManager, attackManager, skillManager, ultimateManager));
            var enemyManager = AddManager(new EnemyManager(enemyAssetManager, equipmentManager, attackManager, skillManager, ultimateManager));
            var particleManager = AddManager(new ParticleManager(particleAssetManager, heroManager, enemyManager));
            var waveManager = AddManager(new WaveManager(enemyManager));
            var currencyManager = AddManager(new CurrencyManager(enemyManager));
            var growthManager = AddManager(new GrowthManager(enemyManager));

            var cameraManager = AddManager(new CameraManager());
            var cameraDirector = AddManager(new CameraDirector(cameraManager, heroManager));
            var worldUIManager = AddManager(new WorldUIManager(cameraManager, heroManager, enemyManager,
                uiAssetManager.WorldHPBarPrefab));

            var phaseUIManager = AddManager(new PhaseUIManager(phaseManager));
            var screenUIManager = AddManager(new ScreenUIManager(phaseUIManager, heroManager,
                waveManager, currencyManager, growthManager, profileAssetManager,
                uiAssetManager.BattlePanelPrefab, uiAssetManager.ResultPanelPrefab));

            var heroControlManager = AddManager(new HeroControlManager(heroManager, enemyManager, screenUIManager.BattlePanel));
            var equipmentUIManager = AddManager(new EquipmentUIManager(screenUIManager.BattlePanel, heroManager,
                equipmentManager));
            var enemyAIManager = AddManager(new EnemyAIManager(heroManager, enemyManager));

            var battleManager = AddManager(new BattleManager(phaseManager, mapManager, heroManager, enemyManager, waveManager));
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
