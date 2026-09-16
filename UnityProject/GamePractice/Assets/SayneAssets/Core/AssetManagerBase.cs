using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Sayne
{
    /// <summary>로드가 필요한 매니저. GameManager 가 모아서 한꺼번에 LoadAsync 를 돌린다.</summary>
    public interface ILoadable
    {
        UniTask LoadAsync();
    }

    /// <summary>
    /// 에셋을 ID로 검색하는 기능만 노출하는 인터페이스.
    /// 에셋 매니저를 통째로 넘기면 Init/Release 같은 수명 관리까지 노출되므로, 검색 기능만 분리해서 전달한다.
    /// </summary>
    public interface IAssets<out TAsset> where TAsset : UnityEngine.Object
    {
        IReadOnlyCollection<string> IDs { get; }
        bool Contains(string id);
        TAsset Get(string id);
    }

    /// <summary>
    /// 에셋 매니저 베이스: ID→에셋 테이블을 보관하고, 로드 헬퍼(어드레서블)와 핸들 수명을 책임진다.
    /// 하위 클래스는 OnLoadAsync 에서 로드→Register 만 하면 된다.
    /// 로드가 끝나기 전에 Get 을 부르면 예외로 바로 터진다.
    /// </summary>
    public abstract class AssetManagerBase<TAsset> : ManagerBase, IAssets<TAsset>, ILoadable
        where TAsset : UnityEngine.Object
    {
        private readonly Dictionary<string, TAsset> _assets = new Dictionary<string, TAsset>();
        private readonly List<AsyncOperationHandle> _handles = new List<AsyncOperationHandle>();

        private bool _isLoaded;

        public IReadOnlyCollection<string> IDs => _assets.Keys;

        protected bool IsLoaded => _isLoaded;

        protected override void OnInit()
        {
        }

        public async UniTask LoadAsync()
        {
            await OnLoadAsync();
            _isLoaded = true;
        }

        protected abstract UniTask OnLoadAsync();

        protected async UniTask<IList<T>> LoadAssetsByLabelAsync<T>(string label)
            where T : UnityEngine.Object
        {
            var handle = Addressables.LoadAssetsAsync<T>(label, null);
            _handles.Add(handle);
            return await handle.ToUniTask(cancellationToken: LifeToken);
        }

        protected async UniTask<T> LoadAssetByAddressAsync<T>(string address)
            where T : UnityEngine.Object
        {
            var handle = Addressables.LoadAssetAsync<T>(address);
            _handles.Add(handle);
            return await handle.ToUniTask(cancellationToken: LifeToken);
        }

        protected void Register(string id, TAsset asset)
        {
            _assets[id] = asset;
        }

        public bool Contains(string id)
        {
            return _assets.ContainsKey(id);
        }

        public TAsset Get(string id)
        {
            if (!_isLoaded)
            {
                throw new InvalidOperationException($"{GetType().Name}: LoadAsync 가 끝나기 전에 Get({id}) 을 불렀다");
            }

            if (!_assets.TryGetValue(id, out var asset))
            {
                throw new KeyNotFoundException($"{GetType().Name}: {id} 에셋이 등록되지 않았다");
            }

            return asset;
        }

        protected sealed override void OnRelease()
        {
            OnAssetRelease();
            _assets.Clear();
            _isLoaded = false;

            foreach (var handle in _handles)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            _handles.Clear();
        }

        /// <summary>테이블·핸들 반납 전에 하위 클래스가 정리할 것들.</summary>
        protected virtual void OnAssetRelease()
        {
        }
    }
}
