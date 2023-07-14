namespace Core.Scripts.Gameplay.SaveRelated
{
    [System.Serializable]
    public struct LevelArgs
    {
        public int LevelId;
        public bool IsLocked;
        public int StarCount;

        private const string LevelSavePostFix = "LevelSave";
        public static string GetKeyByID(int id) => $"{LevelSavePostFix}_{id}";
    }
}