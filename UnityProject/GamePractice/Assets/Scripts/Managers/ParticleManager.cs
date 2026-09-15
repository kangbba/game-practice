using R3;
using UnityEngine;

namespace Sayne
{
    public class ParticleManager : ManagerBase
    {
        private const float HeroAttackEffectScale = 30f;
        private const float EnemyAttackEffectScale = 5f;
        private const float HitEffectScale = 20f;
        private const float DeathEffectScale = 25f;

        private const float SwingForward = 2.5f;
        private const float SwingHeight = 1.3f;
        private const float ChestHeight = 0.9f;

        private readonly ParticleAssetManager _particleAssetManager;
        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;

        public ParticleManager(ParticleAssetManager particleAssetManager, HeroManager heroManager, EnemyManager enemyManager)
        {
            _particleAssetManager = particleAssetManager;
            _heroManager = heroManager;
            _enemyManager = enemyManager;
        }

        protected override void OnInit()
        {
            _heroManager.Spawned
                .Merge(_enemyManager.Spawned)
                .Subscribe(this, (character, self) => self.BindEffects(character))
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
        }

        public void Play(string particleID, Vector3 position, float scale, bool isFacingRight)
        {
            var prefab = _particleAssetManager.GetParticlePrefab(particleID);
            if (prefab == null)
            {
                return;
            }

            // 파티클은 음수 스케일로 안 뒤집힌다. Y축을 돌려서 반전시킨다.
            var rotation = Quaternion.Euler(0f, isFacingRight ? 0f : 180f, 0f);
            var instance = Object.Instantiate(prefab, position, rotation);

            instance.transform.localScale *= scale;

            Object.Destroy(instance, Lifetime(instance));
        }

        private void BindEffects(Character character)
        {
            var attackScale = character is Hero ? HeroAttackEffectScale : EnemyAttackEffectScale;

            character.Attacked
                .Subscribe((self: this, character, attackScale), (_, state) =>
                    state.self.Play(ParticleID.Slash, SwingPoint(state.character), state.attackScale,
                        state.character.IsFacingRight.CurrentValue))
                .RegisterTo(LifeToken);

            character.Damaged
                .Subscribe((self: this, character), (_, state) =>
                    state.self.Play(ParticleID.HitSpark, ChestPoint(state.character), HitEffectScale,
                        state.character.IsFacingRight.CurrentValue))
                .RegisterTo(LifeToken);

            character.Died
                .Subscribe(this, (died, self) =>
                    self.Play(ParticleID.DeathSmoke, died.transform.position, DeathEffectScale,
                        died.IsFacingRight.CurrentValue))
                .RegisterTo(LifeToken);
        }

        /// <summary>바라보는 쪽 가슴 높이 — 무기를 휘두르는 지점.</summary>
        private static Vector3 SwingPoint(Character character)
        {
            var forward = character.IsFacingRight.CurrentValue ? SwingForward : -SwingForward;
            return character.transform.position + new Vector3(forward, SwingHeight, 0f);
        }

        private static Vector3 ChestPoint(Character character)
        {
            return character.transform.position + new Vector3(0f, ChestHeight, 0f);
        }

        private static float Lifetime(GameObject instance)
        {
            var longest = 0f;

            foreach (var particle in instance.GetComponentsInChildren<ParticleSystem>())
            {
                var main = particle.main;
                longest = Mathf.Max(longest, main.duration + main.startLifetime.constantMax);
            }

            return longest;
        }
    }
}
