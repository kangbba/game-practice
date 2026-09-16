using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 장비창과 게임을 잇는 정책. 후보 목록을 채우고, 창의 적용 요청을 실제 장착으로 바꾸고,
    /// 히어로의 장비 상태(진실의 원천)를 구독해 창 표시를 따라가게 한다.
    /// </summary>
    public class EquipmentUIManager : ManagerBase
    {
        private readonly BattlePhaseUIPanel _panel;
        private readonly HeroManager _heroManager;
        private readonly WeaponManager _weaponManager;
        private readonly IAssets<WeaponDefinition> _weaponAssets;

        public EquipmentUIManager(BattlePhaseUIPanel panel, HeroManager heroManager, WeaponManager weaponManager,
            IAssets<WeaponDefinition> weaponAssets)
        {
            _panel = panel;
            _heroManager = heroManager;
            _weaponManager = weaponManager;
            _weaponAssets = weaponAssets;
        }

        protected override void OnInit()
        {
            var window = _panel.EquipmentWindow;

            var candidates = new List<(string, string, Sprite)> { (string.Empty, "맨손", null) };
            foreach (var weaponID in _weaponAssets.IDs)
            {
                candidates.Add((weaponID, weaponID, _weaponAssets.Get(weaponID).Icon));
            }

            window.SetCandidates(candidates);

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

            window.Applied
                .Subscribe(this, (weaponID, self) => self.Apply(weaponID))
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
        }

        private void BindHero(Hero hero)
        {
            hero.Equipment.CurrentWeapon
                .Subscribe(this, (weapon, self) =>
                    self._panel.EquipmentWindow.SetEquippedID(weapon != null ? weapon.Name : string.Empty))
                .RegisterTo(hero.destroyCancellationToken);
        }

        private void Toggle(EquipmentWindow window)
        {
            if (window.IsOpen)
            {
                window.Hide();
            }
            else
            {
                window.Show();
            }
        }

        private void Apply(string weaponID)
        {
            foreach (var hero in _heroManager.CurrentHeroes)
            {
                if (hero == null || !hero.IsAlive.CurrentValue)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(weaponID))
                {
                    _weaponManager.Unequip(hero);
                }
                else
                {
                    _weaponManager.Equip(hero, weaponID);
                }

                return;
            }
        }
    }
}
