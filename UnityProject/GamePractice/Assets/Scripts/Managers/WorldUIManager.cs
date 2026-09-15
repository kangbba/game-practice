using R3;
using UnityEngine;

namespace Sayne
{
    public class WorldUIManager : ManagerBase
    {
        private const string RootName = "WorldUIRoot";
        private const string HPBarPrefabPath = "World/WorldHPBar";

        private static readonly Vector3 HPBarOffset = new Vector3(0f, 6.5f, 0f);

        private readonly CameraManager _cameraManager;
        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;

        private Transform _currentRoot;
        private WorldHPBar _hpBarPrefab;

        public Transform CurrentRoot => _currentRoot;

        public WorldUIManager(CameraManager cameraManager, HeroManager heroManager, EnemyManager enemyManager)
        {
            _cameraManager = cameraManager;
            _heroManager = heroManager;
            _enemyManager = enemyManager;
        }

        protected override void OnInit()
        {
            _currentRoot = new GameObject(RootName).transform;

            _hpBarPrefab = Resources.Load<WorldHPBar>(HPBarPrefabPath);

            _heroManager.Spawned
                .Merge(_enemyManager.Spawned)
                .Subscribe(this, (character, self) => self.CreateHPBar(character))
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            if (_currentRoot != null)
            {
                Object.Destroy(_currentRoot.gameObject);
            }

            _currentRoot = null;
            _hpBarPrefab = null;
        }

        private void CreateHPBar(Character owner)
        {
            var hpBar = Object.Instantiate(_hpBarPrefab, _currentRoot);
            var currentHP = owner.CurrentHP
                .Select(hp => (float)hp)
                .ToReadOnlyReactiveProperty();

            hpBar.Attach(owner.transform, HPBarOffset, currentHP, owner.MaxHP);

            owner.Died
                .Subscribe((hpBar, currentHP), (_, state) =>
                {
                    state.currentHP.Dispose();
                    Object.Destroy(state.hpBar.gameObject);
                })
                .RegisterTo(LifeToken);
        }
    }
}
