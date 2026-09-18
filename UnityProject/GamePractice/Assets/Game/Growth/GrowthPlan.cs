using System;
using System.Collections.Generic;

namespace Sayne
{
    /// <summary>
    /// 성장 항목마다 "한 레벨이 얹는 몫"과 "다음 레벨 값"을 선언해 둔 표.
    /// 값은 레벨마다 CostGrowth 배로 오른다 — 여기 숫자만 고치면 성장 밸런스가 통째로 바뀐다.
    /// 순수 함수라 언제 어디서 계산해도 같다 — 저장할 것은 항목별 레벨뿐이다.
    /// </summary>
    public static class GrowthPlan
    {
        /// <summary>항목 하나의 설계값. 레벨 1 = 보너스 없음, 그 위로 한 레벨마다 Gain 이 얹힌다.</summary>
        private readonly struct Entry
        {
            public readonly int GainPerLevel;
            public readonly long BaseCost;
            public readonly float CostGrowth;
            public readonly int MaxLevel;

            public Entry(int gainPerLevel, long baseCost, float costGrowth, int maxLevel)
            {
                GainPerLevel = gainPerLevel;
                BaseCost = baseCost;
                CostGrowth = costGrowth;
                MaxLevel = maxLevel;
            }
        }

        /// <summary>값을 이 단위로 끊어 읽기 좋게 만든다 — 1173 골드 같은 수는 읽는 맛이 없다.</summary>
        private const long CostUnit = 10;

        private static readonly Dictionary<StatType, Entry> Entries = new Dictionary<StatType, Entry>
        {
            [StatType.AttackPower] = new Entry(gainPerLevel: 2, baseCost: 50, costGrowth: 1.18f, maxLevel: 99),
            [StatType.MaxHP] = new Entry(gainPerLevel: 10, baseCost: 40, costGrowth: 1.15f, maxLevel: 99),
        };

        /// <summary>
        /// 골드로 올릴 수 있는 항목만, 화면에 그리는 순서이자 저장 순서로. 스탯 전체(StatTypes.All)의 부분집합이다 —
        /// 이동속도처럼 장비로만 오르는 스탯은 여기 없다. 항목이 늘면 여기와 Entries 둘 다에 적는다.
        /// </summary>
        public static readonly StatType[] All = { StatType.AttackPower, StatType.MaxHP };

        public static int MaxLevel(StatType stat) => Entries[stat].MaxLevel;

        /// <summary>지금 레벨에서 다음 레벨로 올리는 값. 레벨이 오를수록 CostGrowth 배씩 비싸진다.</summary>
        public static long CostToUpgrade(StatType stat, int currentLevel)
        {
            var entry = Entries[stat];
            var raw = entry.BaseCost * Math.Pow(entry.CostGrowth, currentLevel - 1);
            var units = (long)Math.Round(raw / CostUnit, MidpointRounding.AwayFromZero);

            return Math.Max(units, 1) * CostUnit;
        }

        /// <summary>이 레벨이 얹는 보너스만 돌려준다. 레벨 1 = 보너스 없음. 최종 스탯 = 기본.Add(이것).</summary>
        public static StatGroup BonusFor(StatType stat, int level)
        {
            return new StatGroup(new Stat(stat, Entries[stat].GainPerLevel * (level - 1)));
        }
    }
}
