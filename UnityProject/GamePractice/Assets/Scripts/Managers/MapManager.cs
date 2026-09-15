using UnityEngine;

namespace Sayne
{
    public class MapManager : ManagerBase
    {
        private const string WorldMapPrefabPath = "Maps/WorldMap";

        private GameObject _worldMapPrefab;
        private GameObject _currentWorldMap;

        protected override void OnInit()
        {
            _worldMapPrefab = Resources.Load<GameObject>(WorldMapPrefabPath);
            if (_worldMapPrefab == null)
            {
                Debug.LogError($"MapManager: {WorldMapPrefabPath} 로드 실패");
            }
        }

        protected override void OnRelease()
        {
            ClearWorldMap();
            _worldMapPrefab = null;
        }

        public GameObject CreateWorldMap()
        {
            ClearWorldMap();

            if (_worldMapPrefab == null)
            {
                return null;
            }

            _currentWorldMap = Object.Instantiate(_worldMapPrefab);
            _currentWorldMap.transform.position = Vector3.zero;
            return _currentWorldMap;
        }

        public void ClearWorldMap()
        {
            if (_currentWorldMap != null)
            {
                Object.Destroy(_currentWorldMap);
                _currentWorldMap = null;
            }
        }
    }
}
