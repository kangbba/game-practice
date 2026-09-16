using System.Collections.Generic;

namespace Sayne
{
    /// <summary>장비 선언 테이블. 부위와 무기 수치를 정한다. 비주얼 프리팹은 같은 이름으로 로드한다.</summary>
    public static class EquipmentPlans
    {
        private static readonly Dictionary<string, BodyPart> Table = new Dictionary<string, BodyPart>
        {
            [EquipmentID.Weapon.BareHands] = BodyPart.RightHand,
            [EquipmentID.Weapon.Scythe] = BodyPart.RightHand,
            [EquipmentID.Weapon.Sword] = BodyPart.RightHand,
            [EquipmentID.Weapon.Staff] = BodyPart.RightHand,
            [EquipmentID.Weapon.GoblinClub] = BodyPart.RightHand,
            [EquipmentID.Weapon.OgreClub] = BodyPart.RightHand,

            [EquipmentID.Head.FoxMask] = BodyPart.Head,
            [EquipmentID.Head.Silver] = BodyPart.Head,
            [EquipmentID.Head.WhiteTwin] = BodyPart.Head,

            [EquipmentID.Hair.Silver] = BodyPart.Hair,
            [EquipmentID.Hair.White] = BodyPart.Hair,

            [EquipmentID.Back.Navy] = BodyPart.Back,
            [EquipmentID.Back.Black] = BodyPart.Back,
            [EquipmentID.Back.Violet] = BodyPart.Back,

            [EquipmentID.Neck.Crimson] = BodyPart.Neck,
        };

        /// <summary>무기 정보. 공격력은 나중에 성장으로 올라가고, 계열은 상태이상 부여의 근거가 된다.</summary>
        private static readonly Dictionary<string, WeaponInfo> Weapons = new Dictionary<string, WeaponInfo>
        {
            [EquipmentID.Weapon.BareHands] = new WeaponInfo(WeaponType.Sword, power: 5, range: 2f, comboInterval: 0.45f, cycleInterval: 1.6f),
            [EquipmentID.Weapon.Scythe] = new WeaponInfo(WeaponType.Axe, power: 10, range: 4f, comboInterval: 0.3f, cycleInterval: 1.4f),
            [EquipmentID.Weapon.Sword] = new WeaponInfo(WeaponType.Sword, power: 14, range: 3f, comboInterval: 0.35f, cycleInterval: 1.6f),
            [EquipmentID.Weapon.Staff] = new WeaponInfo(WeaponType.Staff, power: 8, range: 5f, comboInterval: 0.32f, cycleInterval: 1.5f),

            [EquipmentID.Weapon.GoblinClub] = new WeaponInfo(WeaponType.Axe, power: 5, range: 2.5f, comboInterval: 0.5f, cycleInterval: 1.8f),
            [EquipmentID.Weapon.OgreClub] = new WeaponInfo(WeaponType.Axe, power: 12, range: 2.5f, comboInterval: 0.7f, cycleInterval: 2.4f),
        };

        public static IReadOnlyCollection<string> IDs => Table.Keys;

        public static BodyPart BodyPartOf(string equipmentID)
        {
            return Table[equipmentID];
        }

        public static WeaponInfo WeaponOf(string weaponID)
        {
            return Weapons[weaponID];
        }
    }
}
