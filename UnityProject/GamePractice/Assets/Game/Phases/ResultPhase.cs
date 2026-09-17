using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>웨이브 사이 정산 슬롯. 지금은 클리어 로그와 숨 고르기 딜레이만 둔다.</summary>
    public class ResultPhase : PhaseBase
    {
        private const float WaveInterval = 1.5f;

        private readonly WaveNumber _wave;

        public override string Key => PhaseID.Result;

        public ResultPhase(WaveNumber wave)
        {
            _wave = wave;
        }

        public override void Enter(CancellationToken token)
        {
            Debug.Log($"Wave {_wave.Label} clear");
        }

        public override UniTask MainLogicAsync(CancellationToken token)
        {
            return UniTask.Delay(TimeSpan.FromSeconds(WaveInterval), cancellationToken: token);
        }

        public override void Exit()
        {
        }
    }
}
