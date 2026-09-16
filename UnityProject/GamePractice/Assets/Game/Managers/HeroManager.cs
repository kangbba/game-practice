using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Sayne
{
    public class HeroManager : ManagerBase
    {
        private const float ReviveDuration = 5f;

        /// <summary>히어로 기본 설계값. ScriptableObject 대신 당분간 여기서 선언한다.</summary>
        private static readonly Dictionary<string, (CharacterStats Body, EquipmentIDs Outfit)> Plans =
            new Dictionary<string, (CharacterStats, EquipmentIDs)>
            {
                [HeroID.Kage] = (new CharacterStats(maxHP: 100, moveSpeed: 4.5f), new EquipmentIDs(
                    rightHand: EquipmentID.Weapon.Scythe,
                    head: EquipmentID.Head.FoxMask,
                    back: EquipmentID.Back.Navy,
                    neck: EquipmentID.Neck.Crimson)),

                [HeroID.Aldric] = (new CharacterStats(maxHP: 100, moveSpeed: 4.5f), new EquipmentIDs(
                    rightHand: EquipmentID.Weapon.Sword,
                    head: EquipmentID.Head.Silver,
                    hair: EquipmentID.Hair.Silver,
                    back: EquipmentID.Back.Violet)),

                [HeroID.Nyx] = (new CharacterStats(maxHP: 100, moveSpeed: 4.5f), new EquipmentIDs(
                    rightHand: EquipmentID.Weapon.Staff,
                    head: EquipmentID.Head.WhiteTwin,
                    hair: EquipmentID.Hair.White,
                    back: EquipmentID.Back.Black)),
            };

        private readonly IAssets<Hero> _heroAssets;
        private readonly EquipmentManager _equipmentManager;
        private readonly AttackManager _attackManager;
        private readonly SkillManager _skillManager;
        private readonly UltimateManager _ultimateManager;

        private readonly List<Hero> _currentHeroes = new List<Hero>();
        private readonly Subject<Character> _spawned = new Subject<Character>();
        private readonly ReactiveProperty<float> _reviveRemainTime = new ReactiveProperty<float>(0f);

        public IReadOnlyList<Hero> CurrentHeroes => _currentHeroes;
        public Observable<Character> Spawned => _spawned;

        /// <summary>부활까지 남은 시간. 살아있는 동안은 0.</summary>
        public ReadOnlyReactiveProperty<float> ReviveRemainTime => _reviveRemainTime;

        public HeroManager(IAssets<Hero> heroAssets, EquipmentManager equipmentManager,
            AttackManager attackManager, SkillManager skillManager, UltimateManager ultimateManager)
        {
            _heroAssets = heroAssets;
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
            _reviveRemainTime.Dispose();
        }

        public Hero SpawnHero(string heroID, Vector3 position)
        {
            var plan = Plans[heroID];
            var hero = Object.Instantiate(_heroAssets.Get(heroID));
            hero.transform.position = position;
            hero.Init(plan.Body, _attackManager.ComboOf(heroID),
                _skillManager.Of(heroID), _ultimateManager.Of(heroID),
                _equipmentManager.CreateSet(plan.Outfit));

            _currentHeroes.Add(hero);

            hero.Died
                .Subscribe((self: this, heroID, hero), (_, state) => state.self.ReviveAsync(state.heroID, state.hero).Forget())
                .RegisterTo(LifeToken);

            _spawned.OnNext(hero);
            return hero;
        }

        /// <summary>부활 = 시체 디스폰 후 재스폰. HP바·이펙트는 Spawned/Died 흐름이 알아서 재생성한다.</summary>
        private async UniTaskVoid ReviveAsync(string heroID, Hero hero)
        {
            _reviveRemainTime.Value = ReviveDuration;

            while (_reviveRemainTime.Value > 0f)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, LifeToken);

                if (hero == null)
                {
                    _reviveRemainTime.Value = 0f;
                    return;
                }

                _reviveRemainTime.Value = Mathf.Max(_reviveRemainTime.Value - Time.deltaTime, 0f);
            }

            Despawn(hero);
            SpawnHero(heroID, Vector3.zero);
        }

        public void Despawn(Hero hero)
        {
            if (hero == null || !_currentHeroes.Remove(hero))
            {
                return;
            }

            Object.Destroy(hero.gameObject);
        }

        public void DespawnAll()
        {
            foreach (var hero in _currentHeroes)
            {
                if (hero != null)
                {
                    Object.Destroy(hero.gameObject);
                }
            }

            _currentHeroes.Clear();
        }
    }
}
