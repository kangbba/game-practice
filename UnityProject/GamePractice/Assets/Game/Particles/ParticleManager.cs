using R3;
using UnityEngine;

namespace Sayne
{
    public class ParticleManager : ManagerBase
    {
        private const float HitEffectScale = 20f;
        private const float DeathEffectScale = 25f;
        private const float UltimateEffectScale = 40f;

        /// <summary>구슬을 주울 때 터지는 이펙트. 전용 파티클이 생기면 여기만 바꾼다.</summary>
        private const string PickupParticleID = ParticleID.HitSpark;
        private const float PickupEffectScale = 12f;

        private const float ChestHeight = 0.9f;

        private readonly IAssets<GameObject> _particleAssets;
        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;
        private readonly DropManager _dropManager;

        public ParticleManager(IAssets<GameObject> particleAssets, HeroManager heroManager, EnemyManager enemyManager,
            DropManager dropManager)
        {
            _particleAssets = particleAssets;
            _heroManager = heroManager;
            _enemyManager = enemyManager;
            _dropManager = dropManager;
        }

        protected override void OnInit()
        {
            _heroManager.Spawned
                .Merge(_enemyManager.Spawned)
                .Subscribe(this, (character, self) => self.BindEffects(character))
                .RegisterTo(LifeToken);

            // 구슬을 주운 자리에서 작게 반짝인다.
            _dropManager.Collected
                .Subscribe(this, (position, self) => self.Play(PickupParticleID, position, PickupEffectScale, true, false))
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
        }

        /// <summary>
        /// 이펙트를 한 번 터뜨린다. 궁극기 무대 위 캐릭터에서 나는 것이면 궁극기 백그라운드에 가려지지 않게
        /// 무대 이펙트 레이어로 띄운다.
        /// </summary>
        public void Play(string particleID, Vector3 position, float scale, bool isFacingRight, bool onUltimateStage)
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

            if (onUltimateStage)
            {
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    renderer.sortingLayerName = SortingLayers.UltimateEffect;
                }
            }

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
                        state.graphic.IsFacingRight, state.character.IsOnUltimateStage.CurrentValue))
                .RegisterTo(LifeToken);

            character.Died
                .Subscribe((self: this, graphic), (died, state) =>
                    state.self.Play(ParticleID.DeathSmoke, died.transform.position, DeathEffectScale,
                        state.graphic.IsFacingRight, died.IsOnUltimateStage.CurrentValue))
                .RegisterTo(LifeToken);
        }

        /// <summary>휘두르기 연출은 무기 트레일이 맡는다. 여기선 전용 연출이 있는 궁극기만 튼다.</summary>
        private void PlayAttack(BasicAttack attack, Character character, CharacterMotion graphic)
        {
            if (attack == character.Combat.Ultimate && character.Combat.UltimateParticleID != null)
            {
                Play(character.Combat.UltimateParticleID, character.transform.position, UltimateEffectScale,
                    graphic.IsFacingRight, character.IsOnUltimateStage.CurrentValue);
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
