namespace Dokdo.Web
{
    public static class WebBuildConstants
    {
        public const string ProductName = "DOKDO Reconstruction Web";
        public const int InitialMemoryMiB = 256;
        public const int MaximumMemoryMiB = 1024;
        public const float MemoryGrowthFactor = 0.2f;
        public const int MemoryGrowthCapMiB = 96;
        public const int MaximumSaveBytes = 256 * 1024;
        public const string PrimarySaveKey =
            "com.reconstruction.dokdo.web.save-v1.primary";
        public const string BackupSaveKey =
            "com.reconstruction.dokdo.web.save-v1.backup";
        public const string PersistenceStatusKey =
            "com.reconstruction.dokdo.web.persistence-v1";
    }
}
