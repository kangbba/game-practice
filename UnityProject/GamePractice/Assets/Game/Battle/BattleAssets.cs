using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 전투에서만 쓰는 에셋 — 맵, 적, 영웅 설계값, 파티클, 드랍 아이템, 전투 UI.
    /// 전투에 들어가기 전에 로드하고, 전투가 끝나면(InGamePhase 가 나갈 때) 놓는다.
    /// BattleScope 는 이걸 받아야만 만들어진다 — 로드 전에 전투를 여는 코드는 컴파일되지 않는다.
    /// </summary>
    public sealed class BattleAssets : AssetGroup
    {
        private readonly MapAssetManager _maps;

        public GameObject MainMap => _maps.MainMap;
        public IAssets<HeroData> HeroData { get; }
        public IAssets<Enemy> Enemies { get; }
        public IAssets<EnemyData> EnemyData { get; }
        public IAssets<GameObject> Particles { get; }
        public ItemAssetManager Items { get; }
        public BattleUIAssetManager UI { get; }

        private BattleAssets()
        {
            _maps = Add(new MapAssetManager());
            HeroData = Add(new HeroDataAssetManager());
            Enemies = Add(new EnemyAssetManager());
            EnemyData = Add(new EnemyDataAssetManager());
            Particles = Add(new ParticleAssetManager());
            Items = Add(new ItemAssetManager());
            UI = Add(new BattleUIAssetManager());
        }

        /// <summary>로드를 마친 묶음을 돌려준다. 이 밖에서 BattleAssets 를 얻는 길은 없다.</summary>
        public static async UniTask<BattleAssets> LoadAsync(Action<float> onProgress, CancellationToken token)
        {
            var assets = new BattleAssets();
            await assets.LoadAllAsync(onProgress, token);
            return assets;
        }
    }
}
