using UnityEngine;

namespace Sayne
{
    /// <summary>현재 떠 있는 맵 인스턴스를 관리한다. 어느 프리팹인지는 받아서 알고, 에셋 보관은 MapAssetManager 의 몫.</summary>
    public class MapManager : ManagerBase
    {
        private readonly GameObject _mainMap;

        private GameObject _currentMap;

        public MapManager(GameObject mainMap)
        {
            _mainMap = mainMap;
        }

        protected override void OnInit()
        {
        }

        protected override void OnRelease()
        {
            ClearMap();
        }

        public GameObject CreateMainMap()
        {
            ClearMap();

            _currentMap = Object.Instantiate(_mainMap);
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
