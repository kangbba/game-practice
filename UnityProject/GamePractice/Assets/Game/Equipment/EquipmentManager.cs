using System.Collections.Generic;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 장비 매니저. 설계값 에셋(EquipmentPlan)과 비주얼 프리팹으로 파츠를 만들어 캐릭터에 입히고 벗긴다.
    /// 장비의 수치·이름·설명을 묻는 곳은 전부 여기를 거친다 — 설계값의 주인은 에셋이다.
    /// </summary>
    public class EquipmentManager : ManagerBase
    {
        private readonly IAssets<GameObject> _visuals;
        private readonly IAssets<EquipmentPlan> _plans;

        public EquipmentManager(IAssets<GameObject> visuals, IAssets<EquipmentPlan> plans)
        {
            _visuals = visuals;
            _plans = plans;
        }

        /// <summary>설계값이 있는 장비 전부. 장비창 후보 순서가 여기서 나온다.</summary>
        public IReadOnlyCollection<string> IDs => _plans.IDs;

        protected override void OnInit()
        {
        }

        protected override void OnRelease()
        {
        }

        public EquipmentPlan GetPlan(string equipmentID)
        {
            return _plans.Get(equipmentID);
        }

        public EquipmentPart CreatePart(string equipmentID)
        {
            var plan = _plans.Get(equipmentID);

            // 맨손이나 적 무기처럼 그림이 없는 장비도 있다. 뼈에 아무것도 안 달 뿐이다.
            var visual = _visuals.Contains(equipmentID) ? _visuals.Get(equipmentID) : null;

            return plan.IsWeapon
                ? new Weapon(equipmentID, visual, plan.Weapon, plan.Stats)
                : new Cosmetic(equipmentID, visual, plan.Slot, plan.Stats);
        }

        /// <summary>자리별 ID 묶음을 실제 파츠 한 벌로 바꾼다. 비어 있는 자리는 벗은 채로 둔다.</summary>
        public EquipmentSet CreateSet(EquipmentIDs ids)
        {
            var set = new EquipmentSet();

            foreach (var slot in EquipmentSlots.All)
            {
                var id = ids.Get(slot);
                if (!string.IsNullOrEmpty(id))
                {
                    set.Put(CreatePart(id));
                }
            }

            return set;
        }

        /// <summary>
        /// 착용 조건은 자리 일치뿐이다 — 파츠가 말하는 자리에 입히고, 누가 입든 가리지 않는다.
        /// 전용 무기처럼 착용 제한이 생기면 그 판정은 여기에 붙인다.
        /// </summary>
        public void Wear(Character character, string equipmentID)
        {
            character.Equipment.Wear(CreatePart(equipmentID));
        }

        public void TakeOff(Character character, EquipmentSlot slot)
        {
            character.Equipment.TakeOff(slot);
        }
    }
}
