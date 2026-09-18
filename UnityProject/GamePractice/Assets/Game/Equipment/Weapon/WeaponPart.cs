using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 주장비 자리에 낀 무기 한 자루의 기록. 무기 그 자체(Model)는 프리팹의 Weapon 이고, 여기는 그걸 끼운 사실과
    /// 끼울 때 한 번 만든 궁극기 기술을 든다. 맨손도 그림 없는 무기 프리팹이라 Model 은 늘 있다.
    /// </summary>
    public class WeaponPart : EquipmentPart
    {
        /// <summary>프리팹의 무기. 캐릭터 손에 실제로 붙는 건 이걸 복제한 것이다(Character.WornWeapon).</summary>
        public Weapon Model { get; }

        /// <summary>이 무기를 들었을 때의 궁극기. 없으면 null. 끼울 때 한 번 만들어 같은 걸 계속 쓴다 — 판정이 이걸로 대조한다.</summary>
        public CharacterSkill Ultimate { get; }

        public override EquipmentSlot Slot => EquipmentSlot.MainHand;

        /// <summary>무기를 안 든 상태인가. 싸움에선 무기지만 장비창에선 빈 자리로 보인다.</summary>
        public bool IsBareHands => ID == EquipmentID.Weapon.BareHands;

        public WeaponPart(string id, GameObject visual, CharacterStats stats)
            : base(id, visual, stats)
        {
            Model = visual.GetComponent<Weapon>();
            Ultimate = Model.CreateUltimate();
        }
    }
}
