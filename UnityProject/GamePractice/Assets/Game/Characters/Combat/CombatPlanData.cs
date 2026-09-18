using System;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// "어떻게 싸우나"의 직렬화 껍데기. 설계값 에셋(HeroPlan·EnemyPlan)이 이걸 들고 있다가 CombatPlan 으로 굳힌다.
    /// 런타임 타입(CombatPlan·BasicAttack·CharacterSkill)은 애니메이션 해시를 미리 굽는 불변 객체라
    /// 인스펙터가 직접 만질 수 없다 — 그래서 만질 수 있는 값만 여기 모은다.
    /// </summary>
    [Serializable]
    public class CombatPlanData
    {
        /// <summary>쿨타임 기술 한 개. 이름이 비어 있으면 그 기술은 없다. 궁극기는 여기 없다 — 든 무기가 가진다.</summary>
        [Serializable]
        public class SkillData
        {
            [SerializeField] private string _name;
            [SerializeField] private float _powerMultiplier = 1.5f;
            [SerializeField] private float _cooldown = 5f;

            public bool IsEmpty => string.IsNullOrEmpty(_name);

            public CharacterSkill ToSkill(string animation)
            {
                return IsEmpty ? null : new CharacterSkill(_name, animation, _powerMultiplier, _cooldown);
            }
        }

        /// <summary>
        /// 맞은 쪽을 움찔하게 할지. 적의 평타는 안 움찔하게 둔다 —
        /// 계속 얻어맞는 동안 조작이 막히면 안 되기 때문이다.
        /// </summary>
        [SerializeField] private bool _comboStaggers = true;

        [SerializeField] private SkillData _skill = new SkillData();

        public CombatPlan ToPlan()
        {
            return new CombatPlan(_comboStaggers, _skill.ToSkill(CharacterAnimations.SkillName));
        }
    }
}
