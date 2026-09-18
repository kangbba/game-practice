using System;
using System.Collections.Generic;
using R3;

namespace Sayne
{
    /// <summary>캐릭터 장비 상태의 주인. 자리별로 뭘 입었는지 리액티브로 노출한다. null 이면 그 자리는 벗은 상태.</summary>
    public class CharacterEquipment : IDisposable
    {
        private readonly Dictionary<EquipmentSlot, ReactiveProperty<EquipmentPart>> _parts =
            new Dictionary<EquipmentSlot, ReactiveProperty<EquipmentPart>>();

        public CharacterEquipment()
        {
            foreach (var slot in EquipmentSlots.All)
            {
                _parts[slot] = new ReactiveProperty<EquipmentPart>();
            }
        }

        public ReadOnlyReactiveProperty<EquipmentPart> Observe(EquipmentSlot slot) => _parts[slot];

        /// <summary>지금 그 자리에 낀 파츠. 벗은 자리면 null.</summary>
        public EquipmentPart this[EquipmentSlot slot] => _parts[slot].Value;

        /// <summary>낀 무기. 사거리·간격 같은 싸움 방식은 여기서 꺼낸다 — 맨손도 무기라 스폰 뒤엔 항상 있다.</summary>
        public WeaponPart Weapon => (WeaponPart)this[EquipmentSlot.MainHand];

        /// <summary>낀 파츠 전부가 얹는 스탯의 합. 캐릭터의 "장비" 근원 하나로 합류한다.</summary>
        public CharacterStats TotalStats()
        {
            var total = new CharacterStats();

            foreach (var property in _parts.Values)
            {
                if (property.Value != null)
                {
                    total = total.Add(property.Value.Stats);
                }
            }

            return total;
        }

        public void Wear(EquipmentPart part)
        {
            _parts[part.Slot].Value = part;
        }

        public void TakeOff(EquipmentSlot slot)
        {
            _parts[slot].Value = null;
        }

        /// <summary>한 벌을 통째로 갈아입는다. 한 벌에 없는 자리는 벗는다.</summary>
        public void Wear(EquipmentSet equipment)
        {
            foreach (var slot in EquipmentSlots.All)
            {
                _parts[slot].Value = equipment.Get(slot);
            }
        }

        public void Dispose()
        {
            foreach (var property in _parts.Values)
            {
                property.Dispose();
            }
        }
    }
}
