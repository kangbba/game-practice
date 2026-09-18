using System;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 쿨타임 스킬 하나의 저장 형태. 설계값 에셋(HeroData·EnemyData)이 들고 있다가 CharacterSkill 로 굳힌다.
    /// CharacterSkill 은 애니메이션 해시를 미리 굽는 불변 객체라 인스펙터가 직접 만질 수 없다 — 그래서 만질 수 있는 값만 여기 둔다.
    /// 이름이 비어 있으면 스킬이 없다. 평타와 궁극기는 여기 없다 — 든 무기가 가진다.
    /// </summary>
    [Serializable]
    public class SkillData
    {
        [SerializeField] private string _name;
        [SerializeField] private float _powerMultiplier = 1.5f;
        [SerializeField] private float _cooldown = 5f;

        public bool IsEmpty => string.IsNullOrEmpty(_name);

        /// <summary>그 모션으로 트는 스킬. 모션은 든 무기 계열이 정하므로 받아서 쓴다.</summary>
        public CharacterSkill ToSkill(string animation)
        {
            return IsEmpty ? null : new CharacterSkill(_name, animation, _powerMultiplier, _cooldown);
        }
    }
}
