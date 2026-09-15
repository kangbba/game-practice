using System.Collections.Generic;
using UnityEngine;

namespace Sayne
{
    public class GameManager : MonoBehaviour
    {
        private readonly List<ManagerBase> _managers = new List<ManagerBase>();

        private void Start()
        {
            var phaseManager = AddManager(new PhaseManager());
            var mapManager = AddManager(new MapManager());
            var heroAssetManager = AddManager(new HeroAssetManager());
            var heroManager = AddManager(new HeroManager(heroAssetManager));

            phaseManager.ChangePhase(new BattlePhase(new GameContext(mapManager, heroManager)));
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
