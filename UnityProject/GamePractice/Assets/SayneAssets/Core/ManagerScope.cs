using System.Collections.Generic;

namespace Sayne
{
    /// <summary>
    /// 같은 수명의 매니저들을 조립하고 함께 놓는 계층. 하위 클래스는 생성자에서 Add 로 매니저를 세운다.
    /// </summary>
    public abstract class ManagerScope
    {
        private readonly List<ManagerBase> _managers = new List<ManagerBase>();

        protected T Add<T>(T manager) where T : ManagerBase
        {
            manager.Init();
            _managers.Add(manager);
            return manager;
        }

        /// <summary>태어난 역순으로 놓는다 — 나중에 선 것이 먼저 선 것에 기대므로.</summary>
        public virtual void Release()
        {
            for (var i = _managers.Count - 1; i >= 0; i--)
            {
                _managers[i].Release();
            }

            _managers.Clear();
        }
    }
}
