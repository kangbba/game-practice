namespace Sayne
{
    public readonly struct HPBarStyle
    {
        public readonly float ChaseDelay;
        public readonly float ChaseSpeed;
        public readonly float FollowSpeed;

        public HPBarStyle(float chaseDelay, float chaseSpeed, float followSpeed)
        {
            ChaseDelay = chaseDelay;
            ChaseSpeed = chaseSpeed;
            FollowSpeed = followSpeed;
        }

        public static HPBarStyle Default => new HPBarStyle(0.3f, 2.5f, 12f);
    }
}
