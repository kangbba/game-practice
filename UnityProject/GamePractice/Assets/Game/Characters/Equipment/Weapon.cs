using UnityEngine;

namespace Sayne
{
    /// <summary>장착 무기. 공격력·사거리·간격을 준다. 어떤 타격을 내는지는 캐릭터가 정한다.</summary>
    public class Weapon : EquipmentPart
    {
        public WeaponInfo Info { get; }

        public override BodyPart BodyPart => BodyPart.RightHand;

        public Weapon(string id, GameObject visual, WeaponInfo info) : base(id, visual)
        {
            Info = info;
        }
    }
}
