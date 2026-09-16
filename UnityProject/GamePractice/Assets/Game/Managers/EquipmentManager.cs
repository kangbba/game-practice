using UnityEngine;

namespace Sayne
{
    /// <summary>장비 매니저. 선언 테이블과 비주얼 프리팹으로 파츠를 만들어 캐릭터에 입히고 벗긴다.</summary>
    public class EquipmentManager : ManagerBase
    {
        private readonly IAssets<GameObject> _visuals;

        public EquipmentManager(IAssets<GameObject> visuals)
        {
            _visuals = visuals;
        }

        protected override void OnInit()
        {
        }

        protected override void OnRelease()
        {
        }

        public EquipmentPart CreatePart(string equipmentID)
        {
            var bodyPart = EquipmentPlans.BodyPartOf(equipmentID);

            // 맨손이나 적 무기처럼 그림이 없는 장비도 있다. 뼈에 아무것도 안 달 뿐이다.
            var visual = _visuals.Contains(equipmentID) ? _visuals.Get(equipmentID) : null;

            return bodyPart == BodyPart.RightHand
                ? new Weapon(equipmentID, visual, EquipmentPlans.WeaponOf(equipmentID))
                : new Cosmetic(equipmentID, visual, bodyPart);
        }

        /// <summary>부위별 ID 묶음을 실제 파츠 한 벌로 바꾼다. 비어 있는 부위는 벗은 채로 둔다.</summary>
        public EquipmentSet CreateSet(EquipmentIDs ids)
        {
            var set = new EquipmentSet();

            foreach (var bodyPart in BodyParts.All)
            {
                var id = ids.Get(bodyPart);
                if (!string.IsNullOrEmpty(id))
                {
                    set.Put(CreatePart(id));
                }
            }

            return set;
        }

        /// <summary>
        /// 착용 조건은 부위 일치뿐이다 — 파츠가 말하는 부위에 입히고, 누가 입든 가리지 않는다.
        /// 전용 무기처럼 착용 제한이 생기면 그 판정은 여기에 붙인다.
        /// </summary>
        public void Wear(Character character, string equipmentID)
        {
            character.Equipment.Wear(CreatePart(equipmentID));
        }

        public void TakeOff(Character character, BodyPart bodyPart)
        {
            character.Equipment.TakeOff(bodyPart);
        }
    }
}
