using System;
using System.Collections.Generic;

namespace Sayne
{
    public readonly struct WaveSpawnEntry
    {
        public readonly string EnemyID;
        public readonly int Count;
        public readonly CharacterStats Stats;
        public readonly AttackProfile BareHandsAttack;

        /// <summary>스폰 시 장착할 무기 ID. 비어 있으면 맨손.</summary>
        public readonly string WeaponID;

        public WaveSpawnEntry(string enemyID, int count, CharacterStats stats, AttackProfile bareHandsAttack,
            string weaponID = "")
        {
            EnemyID = enemyID;
            Count = count;
            Stats = stats;
            BareHandsAttack = bareHandsAttack;
            WeaponID = weaponID;
        }
    }

    /// <summary>웨이브별 적 구성 선언 테이블. 정의되지 않은 웨이브는 가장 가까운 아래 웨이브를 재사용한다.</summary>
    public static class WavePlans
    {
        private static readonly CharacterStats GoblinBody = new CharacterStats(maxHP: 50, moveSpeed: 1.5f);
        private static readonly AttackProfile GoblinAttack = new AttackProfile(power: 5, range: 2.5f, interval: 1f);

        private static readonly CharacterStats OgreBody = new CharacterStats(maxHP: 120, moveSpeed: 1.2f);
        private static readonly AttackProfile OgreAttack = new AttackProfile(power: 12, range: 2.5f, interval: 1.5f);

        private static readonly Dictionary<int, WaveSpawnEntry[]> Plans = new Dictionary<int, WaveSpawnEntry[]>
        {
            [1] = new[]
            {
                new WaveSpawnEntry(EnemyID.Goblin, 5, GoblinBody, GoblinAttack),
            },
            [2] = new[]
            {
                new WaveSpawnEntry(EnemyID.Goblin, 8, GoblinBody, GoblinAttack),
            },
            [3] = new[]
            {
                new WaveSpawnEntry(EnemyID.Goblin, 6, GoblinBody, GoblinAttack),
                new WaveSpawnEntry(EnemyID.Ogre, 2, OgreBody, OgreAttack),
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
