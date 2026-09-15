namespace Sayne
{
    public abstract class PhaseBase
    {
        public abstract string Key { get; }

        public abstract void OnEnter();
        public abstract void OnExit();
    }
}
