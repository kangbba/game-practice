namespace Sayne
{
    public static class PhaseID
    {
        public static class Root
        {
            public const string Main = "Main";
            public const string Shop = "Shop";
            public const string Battle = "Battle";
        }

        public static class Battle
        {
            public const string Prepare = "Battle/Prepare";
            public const string Combat = "Battle/Combat";
            public const string Result = "Battle/Result";
            public const string Pause = "Battle/Pause";
        }
    }
}
