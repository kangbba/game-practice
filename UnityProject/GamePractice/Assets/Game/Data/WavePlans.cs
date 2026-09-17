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
        /// <summary>일반 웨이브 구성. 키는 스테이지를 무시하고 처음부터 센 통산 번호다(1-1=1, 2-1=10).</summary>
        private static readonly Dictionary<int, WaveSpawnEntry[]> Plans = new Dictionary<int, WaveSpawnEntry[]>
        {
            [1] = new[]
            {
                new WaveSpawnEntry(EnemyID.Goblin, 5),
                new WaveSpawnEntry(EnemyID.Ogre, 1),
            },
            [2] = new[]
            {
                new WaveSpawnEntry(EnemyID.Goblin, 8),
                new WaveSpawnEntry(EnemyID.Ogre, 2),
            },
            // 3웨이브부터 오우거가 몽둥이를 든다. 몸은 같고 장비만 다른 변종이다.
            [3] = new[]
            {
                new WaveSpawnEntry(EnemyID.Goblin, 6),
                new WaveSpawnEntry(EnemyID.ArmedOgre, 2),
            },
        };

        /// <summary>스테이지 보스 구성. 키는 스테이지 번호. 정의되지 않은 스테이지는 가장 가까운 아래 보스를 재사용한다.</summary>
        private static readonly Dictionary<int, WaveSpawnEntry[]> BossPlans = new Dictionary<int, WaveSpawnEntry[]>
        {
            [1] = new[]
            {
                new WaveSpawnEntry(EnemyID.OgreBoss, 1),
            },
        };

        public static IReadOnlyList<WaveSpawnEntry> Get(WaveNumber wave)
        {
            return wave.IsBoss ? GetNearest(BossPlans, wave.Stage) : GetNearest(Plans, wave.NormalCount);
        }

        private static IReadOnlyList<WaveSpawnEntry> GetNearest(Dictionary<int, WaveSpawnEntry[]> table, int key)
        {
            for (; key > 0; key--)
            {
                if (table.TryGetValue(key, out var plan))
                {
                    return plan;
                }
            }

            return Array.Empty<WaveSpawnEntry>();
        }
    }
}
