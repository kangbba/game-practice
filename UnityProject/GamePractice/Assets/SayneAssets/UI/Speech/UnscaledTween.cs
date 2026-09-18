using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>게임이 멈춘(timeScale 0) 동안에도 도는 0→1 진행.
    /// DOTween 은 SetUpdate(true) 로 같은 걸 하지만, 이 프로젝트의 DOTween 포크는 UniTask 브리지와
    /// 어셈블리 이름이 맞지 않아 await 가 안 된다 — 그래서 직접 굴린다. 이 폴더 안에서만 쓴다.</summary>
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
