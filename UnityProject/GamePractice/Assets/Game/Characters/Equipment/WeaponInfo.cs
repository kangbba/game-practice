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

        /// <summary>이 무기에 맞은 쪽이 받는 효과들. 없으면 빈 목록이다.</summary>
        public IReadOnlyList<HitEffect> HitEffects { get; }

        /// <summary>휘두를 때 프리팹 속 트레일을 쓸지. 트레일이 안 어울리는 무기는 끈다.</summary>
        public bool UseTrail { get; }

        public WeaponInfo(WeaponType type, int power, float range, float comboInterval, float cycleInterval,
            bool useTrail = false, params HitEffect[] hitEffects)
        {
            Type = type;
            Power = power;
            Range = range;
            ComboInterval = comboInterval;
            CycleInterval = cycleInterval;
            UseTrail = useTrail;
            HitEffects = hitEffects;
        }
    }
}
