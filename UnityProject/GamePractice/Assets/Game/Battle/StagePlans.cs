using System.Collections.Generic;
using UnityEngine;

namespace Sayne
{
    /// <summary>스테이지 선언 목록. 첫 줄이 1스테이지다. 여기 없는 스테이지는 아직 안 만든 것이다.</summary>
    public static class StagePlans
    {
        private static readonly List<StagePlan> All = new List<StagePlan>
        {
            new StagePlan(new[]
                {
                    new Dictionary<string, int>
                    {
                        [EnemyID.Goblin] = 5,
                        [EnemyID.Ogre] = 1,
                    },
                    new Dictionary<string, int>
                    {
                        [EnemyID.Goblin] = 8,
                        [EnemyID.Ogre] = 2,
                    },
                    // 3웨이브부터 오우거가 몽둥이를 든다. 몸은 같고 장비만 다른 변종이다.
                    new Dictionary<string, int>
                    {
                        [EnemyID.Goblin] = 6,
                        [EnemyID.ArmedOgre] = 2,
                    },
                    // 마지막 웨이브가 보스 판이다.
                    new Dictionary<string, int>
                    {
                        [EnemyID.OgreBoss] = 1,
                    },
                }),
        };

        /// <summary>아직 안 만든 스테이지면 마지막으로 만든 걸 그대로 쓴다. 조용히 넘어가지 않게 로그를 남긴다.</summary>
        public static StagePlan Get(int stage)
        {
            if (stage <= All.Count)
            {
                return All[stage - 1];
            }

            Debug.Log($"{stage}스테이지는 아직 없다. 마지막으로 만든 {All.Count}스테이지를 그대로 쓴다");

            return All[All.Count - 1];
        }
    }
}
