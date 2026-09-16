namespace Sayne
{
    public class Enemy : Character
    {
        public Hero Target { get; private set; }

        public void SetTarget(Hero target)
        {
            Target = target;
        }
    }
}
