using System.Collections.Generic;

namespace Sayne
{
    /// <summary>스테이지 하나의 설계값. 웨이브를 순서대로 돌고, 마지막 웨이브가 곧 보스 판이다.</summary>
    public class StagePlan
    {
        /// <summary>순서대로 도는 웨이브. 한 장이 "적 ID 마다 몇 마리" 다. 마지막 장이 보스 판이다.</summary>
        public readonly IReadOnlyList<Dictionary<string, int>> Waves;

        public StagePlan(IReadOnlyList<Dictionary<string, int>> waves)
        {
            Waves = waves;
        }

        /// <summary>보스 판은 따로 있는 게 아니라 그 스테이지의 마지막 웨이브다.</summary>
        public bool IsBossWave(int waveNumber)
        {
            return waveNumber == Waves.Count;
        }

        /// <summary>그 번호의 웨이브에 나올 적 구성. 번호는 1 부터 센다.</summary>
        public IReadOnlyDictionary<string, int> GetEnemies(int waveNumber)
        {
            return Waves[waveNumber - 1];
        }
    }
}
