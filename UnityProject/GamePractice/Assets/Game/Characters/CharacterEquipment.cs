using System;
using R3;

namespace Sayne
{
    /// <summary>캐릭터 장비 상태의 주인. 현재 무기를 리액티브로 노출한다. null 이면 맨손.</summary>
    public class CharacterEquipment : IDisposable
    {
        private readonly ReactiveProperty<Weapon> _currentWeapon = new ReactiveProperty<Weapon>();

        public ReadOnlyReactiveProperty<Weapon> CurrentWeapon => _currentWeapon;

        public void Equip(Weapon weapon)
        {
            _currentWeapon.Value = weapon;
        }

        public void Unequip()
        {
            _currentWeapon.Value = null;
        }

        public void Dispose()
        {
            _currentWeapon.Dispose();
        }
    }
}
