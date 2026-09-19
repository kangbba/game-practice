using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Object = UnityEngine.Object;

namespace Sayne
{
    /// <summary>
    /// UI 입력의 주인 — EventSystem 하나. 로딩 화면의 시작 탭부터 전투 HUD·팝업까지 모든 UI 입력이 이걸 거친다.
    /// 그래서 무엇보다 먼저, 로딩 화면보다도 먼저 선다. 씬에 이미 놓여 있으면 그걸 쓰고 건드리지 않는다.
    /// </summary>
    public class UIInputManager : ManagerBase
    {
        private const string EventSystemName = "EventSystem";

        /// <summary>여기서 만든 것만 치운다. 씬에 원래 있던 건 씬의 것이다.</summary>
        private GameObject _created;

        protected override void OnInit()
        {
            if (EventSystem.current == null)
            {
                _created = new GameObject(EventSystemName, typeof(EventSystem), typeof(InputSystemUIInputModule));
            }
        }

        protected override void OnRelease()
        {
            // 씬이 먼저 내려가면 이미 없다.
            if (_created != null)
            {
                Object.Destroy(_created);
            }

            _created = null;
        }
    }
}
