using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>게임이 멈춘(timeScale 0) 동안에도 도는 0→1 진행. 튜토리얼 UI 연출 전용이라 밖으로 열지 않는다.</summary>
    internal static class UnscaledTween
    {
        public static async UniTask RunAsync(float seconds, Action<float> apply, CancellationToken token)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                apply(elapsed / seconds);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                elapsed += Time.unscaledDeltaTime;
            }

            apply(1f);
        }
    }
}
