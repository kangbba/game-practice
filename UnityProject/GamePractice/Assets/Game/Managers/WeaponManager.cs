namespace Sayne
{
    /// <summary>장비 매니저. WeaponDefinition 으로부터 무기를 동적 생성해 캐릭터에 장착·해제한다.</summary>
    public class WeaponManager : ManagerBase
    {
        private readonly IAssets<WeaponDefinition> _weaponAssets;

        public WeaponManager(IAssets<WeaponDefinition> weaponAssets)
        {
            _weaponAssets = weaponAssets;
        }

        protected override void OnInit()
        {
        }

        protected override void OnRelease()
        {
        }

        public Weapon CreateWeapon(string weaponID)
        {
            var definition = _weaponAssets.Get(weaponID);
            return definition == null ? null : new Weapon(definition.name, definition.ToProfile(), definition.AttackAnimation);
        }

        public void Equip(Character character, string weaponID)
        {
            var weapon = CreateWeapon(weaponID);
            if (weapon != null)
            {
                character.Equip(weapon);
            }
        }

        public void Unequip(Character character)
        {
            character.Unequip();
        }
    }
}
