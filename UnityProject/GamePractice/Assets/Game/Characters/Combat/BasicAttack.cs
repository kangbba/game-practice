using UnityEngine;

namespace Sayne
{
    /// <summary>평타 한 타. 모션과 배수만 가진다 — 위력은 무기 공격력에 배수를 곱해서 나온다.</summary>
    public class BasicAttack
    {
        /// <summary>평타는 고유 이름이 없어서 순번으로 부른다 — 평타1, 평타2.</summary>
        public string Name { get; }

        public string Animation { get; }
        public int AnimationHash { get; }

        /// <summary>무기 공격력에 곱할 배수. 1이면 무기 공격력 그대로.</summary>
        public float PowerMultiplier { get; }

        /// <summary>휘두르기 시작해서 맞는 순간까지 걸리는 시간(초). 모션의 타격 프레임에 맞춘다.</summary>
        public float HitTime { get; }

        /// <summary>맞은 쪽이 움찔하는 시간(초). 상태이상이 아니라 타격 자체가 가진 기본 반응이다.</summary>
        public float StaggerSeconds { get; }

        public BasicAttack(string name, string animation, float powerMultiplier,
            float hitTime = 0.12f, float staggerSeconds = 0.2f)
        {
            Name = name;
            Animation = animation;
            AnimationHash = Animator.StringToHash(animation);
            PowerMultiplier = powerMultiplier;
            HitTime = hitTime;
            StaggerSeconds = staggerSeconds;
        }
    }
}
