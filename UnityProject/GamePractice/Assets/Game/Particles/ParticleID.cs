namespace Sayne
{
    public static class ParticleID
    {
        public const string Slash = "Slash";
        public const string HitSpark = "HitSpark";
        public const string Impact = "Impact";
        public const string DeathSmoke = "DeathSmoke";
        public const string Heal = "Heal";
        public const string FootstepDust = "FootstepDust";
        public const string SkillCharge = "SkillCharge";
        public const string Portal = "Portal";

        /// <summary>가젯(돌진)으로 달려가는 동안 몸에 붙는 꼬리. 파티클이 아니라 TrailRenderer 다.</summary>
        public const string DashTrail = "DashTrail";

        public static class Hero
        {
            public const string AldricUltimate = "AldricUltimate";
            public const string AldricUltimateCharge = "AldricUltimateCharge";
            public const string NyxUltimate = "NyxUltimate";
            public const string NyxUltimateCharge = "NyxUltimateCharge";
        }
    }
}
