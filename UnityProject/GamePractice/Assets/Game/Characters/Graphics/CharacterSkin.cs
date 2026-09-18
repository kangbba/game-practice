using System;
using System.Collections.Generic;
using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.Rendering;

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
            /// <summary>입으면 가려지는 몸 그림. 투구를 쓰면 머리카락이 빠지는 식이다.</summary>
            [SerializeField] private SpriteRenderer[] _coveredSprites = Array.Empty<SpriteRenderer>();

            /// <summary>
            /// 이 자리가 리그의 어느 다리 본인가(Boots.FrontSide·RearSide). 한 켤레가 두 다리 자리에
            /// 각각 붙으므로, 자리마다 자기 쪽 그림만 남기라고 신발에게 알린다. 신발 자리가 아니면 비워 둔다.
            /// </summary>
            [SerializeField] private string _variant;
            [SerializeField] private bool _overrideSortingOrder;
            [SerializeField] private int _sortingOrder;

            private GameObject _worn;

            public EquipmentSlot Slot => _slot;

            /// <summary>입던 걸 벗고 새로 걸친다. 걸친 인스턴스를 준다.</summary>
            public GameObject Wear(GameObject visual)
            {
                TakeOff();

                _worn = UnityEngine.Object.Instantiate(visual, _transform, false);
                if (!string.IsNullOrEmpty(_variant))
                    _worn.GetComponent<Boots>().ShowSide(_variant);
                foreach (var sprite in _coveredSprites) sprite.enabled = false;
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
                foreach (var sprite in _coveredSprites) sprite.enabled = true;
            }
        }

        /// <summary>인스펙터에서 물려주는 자리 목록. 몸에 본이 없는 자리는 아예 없다 — 그 자리 장비는 스탯만 얹는다.</summary>
        [SerializeField] private SlotEntry[] _slots;

        /// <summary>타격 시점 뒤에 궤적이 이어지는 시간. 휘두른 뒤의 잔상이다.</summary>
        private const float TrailFollowThrough = 0.15f;

        /// <summary>머리 그림이 달린 본 이름. 영웅·적 모두 이 이름의 본을 가진다.</summary>
        private const string HeadBoneName = "Head";

        private Dictionary<EquipmentSlot, List<SlotEntry>> _table;
        private Character _character;
        private WeaponTrail _weaponTrail;

        /// <summary>손에 실제로 붙어 있는 무기. 맨손이면 null. 궁극기는 이 무기가 굽는다.</summary>
        public Weapon WornWeapon { get; private set; }

        /// <summary>몸·장비·궤적을 한 덩어리로 묶는다. 그림은 전부 이 아래에 있어서 레이어 한 칸으로 같이 움직인다.</summary>
        private SortingGroup _sortingGroup;

        private void Awake()
        {
            _sortingGroup = gameObject.AddComponent<SortingGroup>();
        }

        /// <summary>몸 전체를 투명하게 흐린다. 이 아래 그림은 장비까지 전부 같이 사라진다.</summary>
        public Tween FadeOut(float seconds)
        {
            var sequence = DOTween.Sequence();

            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
            {
                sequence.Join(renderer.DOFade(0f, seconds));
            }

            return sequence.SetLink(gameObject);
        }

        /// <summary>몸 전체가 그려질 정렬 레이어. 어느 레이어인지는 캐릭터가 정한다.</summary>
        public void SetSortingLayer(string layer)
        {
            _sortingGroup.sortingLayerName = layer;
        }

        /// <summary>
        /// 발에서 머리 그림 꼭대기까지의 키. 캐릭터마다 키와 배율이 달라 고정값을 못 쓴다.
        /// 그림이 카메라 쪽으로 누워 있어도 재는 건 그림 자신의 위쪽 기준이라 카메라를 몰라도 된다 —
        /// 기울어진 만큼은 보는 쪽에서 화면 위로 올리면 그만이다.
        /// </summary>
        public float GetHeight()
        {
            var height = 0f;

            foreach (var bone in GetComponentsInChildren<Transform>())
            {
                if (bone.name != HeadBoneName)
                {
                    continue;
                }

                foreach (var sprite in bone.GetComponentsInChildren<SpriteRenderer>())
                {
                    var bounds = sprite.sprite.bounds;
                    var corner = sprite.transform.TransformPoint(new Vector3(bounds.center.x, bounds.max.y, 0f));

                    // 그림의 위쪽으로 얼마나 높은가. 누운 각도는 이 축에 이미 들어 있다.
                    height = Mathf.Max(height, Vector3.Dot(sprite.transform.up, corner - transform.position));
                }
            }

            return height;
        }

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
                    ? self._character.GetMotionSeconds(skill.AnimationHash) * .8f : attack.HitTime + TrailFollowThrough))
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
                WornWeapon = null;
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
                if (part is WeaponPart) SetupWeapon(worn);
            }
        }

        /// <summary>
        /// 손에 붙은 무기를 쥐어 둔다. 궤적은 프리팹에 있으면 쓰되 영웅만 — 적까지 궤적을 그리면 화면이 어지럽다.
        /// 꺼진 트레일은 없는 것과 같다.
        /// </summary>
        private void SetupWeapon(GameObject worn)
        {
            WornWeapon = worn.GetComponent<Weapon>();
            _weaponTrail = worn.GetComponentInChildren<WeaponTrail>(true);

            if (_weaponTrail != null)
            {
                _weaponTrail.gameObject.SetActive(_character is Hero);
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
