using UnityEngine;

namespace Sayne
{
    /// <summary>여러 입력원 중 먼저 활성인 것을 따른다. 순서 = 우선순위.</summary>
    public class CompositeMoveSource : IMoveInputSource
    {
        private readonly IMoveInputSource[] _sources;

        public CompositeMoveSource(params IMoveInputSource[] sources)
        {
            _sources = sources;
        }

        public bool IsActive => Active != null;

        public Vector3 Direction => Active?.Direction ?? Vector3.zero;

        private IMoveInputSource Active
        {
            get
            {
                foreach (var source in _sources)
                {
                    if (source.IsActive)
                    {
                        return source;
                    }
                }

                return null;
            }
        }
    }
}
