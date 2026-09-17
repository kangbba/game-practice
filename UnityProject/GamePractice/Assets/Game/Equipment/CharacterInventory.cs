using System;
using System.Collections.Generic;
using R3;

namespace Sayne
{
    /// <summary>
    /// 캐릭터가 들고 다니는 장비 가방. 주운 것만 들어 있고, 장비창의 후보는 여기서 나온다.
    /// 무엇을 입고 있는지는 CharacterEquipment 가 따로 안다 — 가방은 소유, 장비는 착용이다.
    /// </summary>
    public class CharacterInventory : IDisposable
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
