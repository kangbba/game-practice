using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 장비 한 개의 아이템 카드. 어느 부위든 같은 모양이다 — ID, 장비창에 보일 이름·설명, 캐릭터에 얹는 스탯.
    /// 가방·장비창·드랍처럼 "아이템" 으로 다루는 쪽은 그림을 몰라도 이 카드만 보면 된다.
    ///
    /// 부위와 그 부위만의 것은 여기 없다 — 같은 이름의 장비 프리팹에 붙은 부위 스크립트(IEquipment)가 든다.
    /// 무기라면 쥐는 법·평타 방식·궁극기가 전부 Weapon 에 있다. 무기 위력은 여기 공격력 스탯이다.
    /// 주인은 파일명이 아니라 _equipmentID 필드다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Equipment Plan", fileName = "EquipmentPlan")]
    public class EquipmentPlan : ScriptableObject
    {
        [EquipmentIDPicker] [SerializeField] private string _equipmentID;

        [Header("장비창 표시")]
        [SerializeField] private string _displayName;
        [TextArea(2, 4)] [SerializeField] private string _description;

        /// <summary>얹는 스탯. 무기의 공격력이 곧 무기 위력이다 — 위력과 스탯을 두 벌로 적지 않는다.</summary>
        [Header("얹는 스탯")]
        [SerializeField] private Stat[] _stats;

        /// <summary>이 카드의 주인.</summary>
        public string EquipmentID => _equipmentID;

        /// <summary>장비창에 보여줄 이름. 안 적었으면 ID 를 그대로 보여준다.</summary>
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? _equipmentID : _displayName;

        public string Description => _description;

        /// <summary>이 장비가 얹는 스탯. 최종 스탯의 "장비" 근원으로 합류한다.</summary>
        public StatGroup Stats => new StatGroup(_stats);
    }
}
