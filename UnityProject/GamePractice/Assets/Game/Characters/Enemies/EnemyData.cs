using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 적 하나의 설계값 전부. 얼마나 튼튼하고, 뭘 들고 나오고, 죽으면 뭘 흘리는가.
    /// 주인은 파일명이 아니라 _enemyID 필드다 — 같은 폴더에 프로필 에셋이 같은 이름으로 이미 있기 때문이다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Enemy Data", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [EnemyIDPicker] [SerializeField] private string _enemyID;

        /// <summary>보스인가. 화면이 보스를 따로 다룬다 — HP 바가 영웅처럼 머리 위 UI 로 뜬다.</summary>
        [SerializeField] private bool _isBoss;

        /// <summary>기본 스탯. 무기 공격력은 여기 없고 장비 설계값이 얹는다.</summary>
        [Header("기본 스탯")]
        [SerializeField] private StatGroup _baseStats;

        /// <summary>비워 두면 자기 ID 의 프리팹을 쓴다. 같은 몸에 다른 장비를 입힌 변종은 여기만 채운다.</summary>
        [EnemyIDPicker(allowEmpty: true)] [SerializeField] private string _prefabID;

        [Header("시작 장비 세트")]
        [SerializeField] private EquipmentSetData _equipmentSet = new EquipmentSetData();

        [Header("스킬")]
        [SerializeField] private SkillData _skill = new SkillData();

        [Header("죽으면 주는 것")]
        [SerializeField] private int _expReward;
        [SerializeField] private DropItemData[] _drops = Array.Empty<DropItemData>();

        /// <summary>이 설계값의 주인.</summary>
        public string EnemyID => _enemyID;

        public bool IsBoss => _isBoss;

        public StatGroup BaseStats => _baseStats;

        /// <summary>몸으로 쓸 프리팹의 ID. 따로 정하지 않았으면 자기 자신이다.</summary>
        public string PrefabID => string.IsNullOrEmpty(_prefabID) ? _enemyID : _prefabID;

        public EquipmentSetData EquipmentSet => _equipmentSet;

        /// <summary>쿨타임 스킬. 적은 보통 비어 있다. 평타·궁극기는 든 무기가 가진다.</summary>
        public SkillData Skill => _skill;

        /// <summary>죽을 때 주는 경험치. 드랍과 달리 확률 없이 언제나 준다.</summary>
        public int ExpReward => _expReward;

        /// <summary>죽는 순간 한 번 굴린다. 당첨된 줄들이 나온다 — 아무것도 안 나올 수도 있다.</summary>
        public IEnumerable<DropItemData> Roll()
        {
            foreach (var entry in _drops)
            {
                if (entry.IsValid && UnityEngine.Random.value < entry.Chance)
                {
                    yield return entry;
                }
            }
        }
    }
}
