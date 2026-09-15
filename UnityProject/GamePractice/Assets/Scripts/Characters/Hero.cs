using R3;

namespace Sayne
{
    public class Hero : Character
    {
        private readonly Subject<Unit> _ultimateUsed = new Subject<Unit>();

        public Observable<Unit> UltimateUsed => _ultimateUsed;

        public void UseUltimate()
        {
            if (!IsAlive.CurrentValue)
            {
                return;
            }

            _ultimateUsed.OnNext(Unit.Default);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _ultimateUsed.Dispose();
        }
    }
}
