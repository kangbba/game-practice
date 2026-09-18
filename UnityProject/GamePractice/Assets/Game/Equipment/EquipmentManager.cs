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
        private readonly Dictionary<string, Sprite> _icons = new Dictionary<string, Sprite>();

        private EquipmentIconStage _iconStage;

        public EquipmentManager(IAssets<GameObject> visuals, IAssets<EquipmentPlan> plans)
        {
            _visuals = visuals;
            _plans = plans;
        }

        /// <summary>설계값이 있는 장비 전부. 장비창 후보 순서가 여기서 나온다.</summary>
        public IReadOnlyCollection<string> IDs => _plans.IDs;

        protected override void OnInit()
        {
            _iconStage = new EquipmentIconStage();
        }

        protected override void OnRelease()
        {
            foreach (var icon in _icons.Values)
            {
                Object.Destroy(icon.texture);
                Object.Destroy(icon);
            }

            _icons.Clear();
            _iconStage.Dispose();
        }

        public EquipmentPlan GetPlan(string equipmentID)
        {
            return _plans.Get(equipmentID);
        }

        /// <summary>
        /// 장비창·드랍 구슬에 보이는 그림. 따로 그린 초상화는 없다 — 캐릭터가 실제로 장착하는 장비 프리팹을 아이콘 무대에서 찍은 것이다.
        /// 처음 물어볼 때 한 번 찍어 두고 다시 쓴다.
        /// </summary>
        public Sprite GetIcon(string equipmentID)
        {
            if (!_icons.TryGetValue(equipmentID, out var icon))
            {
                _icons[equipmentID] = icon = _iconStage.Shoot(_visuals.Get(equipmentID));
            }

            return icon;
        }

        /// <summary>그 장비가 끼는 부위. 부위는 장비 프리팹의 부위 스크립트가 스스로 밝힌 것이다.</summary>
        public EquipmentSlot GetSlot(string equipmentID)
        {
            return _visuals.Get(equipmentID).GetComponent<IEquipment>().Slot;
        }

        /// <summary>아이템 카드(스탯)와 장비 프리팹(부위·그 부위만의 것)을 묶어 장착할 수 있는 장비 조각으로 만든다.</summary>
        public EquipmentPart CreatePart(string equipmentID)
        {
            var plan = _plans.Get(equipmentID);
            var visual = _visuals.Get(equipmentID);
            var slot = visual.GetComponent<IEquipment>().Slot;

            return slot == EquipmentSlot.MainHand
                ? new WeaponPart(equipmentID, visual, plan.Stats)
                : new Cosmetic(equipmentID, visual, slot, plan.Stats);
        }

        /// <summary>장비 세트 데이터(부위별 ID)를 실제 장비 세트로 만든다. 비어 있는 부위는 비워 둔다 — 무기 부위만 맨손이 든다.</summary>
        public EquipmentSet CreateSet(EquipmentSetData data)
        {
            return CreateSet(data.All());
        }

        /// <summary>장비 ID 들로 장비 세트를 만든다. 무기가 없으면 맨손이 든다.</summary>
        public EquipmentSet CreateSet(IEnumerable<string> equipmentIDs)
        {
            var set = new EquipmentSet().Put(CreatePart(EquipmentID.Weapon.BareHands));

            foreach (var id in equipmentIDs)
            {
                set.Put(CreatePart(id));
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

        /// <summary>
        /// 어느 자리든 벗을 수 있다. 몸 자리는 맨몸이 되고, 무기 자리만 비지 않는다 — 벗기면 맨손이 든다.
        /// 맨손은 가방에 든 아이템이 아니라 "무기 없음" 의 실체라서, 무기를 벗기는 길은 어디서 오든 여기서 맨손으로 끝난다.
        /// </summary>
        public void TakeOff(Character character, EquipmentSlot slot)
        {
            if (slot == EquipmentSlot.MainHand)
            {
                Wear(character, EquipmentID.Weapon.BareHands);
                return;
            }

            character.Equipment.TakeOff(slot);
        }
    }
}
