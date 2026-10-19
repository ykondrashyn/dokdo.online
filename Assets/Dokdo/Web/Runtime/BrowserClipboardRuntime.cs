using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Dokdo;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dokdo.Web
{
    public static class BrowserClipboardRuntime
    {
        public static void Copy(
            string value,
            Action succeeded,
            Action<string> failed)
        {
            BrowserPlatformBridge.Instance.Copy(value, succeeded, failed);
        }

        public static void Paste(
            Action<string> succeeded,
            Action<string> failed)
        {
            BrowserPlatformBridge.Instance.Paste(succeeded, failed);
        }

        public static void ShowManualCopy(string value)
        {
            BrowserPlatformBridge.Instance.ShowManualCopy(value);
        }

        public static void RequestManualPaste(Action<string> completed)
        {
            BrowserPlatformBridge.Instance.RequestManualPaste(completed);
        }
    }

    public sealed class BrowserPlatformBridge : MonoBehaviour
    {
        private const string ObjectName = "DOKDO Web Platform";
        private static BrowserPlatformBridge instance;
        private readonly BrowserLifecycleController lifecycle = new();
        private Action copySucceeded;
        private Action<string> copyFailed;
        private Action<string> pasteSucceeded;
        private Action<string> pasteFailed;
        private Action<string> manualPasteCompleted;
        private float runningTimeScale = 1f;
        private bool runningAudioPause;
        private bool hasSafeArea;
        private float safeTop;
        private float safeRight;
        private float safeBottom;
        private float safeLeft;
        private float cssWidth;
        private float cssHeight;
        private BrowserSteeringControlRuntime steeringControl;
        private BrowserCameraZoomRuntime cameraZoomControl;
        private BrowserKeyboardSteeringRuntime keyboardControl;

        public static BrowserPlatformBridge Instance
        {
            get
            {
                EnsureInstance();
                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstance()
        {
            if (instance != null)
            {
                return;
            }
            var bridgeObject = new GameObject(ObjectName);
            DontDestroyOnLoad(bridgeObject);
            instance = bridgeObject.AddComponent<BrowserPlatformBridge>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            steeringControl = gameObject.AddComponent<
                BrowserSteeringControlRuntime>();
            cameraZoomControl = gameObject.AddComponent<
                BrowserCameraZoomRuntime>();
            keyboardControl = gameObject.AddComponent<BrowserKeyboardSteeringRuntime>();
            if (AdventureMusicRuntime.PlaybackEnabled)
            {
                gameObject.AddComponent<AdventureMusicRuntime>();
            }
            SceneManager.sceneLoaded += OnSceneLoaded;
            Camera.onPreCull += OnCameraPreCull;
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput = false;
            DokdoLifecycleInstall(ObjectName);
            if (DokdoKeyboardRegister(ObjectName) != 0)
                keyboardControl.Configure(available => DokdoKeyboardSetEnabled(available ? 1 : 0));
#endif
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Camera.onPreCull -= OnCameraPreCull;
            if (instance == this)
            {
                instance = null;
            }
        }

        internal void Copy(
            string value,
            Action succeeded,
            Action<string> failed)
        {
            copySucceeded = succeeded;
            copyFailed = failed;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (DokdoClipboardWrite(value, ObjectName) == 0)
            {
                CompleteCopyFailure("Clipboard API is unavailable.");
            }
#else
            CompleteCopyFailure("Clipboard API requires a Web player.");
#endif
        }

        internal void Paste(
            Action<string> succeeded,
            Action<string> failed)
        {
            pasteSucceeded = succeeded;
            pasteFailed = failed;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (DokdoClipboardRead(ObjectName) == 0)
            {
                CompletePasteFailure("Clipboard API is unavailable.");
            }
#else
            CompletePasteFailure("Clipboard API requires a Web player.");
#endif
        }

        internal void ShowManualCopy(string value)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            DokdoClipboardManualCopy(value);
#else
            Debug.LogWarning("Manual browser copy requested.");
#endif
        }

        internal void RequestManualPaste(Action<string> completed)
        {
            manualPasteCompleted = completed;
#if UNITY_WEBGL && !UNITY_EDITOR
            DokdoClipboardManualPaste();
#else
            Debug.LogWarning("Manual browser paste requested.");
#endif
        }

        public void OnClipboardWrite(string result)
        {
            if (result == "ok")
            {
                var callback = copySucceeded;
                ClearCopyCallbacks();
                callback?.Invoke();
            }
            else
            {
                CompleteCopyFailure(result);
            }
        }

        public void OnClipboardRead(string value)
        {
            var callback = pasteSucceeded;
            ClearPasteCallbacks();
            callback?.Invoke(value);
        }

        public void OnClipboardReadFailed(string reason)
        {
            CompletePasteFailure(reason);
        }

        public void OnManualPaste(string value)
        {
            var callback = manualPasteCompleted;
            manualPasteCompleted = null;
            callback?.Invoke(value);
        }

        public void OnBrowserHidden(string reason)
        {
            lifecycle.SetVisibilityBlocked(true, SaveSynchronously,
                PauseRuntime, ResumeRuntime, RecalculateElapsedTime);
            ReportLifecycle();
        }

        public void OnBrowserVisible(string reason)
        {
            lifecycle.SetVisibilityBlocked(false, SaveSynchronously,
                PauseRuntime, ResumeRuntime, RecalculateElapsedTime);
            ReportLifecycle();
        }

        public void OnOrientationBlocked(string payload)
        {
            if (payload != "0" && payload != "1")
            {
                Debug.LogWarning("Invalid browser orientation payload: " +
                    payload);
                return;
            }
            lifecycle.SetOrientationBlocked(payload == "1",
                SaveSynchronously, PauseRuntime, ResumeRuntime,
                RecalculateElapsedTime);
            ReportLifecycle();
        }

        public void OnSafeAreaChanged(string payload)
        {
            var parts = payload.Split(',');
            if (parts.Length != 6 ||
                !TryParse(parts[0], out safeTop) ||
                !TryParse(parts[1], out safeRight) ||
                !TryParse(parts[2], out safeBottom) ||
                !TryParse(parts[3], out safeLeft) ||
                !TryParse(parts[4], out cssWidth) ||
                !TryParse(parts[5], out cssHeight) ||
                cssWidth <= 0f || cssHeight <= 0f)
            {
                Debug.LogWarning("Invalid browser safe-area payload: " +
                    payload);
                return;
            }
            hasSafeArea = true;
            ApplyResponsivePresentationThenSafeArea();
        }

        public void OnCapabilities(string payload)
        {
            var parts = payload.Split(',');
            if (parts.Length < 1 ||
                (parts[0] != "0" && parts[0] != "1"))
            {
                Debug.LogWarning("Invalid browser capability payload: " +
                    payload);
                return;
            }
            BrowserRuntimePolicy.ApplyCapabilities(parts[0] == "1");
        }

        public void OnKeyboardSteering(string payload)
        {
            keyboardControl?.Receive(payload);
        }

        public void OnMouseWheelZoom(string payload)
        {
            if (!TryParse(payload, out var wheelDeltaPixels) ||
                cameraZoomControl == null ||
                !cameraZoomControl.ApplyWheelDelta(wheelDeltaPixels))
            {
                return;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            BrowserPerformanceProbe.ReportResponsivePresentation();
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void OnSmokeCommand(string command)
        {
            if (command == "scene:load")
            {
                SceneManager.LoadScene(SceneIds.Load);
            }
            else if (command == "scene:Intro")
            {
                SceneManager.LoadScene(SceneIds.Intro);
            }
            else if (command == "scene:Main")
            {
                SceneManager.LoadScene(SceneIds.Main);
            }
            else
            {
                Debug.LogWarning("Unknown Web smoke command: " + command);
            }
        }
#endif

        private void CompleteCopyFailure(string reason)
        {
            var callback = copyFailed;
            ClearCopyCallbacks();
            callback?.Invoke(reason);
        }

        private void CompletePasteFailure(string reason)
        {
            var callback = pasteFailed;
            ClearPasteCallbacks();
            callback?.Invoke(reason);
        }

        private void ClearCopyCallbacks()
        {
            copySucceeded = null;
            copyFailed = null;
        }

        private void ClearPasteCallbacks()
        {
            pasteSucceeded = null;
            pasteFailed = null;
        }

        private static void SaveSynchronously()
        {
            InvokeSingleton("GameManager", "Save");
        }

        private void PauseRuntime()
        {
            runningTimeScale = Time.timeScale;
            runningAudioPause = AudioListener.pause;
            AudioListener.pause = true;
            Time.timeScale = 0f;
        }

        private void ResumeRuntime()
        {
            Time.timeScale = runningTimeScale;
            AudioListener.pause = runningAudioPause;
        }

        private void ReportLifecycle()
        {
            keyboardControl?.SetLifecycleBlocked(lifecycle.IsBlocked);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            BrowserPerformanceProbe.ReportLifecycle(
                lifecycle.IsVisibilityBlocked,
                lifecycle.IsOrientationBlocked,
                lifecycle.IsBlocked,
                lifecycle.BlockTransitionCount,
                lifecycle.ResumeTransitionCount,
                Time.timeScale,
                AudioListener.pause);
#endif
        }

        private static void RecalculateElapsedTime()
        {
            if (!InvokeFirstObject("ShipController", "ReCalTime"))
            {
                InvokeSingleton("GameManager", "ReCalTime");
            }
        }

        private static bool InvokeSingleton(string typeName, string methodName)
        {
            var type = Type.GetType(typeName + ", Assembly-CSharp", false);
            var field = type?.GetField("Instance");
            var target = field?.GetValue(null);
            var method = type?.GetMethod(methodName);
            if (target == null || method == null)
            {
                return false;
            }
            method.Invoke(target, null);
            return true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyResponsivePresentationThenSafeArea();
            BrowserRuntimePolicy.ApplyWaterPolicy();
            steeringControl.Refresh();
            cameraZoomControl.Refresh();
            keyboardControl.Refresh();
        }


        private void LateUpdate()
        {
            if (BrowserResponsivePresentation
                .ApplyDiscoveredCanvasScalers() > 0)
            {
                ApplySafeArea();
            }
        }

        private static void OnCameraPreCull(Camera camera)
        {
            BrowserResponsivePresentation.ApplyMainCameraProjection(camera);
        }

        private void ApplyResponsivePresentationThenSafeArea()
        {
            BrowserResponsivePresentation.ApplyCanvasPolicyThen(
                Resources.FindObjectsOfTypeAll<CanvasScaler>(),
                ApplySafeArea);
            BrowserResponsivePresentation
                .ApplyDiscoveredMapViewportBackgrounds();
            BrowserResponsivePresentation.ApplyMainCameraProjection(
                Camera.main);
        }

        private void ApplySafeArea()
        {
            if (!hasSafeArea || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }
            Canvas.ForceUpdateCanvases();
            var scaleX = Screen.width / cssWidth;
            var scaleY = Screen.height / cssHeight;
            var safeArea = new Rect(
                safeLeft * scaleX,
                safeBottom * scaleY,
                Mathf.Max(0f, Screen.width -
                    (safeLeft + safeRight) * scaleX),
                Mathf.Max(0f, Screen.height -
                    (safeTop + safeBottom) * scaleY));
            steeringControl.SetSafeArea(safeArea);
            foreach (var fitter in
                Resources.FindObjectsOfTypeAll<SafeAreaFitter>())
            {
                if (fitter.gameObject.scene.IsValid())
                {
                    // Screen.safeArea is not populated consistently by WebGL.
                    // Once CSS reports the browser insets, prevent the normal
                    // Update loop from replacing them with the full screen.
                    if (fitter.enabled)
                    {
                        fitter.enabled = false;
                    }
                    fitter.ApplySafeArea(safeArea,
                        new Vector2Int(Screen.width, Screen.height));
                }
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            BrowserPerformanceProbe.ReportResponsivePresentation();
#endif
        }

        private static bool TryParse(string value, out float result)
        {
            return float.TryParse(value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out result);
        }

        private static bool InvokeFirstObject(string typeName, string methodName)
        {
            var type = Type.GetType(typeName + ", Assembly-CSharp", false);
            var target = type == null
                ? null
                : FindFirstObjectByType(type) as Component;
            var method = type?.GetMethod(methodName);
            if (target == null || method == null)
            {
                return false;
            }
            method.Invoke(target, null);
            return true;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int DokdoClipboardWrite(
            string value, string receiver);

        [DllImport("__Internal")]
        private static extern int DokdoClipboardRead(string receiver);

        [DllImport("__Internal")]
        private static extern void DokdoClipboardManualCopy(string value);

        [DllImport("__Internal")]
        private static extern void DokdoClipboardManualPaste();

        [DllImport("__Internal")]
        private static extern int DokdoKeyboardRegister(string receiver);

        [DllImport("__Internal")]
        private static extern void DokdoKeyboardSetEnabled(int enabled);

        [DllImport("__Internal")]
        private static extern void DokdoLifecycleInstall(string receiver);
#endif
    }
}
