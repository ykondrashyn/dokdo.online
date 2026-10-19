using UnityEngine;

namespace Dokdo
{
    public static class AndroidRuntimePolicy
    {
        public const int TargetFrameRate = 60;
        public const float FixedTimestep = 0.02f;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
            Time.fixedDeltaTime = FixedTimestep;
#endif
        }
    }
}
