namespace Sayne
{
    /// <summary>HP 바가 어떻게 움직이나. 뒤채움이 따라붙는 호흡과 바가 몸을 따라가는 속도.</summary>
    public readonly struct HPBarMotion
    {
        /// <summary>깎인 뒤 뒤채움이 따라오기 시작할 때까지 버티는 시간.</summary>
        public readonly float ChaseDelay;

        /// <summary>뒤채움이 앞채움을 따라붙는 속도.</summary>
        public readonly float ChaseSpeed;

        /// <summary>바가 몸을 따라가는 속도. 0 이면 딱 붙어 따라간다.</summary>
        public readonly float FollowSpeed;

        public HPBarMotion(float chaseDelay, float chaseSpeed, float followSpeed)
        {
            ChaseDelay = chaseDelay;
            ChaseSpeed = chaseSpeed;
            FollowSpeed = followSpeed;
        }

        public static HPBarMotion Default => new HPBarMotion(0.3f, 2.5f, 12f);
    }
}
