namespace Sayne
{
    /// <summary>
    /// <see cref="Stat"/> 을 종류별로 한 칸씩 모아 둔 자료 구조. 그 이상의 뜻은 없다 —
    /// 캐릭터의 능력치를 가리키는 타입이 아니라, 스탯을 묶어 들고 더할 수 있는 물건이면 무엇이든 쓰는 그릇이다.
    /// 히어로의 타고난 몸, 적의 몸, 무기 하나가 얹는 몫, 성장 한 항목의 몫이 전부 같은 이 꼴이고,
    /// 서로 다른 근원이라도 <see cref="Add"/> 하나로 합쳐진다 — 무엇의 묶음인지는 이걸 든 쪽이 안다.
    ///
    /// 종류마다 필드를 두지 않고 목록 하나만 들기 때문에 StatType 에 항목이 늘어도 여기는 고칠 것이 없다.
    /// 꺼내는 길은 <see cref="Get"/> 하나뿐이다.
    /// </summary>
    public readonly struct StatGroup
    {
        /// <summary>
        /// 칸 번호가 곧 StatType 값이다 — StatTypes.All 이 enum 순서 그대로라 (int)type 으로 바로 찾는다.
        /// default(StatGroup) 는 이게 null 이고, 그건 "전부 0" 을 뜻한다.
        /// </summary>
        private readonly Stat[] _stats;

        /// <summary>
        /// 스탯을 줄줄이 적어 만든다 — new StatGroup(new Stat(StatType.AttackPower, 14), new Stat(StatType.MaxHP, 10)).
        /// 설계값 에셋은 선언해 둔 배열을 그대로 넘긴다. 적지 않은 종류는 0, 같은 종류가 두 줄이면 더해진다.
        /// </summary>
        public StatGroup(params Stat[] stats)
        {
            _stats = new Stat[StatTypes.All.Length];

            for (var i = 0; i < _stats.Length; i++)
            {
                _stats[i] = new Stat(StatTypes.All[i], 0f);
            }

            if (stats == null)
            {
                return;
            }

            foreach (var stat in stats)
            {
                var index = (int)stat.Type;
                _stats[index] = new Stat(stat.Type, _stats[index].Value + stat.Value);
            }
        }

        /// <summary>종류로 수치를 꺼낸다. 이 묶음에서 값을 얻는 유일한 길이다.</summary>
        public float Get(StatType type)
        {
            return _stats == null ? 0f : _stats[(int)type].Value;
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
