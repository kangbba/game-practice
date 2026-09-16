using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>캐릭터의 겉모습. 어느 부위가 어느 본인지 알고, 거기에 장비 그림을 걸치고 벗긴다.</summary>
    public class CharacterSkin : MonoBehaviour
    {
        /// <summary>부위와 그 부위의 본. 지금 걸친 것도 여기서 들고 있다.</summary>
        [Serializable]
        private class BodyPartEntry
        {
            [SerializeField] private BodyPart _bodyPart;
            [SerializeField] private Transform _transform;

            private GameObject _worn;

            public BodyPart BodyPart => _bodyPart;

            /// <summary>입던 걸 벗고 새로 걸친다.</summary>
            public void Wear(GameObject visual)
            {
                TakeOff();

                _worn = UnityEngine.Object.Instantiate(visual, _transform, false);
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
            }
        }

        /// <summary>인스펙터에서 물려주는 부위 목록. 유니티가 딕셔너리를 직렬화하지 못해 배열로 받는다.</summary>
        [SerializeField] private BodyPartEntry[] _bodyParts;

        private Dictionary<BodyPart, BodyPartEntry> _table;

        /// <summary>Init 이 끝난 캐릭터가 불러준다. 이 몸이 가진 부위만 따라간다.</summary>
        public void Bind(Character character)
        {
            foreach (var entry in _bodyParts)
            {
                var bodyPart = entry.BodyPart;

                character.Equipment.Observe(bodyPart)
                    .Subscribe((self: this, bodyPart), (part, state) => state.self.Apply(state.bodyPart, part))
                    .AddTo(this);
            }
        }

        /// <summary>그 부위에 이 그림을 걸친다. 에디터 프리뷰 툴도 이걸 쓴다.</summary>
        public void Wear(BodyPart bodyPart, GameObject visual)
        {
            EntryOf(bodyPart).Wear(visual);
        }

        public void TakeOff(BodyPart bodyPart)
        {
            EntryOf(bodyPart).TakeOff();
        }

        private void Apply(BodyPart bodyPart, EquipmentPart part)
        {
            // 벗은 부위, 그리고 맨손처럼 그림이 없는 파츠는 벗기만 한다.
            if (part?.Visual == null)
            {
                TakeOff(bodyPart);
                return;
            }

            Wear(bodyPart, part.Visual);
        }

        /// <summary>처음 쓸 때 한 번만 묶는다. 에디터에서도 Awake 없이 쓰이므로 여기서 만든다.</summary>
        private BodyPartEntry EntryOf(BodyPart bodyPart)
        {
            if (_table == null)
            {
                _table = new Dictionary<BodyPart, BodyPartEntry>();

                foreach (var entry in _bodyParts)
                {
                    _table[entry.BodyPart] = entry;
                }
            }

            return _table[bodyPart];
        }
    }
}
