using System.Collections.Generic;
using R3;

namespace Sayne
{
    /// <summary>
    /// 파티의 주인 — 영웅들에 관한 플레이어 데이터. 누가 앞에 서는지(리더), 각자 무엇을 입고 있는지, 함께 쓰는 가방.
    /// 게임 내내 산다. 전투가 끝나 영웅 몸이 다 치워져도 이건 남는다 — 몸을 세우고 치우는 건 전투 쪽 HeroManager 가 하고,
    /// 그쪽이 여기를 보고 따라간다. 여기는 HeroManager 를 모른다.
    /// </summary>
    public class PartyManager : ManagerBase
    {
        private readonly EquipmentManager _equipmentManager;

        private readonly ReactiveProperty<string> _leader = new ReactiveProperty<string>(HeroID.Aldric);

        /// <summary>영웅별로 마지막에 입고 있던 장비 ID. 한 번도 안 나온 영웅은 없다 — 그때는 설계값의 시작 장비 세트를 입는다.</summary>
        private readonly Dictionary<string, List<string>> _wornEquipment = new Dictionary<string, List<string>>();

        /// <summary>영웅들이 함께 쓰는 가방. 주운 장비는 누가 주웠든 여기 쌓인다.</summary>
        public PartyInventory Inventory { get; } = new PartyInventory();

        /// <summary>편성 첫 칸의 영웅 ID. 바뀌면 전투 중인 HeroManager 가 그 자리에서 영웅을 갈아 세운다.</summary>
        public ReadOnlyReactiveProperty<string> Leader => _leader;

        public PartyManager(EquipmentManager equipmentManager)
        {
            _equipmentManager = equipmentManager;
        }

        protected override void OnInit()
        {
            // 지금은 있는 장비를 전부 들고 시작한다 — 세 영웅의 장비도, 활도 장비창에서 바로 바꿔 장착해 볼 수 있다.
            // 맨손만 뺀다. 맨손은 아이템이 아니라 "무기 없음" 이다.
            foreach (var equipmentID in _equipmentManager.IDs)
            {
                if (equipmentID != EquipmentID.Weapon.BareHands)
                {
                    Inventory.Add(equipmentID);
                }
            }
        }

        protected override void OnRelease()
        {
            Inventory.Dispose();
            _leader.Dispose();
            _wornEquipment.Clear();
        }

        public void SetLeader(string heroID)
        {
            _leader.Value = heroID;
        }

        /// <summary>그 영웅이 마지막에 입고 있던 장비. 한 번도 안 나왔으면 false.</summary>
        public bool TryGetWorn(string heroID, out List<string> equipmentIDs)
        {
            return _wornEquipment.TryGetValue(heroID, out equipmentIDs);
        }

        /// <summary>영웅 몸이 치워질 때 입고 있던 장비를 남긴다. 다음에 나올 때(부활·교체·다음 전투) 그대로 입고 나온다.</summary>
        public void SetWorn(string heroID, List<string> equipmentIDs)
        {
            _wornEquipment[heroID] = equipmentIDs;
        }
    }
}
