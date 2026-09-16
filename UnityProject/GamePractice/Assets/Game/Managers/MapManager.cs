using UnityEngine;

namespace Sayne
{
    /// <summary>현재 떠 있는 맵 인스턴스를 관리한다. 에셋 보관은 MapAssetManager 의 몫.</summary>
    public class MapManager : ManagerBase
    {
        private readonly IAssets<GameObject> _mapAssets;

        private GameObject _currentMap;

        public MapManager(IAssets<GameObject> mapAssets)
        {
            _mapAssets = mapAssets;
        }

        protected override void OnInit()
        {
        }

        protected override void OnRelease()
        {
            ClearMap();
        }

        public GameObject CreateMap(string mapID)
        {
            ClearMap();

            var prefab = _mapAssets.Get(mapID);
            if (prefab == null)
            {
                return null;
            }

            _currentMap = Object.Instantiate(prefab);
            _currentMap.transform.position = Vector3.zero;
            return _currentMap;
        }

        public void ClearMap()
        {
            if (_currentMap != null)
            {
                Object.Destroy(_currentMap);
                _currentMap = null;
            }
        }
    }
}
