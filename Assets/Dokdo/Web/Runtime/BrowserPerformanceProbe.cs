#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dokdo.Web
{
    public sealed class BrowserPerformanceProbe : MonoBehaviour
    {
        private const float SampleWindowSeconds = 5f;
        private static BrowserPerformanceProbe instance;
        private int frames;
        private float windowStarted;
        private float maximumFrameMilliseconds;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var host = new GameObject("DOKDO Web Performance Probe");
            DontDestroyOnLoad(host);
            host.AddComponent<BrowserPerformanceProbe>();
#endif
        }

        private void Awake()
        {
            instance = this;
            ResetWindow();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            frames++;
            maximumFrameMilliseconds = Mathf.Max(
                maximumFrameMilliseconds,
                Time.unscaledDeltaTime * 1000f);
            var elapsed = Time.realtimeSinceStartup - windowStarted;
            if (elapsed < SampleWindowSeconds)
            {
                return;
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            DokdoPerformanceSample(
                frames / elapsed,
                maximumFrameMilliseconds,
                Profiler.GetTotalAllocatedMemoryLong(),
                Profiler.GetTotalReservedMemoryLong());
#endif
            frames = 0;
            windowStarted = Time.realtimeSinceStartup;
            maximumFrameMilliseconds = 0f;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            instance?.ResetWindow();
#if UNITY_WEBGL && !UNITY_EDITOR
            DokdoSceneChanged(scene.name);
#endif
        }

        private void ResetWindow()
        {
            frames = 0;
            windowStarted = Time.realtimeSinceStartup;
            maximumFrameMilliseconds = 0f;
        }

        public static void ReportWaterPolicy(bool simpleWater, int count)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            DokdoWaterPolicyApplied(simpleWater ? 1 : 0, count);
#endif
        }

        public static void ReportResponsivePresentation()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var logicalWidth = float.PositiveInfinity;
            var logicalHeight = float.PositiveInfinity;
            var scalerCount = 0;
            foreach (var scaler in
                Resources.FindObjectsOfTypeAll<CanvasScaler>())
            {
                if (!BrowserResponsivePresentation.IsEligible(scaler) ||
                    scaler.transform is not RectTransform rectTransform)
                {
                    continue;
                }
                scalerCount++;
                logicalWidth = Mathf.Min(logicalWidth, rectTransform.rect.width);
                logicalHeight = Mathf.Min(
                    logicalHeight, rectTransform.rect.height);
            }

            var activeSafeTargets = 0;
            var visibleSafeTargets = 0;
            var corners = new Vector3[4];
            var offscreenSafeTargets = new List<string>();
            foreach (var fitter in
                Resources.FindObjectsOfTypeAll<SafeAreaFitter>())
            {
                var target = fitter.Target;
                if (target == null || !target.gameObject.activeInHierarchy ||
                    !target.gameObject.scene.IsValid())
                {
                    continue;
                }
                activeSafeTargets++;
                target.GetWorldCorners(corners);
                var canvas = target.GetComponentInParent<Canvas>();
                var camera = canvas != null && canvas.renderMode !=
                    RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                var minimum = new Vector2(
                    float.PositiveInfinity, float.PositiveInfinity);
                var maximum = new Vector2(
                    float.NegativeInfinity, float.NegativeInfinity);
                foreach (var corner in corners)
                {
                    var point = RectTransformUtility.WorldToScreenPoint(
                        camera, corner);
                    minimum = Vector2.Min(minimum, point);
                    maximum = Vector2.Max(maximum, point);
                }
                var pivot = RectTransformUtility.WorldToScreenPoint(
                    camera, target.position);
                var overlapsScreen = maximum.x >= -1f && maximum.y >= -1f &&
                    minimum.x <= Screen.width + 1f &&
                    minimum.y <= Screen.height + 1f;
                var pivotOnScreen = pivot.x >= -1f && pivot.y >= -1f &&
                    pivot.x <= Screen.width + 1f &&
                    pivot.y <= Screen.height + 1f;
                if (overlapsScreen && pivotOnScreen)
                {
                    visibleSafeTargets++;
                }
                else
                {
                    offscreenSafeTargets.Add(target.name);
                }
            }

            var mainCamera = Camera.main;
            var viewportAspect = mainCamera != null &&
                mainCamera.pixelHeight > 0
                ? (float)mainCamera.pixelWidth / mainCamera.pixelHeight
                : 0f;
            var appliedOrthographicSize = mainCamera != null &&
                mainCamera.orthographic
                ? BrowserResponsivePresentation
                    .CalculateMainCameraOrthographicSize(
                        mainCamera, viewportAspect)
                : 0f;
            var extents = mainCamera != null && mainCamera.orthographic
                ? BrowserResponsivePresentation
                    .CalculateOrthographicHalfExtents(
                        appliedOrthographicSize, viewportAspect)
                : Vector2.zero;
            DokdoResponsivePresentation(
                Screen.width, Screen.height,
                float.IsPositiveInfinity(logicalWidth) ? 0f : logicalWidth,
                float.IsPositiveInfinity(logicalHeight) ? 0f : logicalHeight,
                extents.x, extents.y,
                mainCamera != null ? mainCamera.orthographicSize : 0f,
                appliedOrthographicSize,
                scalerCount,
                activeSafeTargets, visibleSafeTargets,
                string.Join(",", offscreenSafeTargets));
#endif
        }

        public static void ReportLifecycle(bool visibilityBlocked,
            bool orientationBlocked, bool blocked, int blockTransitions,
            int resumeTransitions, float timeScale, bool audioPaused)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            DokdoLifecycleState(
                visibilityBlocked ? 1 : 0,
                orientationBlocked ? 1 : 0,
                blocked ? 1 : 0,
                blockTransitions,
                resumeTransitions,
                timeScale,
                audioPaused ? 1 : 0);
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void DokdoPerformanceSample(
            float framesPerSecond,
            float maximumFrameMilliseconds,
            double allocatedBytes,
            double reservedBytes);

        [DllImport("__Internal")]
        private static extern void DokdoSceneChanged(string sceneName);

        [DllImport("__Internal")]
        private static extern void DokdoWaterPolicyApplied(
            int simpleWater, int changedWaterComponents);

        [DllImport("__Internal")]
        private static extern void DokdoResponsivePresentation(
            int screenWidth, int screenHeight,
            float logicalWidth, float logicalHeight,
            float projectionHalfWidth, float projectionHalfHeight,
            float sourceOrthographicSize, float orthographicSize,
            int scalerCount, int activeSafeTargets,
            int visibleSafeTargets, string offscreenSafeTargets);

        [DllImport("__Internal")]
        private static extern void DokdoLifecycleState(
            int visibilityBlocked, int orientationBlocked, int blocked,
            int blockTransitions, int resumeTransitions,
            float timeScale, int audioPaused);
#endif
    }
}
#endif
