using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 장비창과 게임을 잇는 정책. 히어로 가방을 후보 목록으로 채우고, 창의 적용 요청을 실제 장착으로 바꾸고,
    /// 히어로의 장비 상태(진실의 원천)를 구독해 창 표시를 따라가게 한다.
    /// 창 안의 캐릭터 프리뷰도 같은 장비 상태를 따라 입는 인형이다.
    /// </summary>
    public class EquipmentUIManager : ManagerBase
    {
        private readonly BattlePhaseUIPanel _panel;
        private readonly HeroManager _heroManager;
        private readonly EquipmentManager _equipmentManager;
        private readonly IAssets<Sprite> _portraits;
        private readonly IAssets<Hero> _heroAssets;

        private CharacterPreviewStage _previewStage;

        public EquipmentUIManager(BattlePhaseUIPanel panel, HeroManager heroManager, EquipmentManager equipmentManager,
            IAssets<Sprite> portraits, IAssets<Hero> heroAssets)
        {
            _heroAssets = heroAssets;
            _panel = panel;
            _heroManager = heroManager;
            _equipmentManager = equipmentManager;
            _portraits = portraits;
        }

        protected override void OnInit()
        {
            var window = _panel.EquipmentWindow;

            _previewStage = new CharacterPreviewStage();
            window.SetPreview(_previewStage.Texture);

            window.IsOpen
                .Subscribe(this, (isOpen, self) => self._previewStage.SetActive(isOpen))
                .RegisterTo(LifeToken);

            _heroManager.Spawned
                .Subscribe(this, (character, self) =>
                {
                    if (character is Hero hero)
                    {
                        self.BindHero(hero);
                    }
                })
                .RegisterTo(LifeToken);

            _panel.EquipMenuClicked
                .Subscribe((self: this, window), (_, state) => state.self.Toggle(state.window))
                .RegisterTo(LifeToken);

            window.EquipRequested
                .Subscribe(this, (equipmentID, self) => self.Equip(equipmentID))
                .RegisterTo(LifeToken);

            window.UnequipRequested
                .Subscribe(this, (slot, self) => self.Unequip(slot))
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            _previewStage.Dispose();
        }

        /// <summary>
        /// 가방에 든 것만 후보다 — 드랍으로 주운 장비가 그대로 여기 뜬다.
        /// 순서는 가방이 아니라 설계값 에셋 테이블을 따른다. 줍는 순서대로 목록이 뒤섞이면 안 된다.
        /// 벗기는 후보가 아니라 해제 버튼이 맡는다.
        /// </summary>
        private List<(string, EquipmentSlot, string, Sprite, string, CharacterStats)> BuildCandidates(Hero hero)
        {
            var candidates = new List<(string, EquipmentSlot, string, Sprite, string, CharacterStats)>();

            foreach (var equipmentID in _equipmentManager.IDs)
            {
                if (!hero.Inventory.Contains(equipmentID))
                {
                    continue;
                }

                // 초상화는 있는 것만 그린다. 없는 장비는 자리 박스로 남는다 — 미리 파둔 구조다.
                var portrait = _portraits.Contains(equipmentID) ? _portraits.Get(equipmentID) : null;

                var plan = _equipmentManager.GetPlan(equipmentID);
                candidates.Add((equipmentID, plan.Slot, plan.DisplayName, portrait, plan.Description, plan.Stats));
            }

            return candidates;
        }

        private void BindHero(Hero hero)
        {
            // 가방이 바뀔 때마다 후보를 다시 그린다. 시작 장비는 이미 들어 있으므로 여기서 한 번 그려 두고 시작한다.
            hero.Inventory.Changed
                .Subscribe((self: this, hero), (_, state) =>
                    state.self._panel.EquipmentWindow.SetCandidates(state.self.BuildCandidates(state.hero)))
                .RegisterTo(hero.destroyCancellationToken);

            _panel.EquipmentWindow.SetCandidates(BuildCandidates(hero));

            // 인형은 같은 프리팹의 빈 몸이다. 아래 구독이 즉시 한 번 돌면서 지금 입은 한 벌이 그대로 입혀진다.
            _previewStage.SetDoll(_heroAssets.Get(hero.ID));

            foreach (var slot in EquipmentSlots.All)
            {
                hero.Equipment.Observe(slot)
                    .Subscribe((self: this, slot), (part, state) =>
                    {
                        state.self._panel.EquipmentWindow.SetEquipped(state.slot, part != null ? part.ID : string.Empty);
                        state.self._previewStage.Wear(state.slot, part?.Visual);
                    })
                    .RegisterTo(hero.destroyCancellationToken);
            }

            // 장비창엔 몸 스탯(기본 + 성장 합)만 준다. 장비 몫은 창이 입은 것·고른 것을 보고 직접 얹어 비교한다.
            hero.CurrentStats
                .Subscribe((self: this, hero), (_, state) =>
                    state.self._panel.EquipmentWindow.SetBodyStats(state.hero.BaseStats.Add(state.hero.GrowthBonus)))
                .RegisterTo(hero.destroyCancellationToken);
        }

        private void Toggle(EquipmentWindow window)
        {
            if (window.IsOpen.CurrentValue)
            {
                window.Hide();
            }
            else
            {
                window.Show();
            }
        }

        private void Equip(string equipmentID)
        {
            var hero = FirstAliveHero();
            if (hero != null)
            {
                _equipmentManager.Wear(hero, equipmentID);
            }
        }

        /// <summary>벗기 정책: 무기 자리는 비울 수 없다 — 맨손도 무기라 맨손으로 갈아끼운다.</summary>
        private void Unequip(EquipmentSlot slot)
        {
            var hero = FirstAliveHero();
            if (hero == null)
            {
                return;
            }

            if (slot == EquipmentSlot.MainHand)
            {
                _equipmentManager.Wear(hero, EquipmentID.Weapon.BareHands);
            }
            else
            {
                _equipmentManager.TakeOff(hero, slot);
            }
        }

        private Hero FirstAliveHero()
        {
            foreach (var hero in _heroManager.CurrentHeroes)
            {
                if (hero != null && hero.IsAlive)
                {
                    return hero;
                }
            }

            return null;
        }
    }
}
