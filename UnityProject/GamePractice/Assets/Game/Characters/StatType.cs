using System;

namespace Sayne
{
    /// <summary>
    /// 캐릭터가 가진 수치의 종류. 스탯 목록의 진실의 원천이다 —
    /// 스탯을 늘린다는 건 여기 한 줄을 더한다는 뜻이고, 그게 전부다.
    /// 구조체도 창도 빌더도 이 목록을 돌 뿐이라 고칠 것이 없다.
    /// </summary>
    public enum StatType
    {
        AttackPower,
        MaxHP,
        MoveSpeed,
        Luck
    }

    public static class StatTypes
    {
        /// <summary>
        /// 전부, enum 에 적은 순서 그대로. 손으로 다시 적지 않는다 — 목록이 두 벌이면 한쪽만 고치는 순간 어긋난다.
        /// 칸 번호가 곧 StatType 값이라 StatGroup 가 이걸 색인으로 쓴다.
        /// </summary>
        public static readonly StatType[] All = (StatType[])Enum.GetValues(typeof(StatType));

        public static string DisplayName(StatType type)
        {
            return type switch
            {
                StatType.AttackPower => "공격력",
                StatType.MaxHP => "체력",
                StatType.MoveSpeed => "이동속도",
                StatType.Luck => "행운",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}
