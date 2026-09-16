using R3;
using UnityEngine;

namespace Sayne
{
    public class WorldUIManager : ManagerBase
    {
        private const string RootName = "WorldUIRoot";

        /// <summary>빌보드(=카메라) 기준 오프셋이라 y 가 화면상 '위'다. 머리 높이에 맞춰 눈으로 조정할 값.</summary>
        private static readonly Vector3 HPBarOffset = new Vector3(0f, 2.2f, 0f);

        private readonly CameraManager _cameraManager;
        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;
        private readonly WorldHPBar _hpBarPrefab;

        public Transform CurrentRoot { get; private set; }

        public WorldUIManager(CameraManager cameraManager, HeroManager heroManager, EnemyManager enemyManager,
            WorldHPBar hpBarPrefab)
        {
            _cameraManager = cameraManager;
            _heroManager = heroManager;
            _enemyManager = enemyManager;
            _hpBarPrefab = hpBarPrefab;
        }

        protected override void OnInit()
        {
            CurrentRoot = new GameObject(RootName).transform;

            _heroManager.Spawned
                .Merge(_enemyManager.Spawned)
                .Subscribe(this, (character, self) => self.CreateHPBar(character))
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            if (CurrentRoot != null)
            {
                Object.Destroy(CurrentRoot.gameObject);
            }

            CurrentRoot = null;
        }

        private void CreateHPBar(Character owner)
        {
            var hpBar = Object.Instantiate(_hpBarPrefab, CurrentRoot);
            var currentHP = owner.CurrentHP
                .Select(hp => (float)hp)
                .ToReadOnlyReactiveProperty();

            hpBar.Attach(owner.transform, HPBarOffset, currentHP, owner.CurrentStats.CurrentValue.MaxHP);

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
