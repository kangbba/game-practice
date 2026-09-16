using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    public class PreparePhase : PhaseBase
    {
        private const float CombatStartDelay = 1f;

        private readonly MapManager _mapManager;
        private readonly HeroManager _heroManager;

        public override string Key => PhaseID.Prepare;

        public PreparePhase(MapManager mapManager, HeroManager heroManager)
        {
            _mapManager = mapManager;
            _heroManager = heroManager;
        }

        public override void Enter(CancellationToken token)
        {
        }

        public override async UniTask MainLogicAsync(CancellationToken token)
        {
            _mapManager.CreateMap(MapID.World);
            _heroManager.SpawnHero(HeroID.Kage, Vector3.zero);

            await UniTask.Delay(TimeSpan.FromSeconds(CombatStartDelay), cancellationToken: token);
        }

        public override void Exit()
        {
        }
    }
}
