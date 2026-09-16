using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    public class EnemyManager : ManagerBase
    {
        /// <summary>적 기본 설계값. 히어로와 같은 모양이다 — ScriptableObject 대신 당분간 여기서 선언한다.</summary>
        private static readonly Dictionary<string, (CharacterStats Body, EquipmentIDs Outfit)> Plans =
            new Dictionary<string, (CharacterStats, EquipmentIDs)>
            {
                [EnemyID.Goblin] = (new CharacterStats(maxHP: 50, moveSpeed: 1.5f),
                    new EquipmentIDs(rightHand: EquipmentID.Weapon.GoblinClub)),

                [EnemyID.Ogre] = (new CharacterStats(maxHP: 120, moveSpeed: 1.2f),
                    new EquipmentIDs(rightHand: EquipmentID.Weapon.OgreClub)),
            };

        private readonly IAssets<Enemy> _enemyAssets;
        private readonly EquipmentManager _equipmentManager;
        private readonly AttackManager _attackManager;
        private readonly SkillManager _skillManager;
        private readonly UltimateManager _ultimateManager;

        private readonly List<Enemy> _currentEnemies = new List<Enemy>();
        private readonly Subject<Character> _spawned = new Subject<Character>();
        private readonly Subject<Enemy> _died = new Subject<Enemy>();

        public IReadOnlyList<Enemy> CurrentEnemies => _currentEnemies;
        public Observable<Character> Spawned => _spawned;

        /// <summary>적이 죽었다. 재화·경험치·처치 수가 이걸 본다.</summary>
        public Observable<Enemy> Died => _died;

        public EnemyManager(IAssets<Enemy> enemyAssets, EquipmentManager equipmentManager,
            AttackManager attackManager, SkillManager skillManager, UltimateManager ultimateManager)
        {
            _enemyAssets = enemyAssets;
            _equipmentManager = equipmentManager;
            _attackManager = attackManager;
            _skillManager = skillManager;
            _ultimateManager = ultimateManager;
        }

        protected override void OnInit()
        {
        }

        protected override void OnRelease()
        {
            DespawnAll();
            _spawned.Dispose();
            _died.Dispose();
        }

        public Enemy SpawnEnemy(string enemyID, Vector3 position)
        {
            var plan = Plans[enemyID];
            var enemy = Object.Instantiate(_enemyAssets.Get(enemyID));
            enemy.transform.position = position;

            enemy.Init(plan.Body, _attackManager.ComboOf(enemyID),
                _skillManager.Of(enemyID), _ultimateManager.Of(enemyID),
                _equipmentManager.CreateSet(plan.Outfit));

            _currentEnemies.Add(enemy);

            enemy.Died
                .Subscribe((self: this, enemy), (_, state) => state.self._died.OnNext(state.enemy))
                .RegisterTo(enemy.destroyCancellationToken);

            _spawned.OnNext(enemy);
            return enemy;
        }

        public void DespawnAll()
        {
            foreach (var enemy in _currentEnemies)
            {
                if (enemy != null)
                {
                    Object.Destroy(enemy.gameObject);
                }
            }

            _currentEnemies.Clear();
        }
    }
}
