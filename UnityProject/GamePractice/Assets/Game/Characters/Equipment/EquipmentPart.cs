using UnityEngine;

namespace Sayne
{
    /// <summary>장착된 파츠 한 개. 비주얼 프리팹은 소켓 본 밑에 그대로 붙는다.</summary>
    public abstract class EquipmentPart
    {
        public string ID { get; }

        /// <summary>부위에 붙일 비주얼 프리팹.</summary>
        public GameObject Visual { get; }

        public abstract BodyPart BodyPart { get; }

        protected EquipmentPart(string id, GameObject visual)
        {
            ID = id;
            Visual = visual;
        }
    }

    /// <summary>수치 없이 보이기만 하는 파츠 — 머리, 머리카락, 망토, 스카프.</summary>
    public class Cosmetic : EquipmentPart
    {
        public override BodyPart BodyPart { get; }

        public Cosmetic(string id, GameObject visual, BodyPart bodyPart) : base(id, visual)
        {
            BodyPart = bodyPart;
        }
    }
}
