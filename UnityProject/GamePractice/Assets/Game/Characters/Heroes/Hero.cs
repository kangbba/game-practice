using UnityEngine.Rendering;

namespace Sayne
{
    /// <summary>플레이어가 굴리는 캐릭터. 실제 히어로는 이걸 상속해 자기 ID 와 전투를 밝힌다.</summary>
    public abstract class Hero : Character
    {
        SortingGroup _sortingGroup;
        private void Awake()
        {
            if(_sortingGroup == null)
            {
                _sortingGroup = gameObject.AddComponent<SortingGroup>();
            }
            _sortingGroup.sortingLayerName = "Hero";
        }
    }
}
