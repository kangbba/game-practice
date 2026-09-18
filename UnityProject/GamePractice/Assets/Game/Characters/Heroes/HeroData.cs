using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 히어로 하나의 설계값 전부. 몸이 얼마나 튼튼하고, 무슨 장비를 장착하고 태어나고, 어떻게 싸우는가.
    /// 주인은 파일명이 아니라 _heroID 필드다 — 같은 폴더에 프로필·프리팹이 같은 이름으로 이미 있기 때문이다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Hero Data", fileName = "HeroData")]
    public class HeroData : ScriptableObject
    {
        [HeroIDPicker] [SerializeField] private string _heroID;

        /// <summary>기본 스탯. 무기 공격력은 여기 없고 장비 설계값이 얹는다.</summary>
        [Header("기본 스탯")]
        [SerializeField] private StatGroup _baseStats;

        [Header("시작 장비 세트")]
        [SerializeField] private EquipmentSetData _equipmentSet = new EquipmentSetData();

        [Header("스킬")]
        [SerializeField] private SkillData _skill = new SkillData();

        /// <summary>이 설계값의 주인.</summary>
        public string HeroID => _heroID;

        public StatGroup BaseStats => _baseStats;

        public EquipmentSetData EquipmentSet => _equipmentSet;

        /// <summary>쿨타임 스킬. 히어로는 전부 가진다. 모션은 든 무기 계열이 정한다. 평타·궁극기는 든 무기가 가진다.</summary>
        public SkillData Skill => _skill;
    }
}
