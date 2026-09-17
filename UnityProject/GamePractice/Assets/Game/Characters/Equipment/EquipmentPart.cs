using UnityEngine;

namespace Sayne
{
    /// <summary>장착된 파츠 한 개. 비주얼 프리팹은 소켓 본 밑에 그대로 붙는다.</summary>
    public abstract class EquipmentPart
    {
        public string ID { get; }

        /// <summary>자리에 붙일 비주얼 프리팹.</summary>
        public GameObject Visual { get; }

        /// <summary>
        /// 이 파츠가 캐릭터에 얹는 스탯. 캐릭터 기본 스탯과 같은 꼴을 공유한다 —
        /// 그래서 무기든 투구든 어떤 자리든, 구조체에 있는 어떤 스탯이든 얹을 수 있다.
        /// 얹을 게 없으면 0 일 뿐, 구조는 같다.
        /// </summary>
        public CharacterStats Stats { get; }

        public abstract EquipmentSlot Slot { get; }

        protected EquipmentPart(string id, GameObject visual, CharacterStats stats = default)
        {
            ID = id;
            Visual = visual;
            Stats = stats;
        }
    }

    /// <summary>무기가 아닌 파츠 — 방패·투구·갑옷처럼 자리만 차지하고 스탯을 얹는 것들.</summary>
    public class Cosmetic : EquipmentPart
    {
        public override EquipmentSlot Slot { get; }

        public Cosmetic(string id, GameObject visual, EquipmentSlot slot, CharacterStats stats = default)
            : base(id, visual, stats)
        {
            Slot = slot;
        }
    }
}
