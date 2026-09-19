using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Sayne
{
    /// <summary>
    /// 같은 수명으로 함께 로드되고 함께 놓이는 에셋 매니저 묶음. 하위 클래스는 생성자에서 Add 로 로더를 모으고,
    /// 생성자를 감춘 채 "로드를 마친 묶음"만 돌려주는 정적 LoadAsync 를 연다 —
    /// 그래서 밖에서는 로드가 끝난 묶음만 손에 쥘 수 있고, 로드 전에 에셋을 꺼내는 코드는 컴파일되지 않는다.
    /// </summary>
    public abstract class AssetGroup
    {
        private readonly List<ManagerBase> _managers = new List<ManagerBase>();
        private readonly List<ILoadable> _loadables = new List<ILoadable>();

        protected T Add<T>(T loader) where T : ManagerBase, ILoadable
        {
            loader.Init();
            _managers.Add(loader);
            _loadables.Add(loader);
            return loader;
        }

        /// <summary>
        /// 전부 한꺼번에 돌리고, 끝날 때까지 매 프레임 진행도 평균을 알린다.
        /// 도중에 취소되거나 터지면 이미 연 핸들까지 놓고 그대로 올려 보낸다 — 반쯤 로드된 묶음은 밖으로 나가지 않는다.
        /// </summary>
        protected async UniTask LoadAllAsync(Action<float> onProgress, CancellationToken token)
        {
            try
            {
                var loads = new List<UniTask>(_loadables.Count);
                foreach (var loadable in _loadables)
                {
                    loads.Add(loadable.LoadAsync());
                }

                var loading = UniTask.WhenAll(loads).Preserve();

                while (!loading.Status.IsCompleted())
                {
                    onProgress(AverageProgress());
                    await UniTask.Yield(token);
                }

                await loading;
                onProgress(1f);
            }
            catch
            {
                Release();
                throw;
            }
        }

        /// <summary>로더를 연 역순으로 놓는다. 에셋 핸들이 전부 반납된다.</summary>
        public void Release()
        {
            for (var i = _managers.Count - 1; i >= 0; i--)
            {
                _managers[i].Release();
            }

            _managers.Clear();
            _loadables.Clear();
        }

        private float AverageProgress()
        {
            var sum = 0f;
            foreach (var loadable in _loadables)
            {
                sum += loadable.Progress;
            }

            return sum / _loadables.Count;
        }
    }
}
