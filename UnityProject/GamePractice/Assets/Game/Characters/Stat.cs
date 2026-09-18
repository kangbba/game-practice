using System;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 스탯 한 건 — 어느 종류를 얼마. 그 이상의 뜻은 없는 밑바닥 자료다.
    ///
    /// 캐릭터의 능력을 뜻하지 않는다. 누구의 것인지, 무엇이 얹는 몫인지는 이걸 든 쪽이 정한다 —
    /// 히어로의 타고난 몸도, 무기가 얹는 위력도, 성장 한 레벨의 몫도 전부 같은 이 줄이다.
    /// 그래서 스탯을 선언하고 싶은 것은 무엇이든(장비·소모품·버프·세트 효과) 필드를 새로 만들지 않고 이 줄을 늘어놓으면 된다.
    ///
    /// 종류마다 필드를 두지 않으므로 StatType 에 항목이 늘어도 이 줄을 쓰는 쪽은 고칠 것이 없다.
    /// </summary>
    [Serializable]
    public struct Stat
    {
        [SerializeField] private StatType _type;
        [SerializeField] private float _value;

        public Stat(StatType type, float value)
        {
            _type = type;
            _value = value;
        }

        public StatType Type => _type;

        public float Value => _value;
    }
}
