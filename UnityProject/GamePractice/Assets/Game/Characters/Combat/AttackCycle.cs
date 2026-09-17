using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 평타 묶음의 몇 번째 타인지만 센다. 한동안 안 치면 1타로 돌아간다.
    /// 몇 타짜리인지는 낀 무기가 정하므로 물을 때마다 받아 쓴다 — 무기를 바꾸면 그 자리에서 따라간다.
    /// </summary>
    public class AttackCycle
    {
        private const float ComboWindow = 1.2f;

        private int _step;
        private float _expireTime;

        /// <summary>다음 타가 묶음의 마지막인가. 마지막 타 뒤에는 다음 묶음까지 길게 쉰다.</summary>
        public bool IsAtLast(int comboCount)
        {
            return StepIn(comboCount) == comboCount - 1;
        }

        /// <summary>다음에 나갈 타의 순번(0부터)을 꺼내고 한 칸 넘긴다.</summary>
        public int Draw(int comboCount)
        {
            var step = StepIn(comboCount);

            _step = (step + 1) % comboCount;
            _expireTime = Time.time + ComboWindow;

            return step;
        }

        /// <summary>묶음이 짧은 무기로 갈아끼면 순번이 넘칠 수 있다 — 그때는 1타부터 다시 센다.</summary>
        private int StepIn(int comboCount)
        {
            return Time.time <= _expireTime && _step < comboCount ? _step : 0;
        }
    }
}
