using System;
using System.Reflection;
using UnityEngine;

namespace Dokdo.Web
{
    public static class BrowserRuntimePolicy
    {
        private const string WaterTypeName =
            "UnityStandardAssets.Water.Water, Assembly-CSharp";
        private static bool capabilitiesReceived;
        private static bool coarsePointer;

        public static bool CoarsePointer
        {
            get { return coarsePointer; }
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyTiming()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Application.targetFrameRate =
                BrowserPerformanceSettings.TargetFrameRate;
            Time.fixedDeltaTime =
                BrowserPerformanceSettings.FixedTimestep;
#endif
        }

        public static void ApplyCapabilities(bool hasCoarsePointer)
        {
            capabilitiesReceived = true;
            coarsePointer = hasCoarsePointer;
            ApplyWaterPolicy();
        }

        public static void ApplyWaterPolicy()
        {
            if (!capabilitiesReceived)
            {
                return;
            }
            var useSimpleWater =
                BrowserPerformanceSettings.ShouldDisablePlanarReflection(
                    coarsePointer);
            var changed = useSimpleWater
                ? DisablePlanarReflections()
                : 0;
#if UNITY_WEBGL && !UNITY_EDITOR && DEVELOPMENT_BUILD
            BrowserPerformanceProbe.ReportWaterPolicy(
                useSimpleWater, changed);
#endif
        }

        private static int DisablePlanarReflections()
        {
            var waterType = Type.GetType(WaterTypeName, false);
            var setter = waterType?.GetMethod(
                "SetPlanarReflectionEnabled",
                BindingFlags.Instance | BindingFlags.Public);
            if (waterType == null || setter == null)
            {
                Debug.LogWarning(
                    "Web mobile water fallback could not find its adapter.");
                return 0;
            }

            var changed = 0;
            foreach (var candidate in
                Resources.FindObjectsOfTypeAll(waterType))
            {
                if (candidate is Component component &&
                    component.gameObject.scene.IsValid())
                {
                    setter.Invoke(candidate, new object[] { false });
                    changed++;
                }
            }
            return changed;
        }
    }
}
