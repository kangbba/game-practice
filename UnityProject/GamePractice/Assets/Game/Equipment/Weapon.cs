using UnityEngine;

namespace Sayne
{
    /// <summary>장착 무기. 공격력·사거리·간격을 준다. 어떤 타격을 내는지는 캐릭터가 정한다.</summary>
    public class Weapon : EquipmentPart
    {
        public WeaponInfo Info { get; }

        public override EquipmentSlot Slot => EquipmentSlot.MainHand;

        /// <summary>휘두를 때 트레일을 쓸지. 프리팹에 트레일이 있어도 여기서 끄면 안 나온다.</summary>
        public bool UseTrail => Info.UseTrail;

        public Weapon(string id, GameObject visual, WeaponInfo info, CharacterStats stats)
            : base(id, visual, stats)
        {
            Info = info;
        }
    }
}
