using System;
using System.Collections.Generic;
using R3;

namespace Sayne
{
    /// <summary>
    /// 영웅들이 함께 쓰는 장비 가방 하나. 누가 주웠든 여기 쌓이고, 누구든 여기서 꺼내 입는다 — 장비창의 후보가 여기서 나온다.
    /// 영웅이 죽었다 살아나도 가방은 그대로다. 무엇을 입고 있는지는 CharacterEquipment 가 따로 안다 — 가방은 소유, 장비는 착용이다.
    /// </summary>
    public class PartyInventory : IDisposable
    {
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();
        private readonly Subject<Unit> _changed = new Subject<Unit>();

        /// <summary>가방 내용이 바뀌었다. 장비창이 이걸 보고 후보를 다시 그린다.</summary>
        public Observable<Unit> Changed => _changed;

        public bool Contains(string equipmentID)
        {
            return _counts.ContainsKey(equipmentID);
        }

        public int GetCount(string equipmentID)
        {
            return _counts.TryGetValue(equipmentID, out var count) ? count : 0;
        }

        public void Add(string equipmentID, int count = 1)
        {
            if (string.IsNullOrEmpty(equipmentID) || count <= 0)
            {
                return;
            }

            _counts[equipmentID] = GetCount(equipmentID) + count;
            _changed.OnNext(Unit.Default);
        }

        public void Remove(string equipmentID, int count = 1)
        {
            var left = GetCount(equipmentID) - count;
            if (left > 0)
            {
                _counts[equipmentID] = left;
            }
            else if (!_counts.Remove(equipmentID))
            {
                return;
            }

            _changed.OnNext(Unit.Default);
        }

        public void Dispose()
        {
            _counts.Clear();
            _changed.Dispose();
        }
    }
}
