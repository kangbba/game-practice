using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 게임 내내 쓰는 에셋 — 영웅 프리팹·프로필(창의 인형과 초상화도 쓴다), 장비, 공용 UI.
    /// 게임을 켤 때 로드하고 게임이 꺼질 때 놓는다. 전투에서만 쓰는 건 BattleAssets 의 것이다.
    /// </summary>
    public sealed class GameAssets : AssetGroup
    {
        public IAssets<Hero> Heroes { get; }
        public IAssets<CharacterProfile> Profiles { get; }
        public IAssets<GameObject> EquipmentVisuals { get; }
        public IAssets<EquipmentPlan> EquipmentPlans { get; }
        public UIAssetManager UI { get; }

        private GameAssets()
        {
            Heroes = Add(new HeroAssetManager());
            Profiles = Add(new CharacterProfileAssetManager());
            EquipmentVisuals = Add(new EquipmentAssetManager());
            EquipmentPlans = Add(new EquipmentPlanAssetManager());
            UI = Add(new UIAssetManager());
        }

        /// <summary>로드를 마친 묶음을 돌려준다. 이 밖에서 GameAssets 를 얻는 길은 없다.</summary>
        public static async UniTask<GameAssets> LoadAsync(Action<float> onProgress, CancellationToken token)
        {
            var assets = new GameAssets();
            await assets.LoadAllAsync(onProgress, token);
            return assets;
        }
    }
}
