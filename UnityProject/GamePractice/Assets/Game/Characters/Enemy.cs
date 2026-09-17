using UnityEngine.Rendering;

namespace Sayne
{
    /// <summary>싸우러 오는 캐릭터. 실제 적은 이걸 상속해 자기 ID 와 전투를 밝힌다.</summary>
    public abstract class Enemy : Character
    {
        /// <summary>자기 설계값. 태어난 근거이자, 죽으면 뭘 흘릴 운명인지도 여기 적혀 있다.</summary>
        public EnemyPlan Plan { get; private set; }
        SortingGroup _sortingGroup;

        private void Awake()
        {
            if(_sortingGroup == null)
            {
                _sortingGroup = gameObject.AddComponent<SortingGroup>();
            }
            _sortingGroup.sortingLayerName = "Enemy";
        }
        /// <summary>스폰 직후 한 번. 몸을 만드는 Init 보다 먼저 받아 둔다.</summary>
        public void SetPlan(EnemyPlan plan)
        {
            Plan = plan;
        }
    }
}
