using R3;
using UnityEngine;

namespace Sayne
{
    public class ParticleManager : ManagerBase
    {
        private const float HitEffectScale = 20f;
        private const float DeathEffectScale = 25f;
        private const float UltimateEffectScale = 40f;

        private const float ChestHeight = 0.9f;

        private readonly IAssets<GameObject> _particleAssets;
        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;

        public ParticleManager(IAssets<GameObject> particleAssets, HeroManager heroManager, EnemyManager enemyManager)
        {
            _particleAssets = particleAssets;
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
            var prefab = _particleAssets.Get(particleID);
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
            var graphic = character.GetComponentInChildren<CharacterMotion>();

            character.Combat.HitMoment
                .Subscribe((self: this, character, graphic), (attack, state) =>
                    state.self.PlayAttack(attack, state.character, state.graphic))
                .RegisterTo(LifeToken);

            character.Damaged
                .Subscribe((self: this, character, graphic), (_, state) =>
                    state.self.Play(ParticleID.HitSpark, ChestPoint(state.character), HitEffectScale,
                        state.graphic.IsFacingRight))
                .RegisterTo(LifeToken);

            character.Died
                .Subscribe((self: this, graphic), (died, state) =>
                    state.self.Play(ParticleID.DeathSmoke, died.transform.position, DeathEffectScale,
                        state.graphic.IsFacingRight))
                .RegisterTo(LifeToken);
        }

        /// <summary>휘두르기 연출은 무기 트레일이 맡는다. 여기선 전용 연출이 있는 궁극기만 튼다.</summary>
        private void PlayAttack(BasicAttack attack, Character character, CharacterMotion graphic)
        {
            if (attack == character.Combat.Ultimate && character.Combat.UltimateParticleID != null)
            {
                Play(character.Combat.UltimateParticleID, character.transform.position, UltimateEffectScale,
                    graphic.IsFacingRight);
            }
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
