using System;
using System.Collections.Generic;
using R3;

namespace Sayne
{
    /// <summary>캐릭터 장비 상태의 주인. 부위별로 뭘 입었는지 리액티브로 노출한다. null 이면 그 부위는 벗은 상태.</summary>
    public class CharacterEquipment : IDisposable
    {
        private readonly Dictionary<BodyPart, ReactiveProperty<EquipmentPart>> _parts =
            new Dictionary<BodyPart, ReactiveProperty<EquipmentPart>>();

        public CharacterEquipment()
        {
            foreach (var bodyPart in BodyParts.All)
            {
                _parts[bodyPart] = new ReactiveProperty<EquipmentPart>();
            }
        }

        public ReadOnlyReactiveProperty<EquipmentPart> Observe(BodyPart bodyPart) => _parts[bodyPart];

        /// <summary>지금 그 부위에 낀 파츠. 벗은 부위면 null.</summary>
        public EquipmentPart this[BodyPart bodyPart] => _parts[bodyPart].Value;

        /// <summary>낀 무기. 공격 수치는 전부 여기서 꺼낸다 — 맨손도 무기라 스폰 뒤엔 항상 있다.</summary>
        public Weapon Weapon => (Weapon)this[BodyPart.RightHand];

        public void Wear(EquipmentPart part)
        {
            _parts[part.BodyPart].Value = part;
        }

        public void TakeOff(BodyPart bodyPart)
        {
            _parts[bodyPart].Value = null;
        }

        /// <summary>한 벌을 통째로 갈아입는다. 한 벌에 없는 부위는 벗는다.</summary>
        public void Wear(EquipmentSet equipment)
        {
            foreach (var bodyPart in BodyParts.All)
            {
                _parts[bodyPart].Value = equipment.Get(bodyPart);
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
