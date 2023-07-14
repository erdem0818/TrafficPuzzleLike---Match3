namespace Core.Scripts.Gameplay
{
    public static class Values
    {
        public const int GridPartMask = 1 << 6;
        public const int ArrowMask = 1 << 7;

        public const sbyte RowOrColumnFull = -0b1100100;

        public const float OneGridPassTime = 0.2f;

        public const string LevelArgsKey = "LevelArgs";
    }
}
