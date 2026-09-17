using System.Collections.Generic;

namespace Sayne
{
    /// <summary>
    /// 무기 한 자루의 정보. 계열·싸움 방식(사거리·간격)과, 휘둘러 맞혔을 때 상대가 받는 효과를 가진다.
    /// </summary>
    public class WeaponInfo
    {
        public WeaponType Type { get; }

        /// <summary>이 무기가 얹는 공격력 선언값. 장비 스탯(EquipmentPart.Stats)으로 흘러가 최종 공격력에 합산된다.</summary>
        public int Power { get; }

        public float Range { get; }

        /// <summary>묶음 안에서 다음 타까지의 간격. 짧아야 3콤보가 이어진 느낌이 난다.</summary>
        public float ComboInterval { get; }

        /// <summary>묶음(평타 3타 + 고유스킬)을 마친 뒤 다음 묶음까지의 간격.</summary>
        public float CycleInterval { get; }

        /// <summary>이 무기에 맞은 쪽에 거는 상태이상들. 없으면 빈 목록이다.</summary>
        public IReadOnlyList<Debuff> HitDebuffs { get; }

        /// <summary>휘두를 때 프리팹 속 트레일을 쓸지. 트레일이 안 어울리는 무기는 끈다.</summary>
        public bool UseTrail { get; }

        /// <summary>원거리류가 쏘는 투사체 프리팹. 화살·총알처럼 무기마다 다른 걸 쏠 수 있게 무기가 들고 있다. 원거리류가 아니면 안 쓴다.</summary>
        public Projectile Projectile { get; }

        /// <summary>
        /// 쏘는 무기인가. 계열이 정한다 — 원거리류면 때리는 순간 투사체를 쏘고, 판정은 투사체가 닿을 때로 밀린다.
        /// 사거리가 길다고 원거리가 아니다.
        /// </summary>
        public bool IsRanged => Type == WeaponType.Ranged;

        public WeaponInfo(WeaponType type, int power, float range, float comboInterval, float cycleInterval,
            bool useTrail = false, Projectile projectile = null, params Debuff[] hitDebuffs)
        {
            Type = type;
            Power = power;
            Range = range;
            ComboInterval = comboInterval;
            CycleInterval = cycleInterval;
            UseTrail = useTrail;
            Projectile = projectile;
            HitDebuffs = hitDebuffs;
        }
    }
}
