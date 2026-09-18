using System;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// <see cref="Stat"/> 을 모아 둔 자료 구조. 그 이상의 뜻은 없다 —
    /// 캐릭터의 능력치를 가리키는 타입이 아니라, 스탯을 묶어 들고 더할 수 있는 물건이면 무엇이든 쓰는 그릇이다.
    /// 히어로의 타고난 몸, 적의 몸, 무기 하나가 얹는 몫, 성장 한 항목의 몫이 전부 같은 이 꼴이고,
    /// 서로 다른 근원이라도 <see cref="Add"/> 하나로 합쳐진다 — 무엇의 묶음인지는 이걸 든 쪽이 안다.
    ///
    /// 설계값 에셋이 인스펙터에서 그대로 들고 적는다. 적은 줄만 들고, 적지 않은 종류는 0, 같은 종류가 두 줄이면 더해진다.
    /// 종류마다 필드를 두지 않고 목록 하나만 들기 때문에 StatType 에 항목이 늘어도 여기는 고칠 것이 없다.
    /// 꺼내는 길은 <see cref="Get"/> 하나뿐이다.
    /// </summary>
    [Serializable]
    public struct StatGroup
    {
        /// <summary>적은 줄 그대로. default(StatGroup) 는 이게 null 이고, 그건 "전부 0" 을 뜻한다.</summary>
        [SerializeField] private Stat[] _stats;

        /// <summary>스탯을 줄줄이 적어 만든다 — new StatGroup(new Stat(StatType.AttackPower, 14), new Stat(StatType.MaxHP, 10)).</summary>
        public StatGroup(params Stat[] stats)
        {
            _stats = stats;
        }

        /// <summary>종류로 수치를 꺼낸다. 이 묶음에서 값을 얻는 유일한 길이다.</summary>
        public float Get(StatType type)
        {
            if (_stats == null)
            {
                return 0f;
            }

            var sum = 0f;

            foreach (var stat in _stats)
            {
                if (stat.Type == type)
                {
                    sum += stat.Value;
                }
            }

            return sum;
        }

        /// <summary>두 묶음의 합. 최종 스탯 = 기본 + 성장 + 장비 합성이 이걸 쓴다.</summary>
        public StatGroup Add(StatGroup other)
        {
            var sum = new Stat[StatTypes.All.Length];

            for (var i = 0; i < sum.Length; i++)
            {
                var type = StatTypes.All[i];
                sum[i] = new Stat(type, Get(type) + other.Get(type));
            }

            return new StatGroup(sum);
        }
    }
}
