using System.Collections.Generic;
using R3;

namespace Sayne
{
    public class PhaseUIManager : ManagerBase
    {
        private readonly PhaseManager _phaseManager;
        private readonly List<PhaseUIBase> _views = new List<PhaseUIBase>();

        public PhaseUIManager(PhaseManager phaseManager)
        {
            _phaseManager = phaseManager;
        }

        protected override void OnInit()
        {
            _phaseManager.CurrentPhase
                .Subscribe(ApplyPhase)
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            _views.Clear();
        }

        public void RegisterView(PhaseUIBase view)
        {
            _views.Add(view);
            ApplyPhase(_phaseManager.CurrentPhase.CurrentValue);
        }

        private void ApplyPhase(PhaseBase phase)
        {
            foreach (var view in _views)
            {
                view.SetVisible(phase != null && view.PhaseKey == phase.Key);
            }
        }
    }
}
