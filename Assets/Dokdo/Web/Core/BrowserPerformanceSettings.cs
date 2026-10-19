namespace Dokdo.Web
{
    public static class BrowserPerformanceSettings
    {
        public const int TargetFrameRate = 60;
        public const float FixedTimestep = 0.02f;
        public const int MaximumWasmHeapMiB = 1024;

        public static bool ShouldDisablePlanarReflection(
            bool coarsePointer)
        {
            return coarsePointer;
        }
    }
}
