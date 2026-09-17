using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>캐릭터의 겉모습. 장비 자리마다 어느 본에 다는지 알고, 거기에 장비 그림을 걸치고 벗긴다.
    /// 머리·머리카락 같은 몸은 프리팹에 이미 붙어 있어 여기서 다루지 않는다.</summary>
    public class CharacterSkin : MonoBehaviour
    {
        /// <summary>장비 자리와 그 자리의 본. 지금 걸친 것도 여기서 들고 있다.</summary>
        [Serializable]
        private class SlotEntry
        {
            [SerializeField] private EquipmentSlot _slot;
            [SerializeField] private Transform _transform;
            [SerializeField] private SpriteRenderer[] _coveredSprites = Array.Empty<SpriteRenderer>();
            [SerializeField] private bool _overrideSortingOrder;
            [SerializeField] private int _sortingOrder;

            private GameObject _worn;

            public EquipmentSlot Slot => _slot;

            /// <summary>입던 걸 벗고 새로 걸친다. 걸친 인스턴스를 준다.</summary>
            public GameObject Wear(GameObject visual)
            {
                TakeOff();

                _worn = UnityEngine.Object.Instantiate(visual, _transform, false);
                foreach (var sprite in _coveredSprites)
                    if (sprite != null) sprite.enabled = false;
                if (_overrideSortingOrder)
                    foreach (var sprite in _worn.GetComponentsInChildren<SpriteRenderer>())
                        sprite.sortingOrder = _sortingOrder;
                return _worn;
            }

            /// <summary>걸친 걸 벗어 없앤다.</summary>
            public void TakeOff()
            {
                if (_worn == null)
                {
                    return;
                }

                UnityEngine.Object.Destroy(_worn);
                _worn = null;
                foreach (var sprite in _coveredSprites)
                    if (sprite != null) sprite.enabled = true;
            }
        }

        /// <summary>인스펙터에서 물려주는 자리 목록. 몸에 본이 없는 자리는 아예 없다 — 그 자리 장비는 스탯만 얹는다.</summary>
        [SerializeField] private SlotEntry[] _slots;

        /// <summary>타격 시점 뒤에 궤적이 이어지는 시간. 휘두른 뒤의 잔상이다.</summary>
        private const float TrailFollowThrough = 0.15f;

        private Dictionary<EquipmentSlot, List<SlotEntry>> _table;
        private Character _character;
        private WeaponTrail _weaponTrail;

        /// <summary>Init 이 끝난 캐릭터가 불러준다. 이 몸이 가진 자리만 따라간다.</summary>
        public void Bind(Character character)
        {
            _character = character;

            foreach (var slot in Entries().Keys)
            {
                character.Equipment.Observe(slot)
                    .Subscribe((self: this, slot), (part, state) => state.self.Apply(state.slot, part))
                    .AddTo(this);
            }

            character.Combat.Attacked
                .Subscribe(this, (attack, self) => self._weaponTrail?.Play(attack is CharacterSkill skill
                    ? skill.MotionSeconds * .8f : attack.HitTime + TrailFollowThrough))
                .AddTo(this);
        }

        /// <summary>그 자리에 이 그림을 걸친다. 에디터 프리뷰 툴도 이걸 쓴다.</summary>
        public void Wear(EquipmentSlot slot, GameObject visual)
        {
            if (!Entries().TryGetValue(slot, out var entries)) return;
            foreach (var entry in entries) entry.Wear(visual);
        }

        public void TakeOff(EquipmentSlot slot)
        {
            if (!Entries().TryGetValue(slot, out var entries)) return;
            foreach (var entry in entries) entry.TakeOff();
        }

        private void Apply(EquipmentSlot slot, EquipmentPart part)
        {
            if (slot == EquipmentSlot.MainHand)
            {
                _weaponTrail = null;
            }

            // 벗은 자리, 그리고 맨손처럼 그림이 없는 파츠는 벗기만 한다.
            if (part?.Visual == null)
            {
                TakeOff(slot);
                return;
            }

            if (!Entries().TryGetValue(slot, out var entries)) return;
            foreach (var entry in entries)
            {
                var worn = entry.Wear(part.Visual);
                if (part is Weapon weapon) SetupTrail(worn, weapon);
            }
        }

        /// <summary>트레일을 쓸지는 입힐 때 한 번만 정한다 — 무기가 허락하고 주인이 영웅일 때. 꺼진 트레일은 없는 것과 같다.</summary>
        private void SetupTrail(GameObject worn, Weapon weapon)
        {
            _weaponTrail = worn.GetComponentInChildren<WeaponTrail>(true);

            if (_weaponTrail != null)
            {
                _weaponTrail.gameObject.SetActive(weapon.UseTrail && _character is Hero);
            }
        }

        /// <summary>처음 쓸 때 한 번만 묶는다. 에디터에서도 Awake 없이 쓰이므로 여기서 만든다.</summary>
        private Dictionary<EquipmentSlot, List<SlotEntry>> Entries()
        {
            if (_table == null)
            {
                _table = new Dictionary<EquipmentSlot, List<SlotEntry>>();

                foreach (var entry in _slots)
                {
                    if (!_table.TryGetValue(entry.Slot, out var entries))
                        _table[entry.Slot] = entries = new List<SlotEntry>();
                    entries.Add(entry);
                }
            }

            return _table;
        }
    }
}
