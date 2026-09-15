using UnityEngine;
using UnityEngine.InputSystem;

namespace Sayne
{
    public class HeroInputManager : ManagerBase
    {
        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;

        public HeroInputManager(HeroManager heroManager, EnemyManager enemyManager)
        {
            _heroManager = heroManager;
            _enemyManager = enemyManager;
        }

        protected override void OnInit()
        {
        }

        protected override void OnRelease()
        {
        }

        public void UpdateInput()
        {
            var heroes = _heroManager.CurrentHeroes;
            if (heroes.Count == 0)
            {
                return;
            }

            var hero = heroes[0];
            hero.Move(ReadDirection());

            if (IsAttackPressed() && hero.CanAttack)
            {
                hero.Attack();

                var target = FindTargetInRange(hero);
                if (target != null)
                {
                    target.TakeDamage(hero.AttackPower, hero);
                }
            }
        }

        private static bool IsAttackPressed()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.spaceKey.isPressed;
        }

        private Enemy FindTargetInRange(Hero hero)
        {
            Enemy nearest = null;
            var nearestDistance = hero.AttackRange;

            foreach (var enemy in _enemyManager.CurrentEnemies)
            {
                if (!enemy.IsAlive.CurrentValue)
                {
                    continue;
                }

                var offset = enemy.transform.position - hero.transform.position;
                offset.y = 0f;

                var distance = offset.magnitude;
                if (distance <= nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        private static Vector3 ReadDirection()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector3.zero;
            }

            var direction = Vector3.zero;

            if (keyboard.upArrowKey.isPressed) direction.z += 1f;
            if (keyboard.downArrowKey.isPressed) direction.z -= 1f;
            if (keyboard.rightArrowKey.isPressed) direction.x += 1f;
            if (keyboard.leftArrowKey.isPressed) direction.x -= 1f;

            return direction;
        }
    }
}
