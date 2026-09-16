using System;
using System.Collections.Generic;

namespace Sayne
{
    /// <summary>웨이브에 나오는 적 한 줄. 능력치·장비는 적 자신이 가진다.</summary>
    public readonly struct WaveSpawnEntry
    {
        public readonly string EnemyID;
        public readonly int Count;

        public WaveSpawnEntry(string enemyID, int count)
        {
            EnemyID = enemyID;
            Count = count;
        }
    }

    /// <summary>웨이브별 적 구성 선언 테이블. 정의되지 않은 웨이브는 가장 가까운 아래 웨이브를 재사용한다.</summary>
    public static class WavePlans
    {
        private static readonly Dictionary<int, WaveSpawnEntry[]> Plans = new Dictionary<int, WaveSpawnEntry[]>
        {
            [1] = new[]
            {
                new WaveSpawnEntry(EnemyID.Goblin, 5),
            },
            [2] = new[]
            {
                new WaveSpawnEntry(EnemyID.Goblin, 8),
            },
            [3] = new[]
            {
                new WaveSpawnEntry(EnemyID.Goblin, 6),
                new WaveSpawnEntry(EnemyID.Ogre, 2),
            },
        };

        public static IReadOnlyList<WaveSpawnEntry> Get(int wave)
        {
            for (; wave > 0; wave--)
            {
                if (Plans.TryGetValue(wave, out var plan))
                {
                    return plan;
                }
            }

            return Array.Empty<WaveSpawnEntry>();
        }
    }
}
