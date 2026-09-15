using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    public class GameManager : MonoBehaviour
    {
        private readonly List<ManagerBase> _managers = new List<ManagerBase>();

        private CameraManager _cameraManager;
        private HeroInputManager _heroInputManager;
        private EnemyAIManager _enemyAIManager;

        private void Start()
        {
            var phaseManager = AddManager(new PhaseManager("RootPhase", destroyCancellationToken));
            var mapManager = AddManager(new MapManager());

            var heroAssetManager = AddManager(new HeroAssetManager());
            var heroManager = AddManager(new HeroManager(heroAssetManager));

            var enemyAssetManager = AddManager(new EnemyAssetManager());
            var enemyManager = AddManager(new EnemyManager(enemyAssetManager));

            var particleAssetManager = AddManager(new ParticleAssetManager());
            AddManager(new ParticleManager(particleAssetManager, heroManager, enemyManager));

            _cameraManager = AddManager(new CameraManager(heroManager));
            _heroInputManager = AddManager(new HeroInputManager(heroManager, enemyManager));
            _enemyAIManager = AddManager(new EnemyAIManager(heroManager, enemyManager));

            var worldUIManager = AddManager(new WorldUIManager(_cameraManager, heroManager, enemyManager));

            var context = new GameContext(mapManager, heroManager, enemyManager, _cameraManager, worldUIManager);
            RunBattleAsync(phaseManager, context).Forget();
        }

        private async UniTaskVoid RunBattleAsync(PhaseManager phaseManager, GameContext context)
        {
            await phaseManager.RunAsync(new PreparePhase(context.MapManager, context.HeroManager));
            await phaseManager.RunAsync(new CombatPhase(context.EnemyManager));
            await phaseManager.RunAsync(new ResultPhase(context.MapManager, context.HeroManager));
        }

        private void Update()
        {
            _heroInputManager.UpdateInput();
            _enemyAIManager.UpdateAI();
        }

        private void LateUpdate()
        {
            _cameraManager.UpdateFollow();
            _cameraManager.UpdateBillboardRotation();
        }

        private void OnDestroy()
        {
            for (var i = _managers.Count - 1; i >= 0; i--)
            {
                _managers[i].Release();
            }

            _managers.Clear();
        }

        private T AddManager<T>(T manager) where T : ManagerBase
        {
            manager.Init();
            _managers.Add(manager);
            return manager;
        }
    }
}
