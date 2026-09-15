namespace Sayne
{
    public class PhaseManager : ManagerBase
    {
        private PhaseBase _currentPhase;

        protected override void OnInit()
        {
        }

        protected override void OnRelease()
        {
            if (_currentPhase != null)
            {
                _currentPhase.OnExit();
                _currentPhase = null;
            }
        }

        public void ChangePhase(PhaseBase phase)
        {
            if (_currentPhase != null)
            {
                _currentPhase.OnExit();
            }

            _currentPhase = phase;
            _currentPhase.OnEnter();
        }
    }
}
