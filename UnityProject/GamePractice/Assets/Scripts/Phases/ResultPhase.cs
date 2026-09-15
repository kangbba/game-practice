using System.Threading;
using Cysharp.Threading.Tasks;

namespace Sayne
{
    public class ResultPhase : PhaseBase
    {
        private readonly MapManager _mapManager;
        private readonly HeroManager _heroManager;

        public override string Key => PhaseID.Result;

        public ResultPhase(MapManager mapManager, HeroManager heroManager)
        {
            _mapManager = mapManager;
            _heroManager = heroManager;
        }

        public override void Enter(CancellationToken token)
        {
        }

        public override UniTask MainLogicAsync(CancellationToken token)
        {
            return UniTask.CompletedTask;
        }

        public override void Exit()
        {
            _heroManager.DespawnAll();
            _mapManager.ClearWorldMap();
        }
    }
}
