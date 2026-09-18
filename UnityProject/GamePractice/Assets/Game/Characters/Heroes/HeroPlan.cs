using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 히어로 하나의 설계값 전부. 몸이 얼마나 튼튼하고, 뭘 입고 나오고, 어떻게 싸우는가.
    /// 주인은 파일명이 아니라 _heroID 필드다 — 같은 폴더에 프로필·프리팹이 같은 이름으로 이미 있기 때문이다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Hero Plan", fileName = "HeroPlan")]
    public class HeroPlan : ScriptableObject
    {
        [HeroIDPicker] [SerializeField] private string _heroID;

        /// <summary>맨몸의 스탯. 무기 공격력은 여기 없고 장비 설계값이 얹는다.</summary>
        [Header("몸")]
        [SerializeField] private Stat[] _stats;

        [Header("입고 나오는 한 벌")]
        [EquipmentIDPicker(allowEmpty: true)] [SerializeField] private string _mainHand;
        [EquipmentIDPicker(allowEmpty: true)] [SerializeField] private string _offHand;
        [EquipmentIDPicker(allowEmpty: true)] [SerializeField] private string _helmet;
        [EquipmentIDPicker(allowEmpty: true)] [SerializeField] private string _chest;
        [EquipmentIDPicker(allowEmpty: true)] [SerializeField] private string _greaves;
        [EquipmentIDPicker(allowEmpty: true)] [SerializeField] private string _boots;

        [Header("싸우는 방식")]
        [SerializeField] private CombatPlanData _combat = new CombatPlanData();

        /// <summary>이 설계값의 주인.</summary>
        public string HeroID => _heroID;

        public StatGroup Body => new StatGroup(_stats);

        public EquipmentIDs Outfit => new EquipmentIDs(_mainHand, _offHand, _helmet, _chest, _greaves, _boots);

        /// <summary>평타 콤보와 기술. 히어로는 전부 스킬·궁극기를 가진다.</summary>
        public CombatPlan Combat => _combat.ToPlan();
    }
}
