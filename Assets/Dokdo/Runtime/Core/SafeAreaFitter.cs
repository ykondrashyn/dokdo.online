using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dokdo
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [Flags]
        public enum Edges
        {
            None = 0,
            Left = 1,
            Right = 2,
            Bottom = 4,
            Top = 8,
            All = Left | Right | Bottom | Top
        }

        [SerializeField]
        private RectTransform target;

        [SerializeField]
        private Edges fittedEdges = Edges.All;

        private Vector2 baselineAnchorMin;
        private Vector2 baselineAnchorMax;
        private Vector2 baselineOffsetMin;
        private Vector2 baselineOffsetMax;
        private Rect lastSafeArea = new Rect(-1f, -1f, -1f, -1f);
        private Vector2Int lastScreenSize = new Vector2Int(-1, -1);
        private bool baselineCaptured;
        private bool applying;

        public Edges FittedEdges
        {
            get { return fittedEdges; }
        }

        public RectTransform Target
        {
            get { return target; }
        }

        public void Configure(Edges edges)
        {
            fittedEdges = edges;
            CaptureBaseline();
            ApplyCurrentSafeArea(true);
        }

        public void ApplySafeArea(Rect safeArea, Vector2Int screenSize)
        {
            CaptureBaseline();
            if (target == null || screenSize.x <= 0 || screenSize.y <= 0)
            {
                return;
            }

            var minimum = new Vector2(
                Mathf.Clamp01(safeArea.xMin / screenSize.x),
                Mathf.Clamp01(safeArea.yMin / screenSize.y));
            var maximum = new Vector2(
                Mathf.Clamp01(safeArea.xMax / screenSize.x),
                Mathf.Clamp01(safeArea.yMax / screenSize.y));
            maximum.x = Mathf.Max(maximum.x, minimum.x);
            maximum.y = Mathf.Max(maximum.y, minimum.y);

            var anchorMin = baselineAnchorMin;
            var anchorMax = baselineAnchorMax;
            FitAxis(ref anchorMin.x, ref anchorMax.x, minimum.x,
                1f - maximum.x,
                (fittedEdges & Edges.Left) != 0,
                (fittedEdges & Edges.Right) != 0);
            FitAxis(ref anchorMin.y, ref anchorMax.y, minimum.y,
                1f - maximum.y,
                (fittedEdges & Edges.Bottom) != 0,
                (fittedEdges & Edges.Top) != 0);

            applying = true;
            target.anchorMin = anchorMin;
            target.anchorMax = anchorMax;
            target.offsetMin = baselineOffsetMin;
            target.offsetMax = baselineOffsetMax;
            applying = false;
            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
        }

        private static void FitAxis(ref float anchorMin, ref float anchorMax,
            float minimumInset, float maximumInset, bool fitMinimum,
            bool fitMaximum)
        {
            if (fitMinimum && fitMaximum)
            {
                anchorMin += minimumInset;
                anchorMax -= maximumInset;
                return;
            }
            if (fitMinimum)
            {
                anchorMin += minimumInset;
                anchorMax += minimumInset;
            }
            else if (fitMaximum)
            {
                anchorMin -= maximumInset;
                anchorMax -= maximumInset;
            }
        }

        public void RestoreBaseline()
        {
            if (!baselineCaptured || target == null)
            {
                return;
            }
            applying = true;
            target.anchorMin = baselineAnchorMin;
            target.anchorMax = baselineAnchorMax;
            target.offsetMin = baselineOffsetMin;
            target.offsetMax = baselineOffsetMax;
            applying = false;
        }

        private void Reset()
        {
            target = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            CaptureBaseline();
            ApplyCurrentSafeArea(true);
        }

        private void OnDisable()
        {
            RestoreBaseline();
        }

        private void Update()
        {
            ApplyCurrentSafeArea(false);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!applying && isActiveAndEnabled)
            {
                ApplyCurrentSafeArea(false);
            }
        }

        private void CaptureBaseline()
        {
            if (baselineCaptured)
            {
                return;
            }
            if (target == null)
            {
                target = GetComponent<RectTransform>();
            }
            if (target == null)
            {
                return;
            }
            baselineAnchorMin = target.anchorMin;
            baselineAnchorMax = target.anchorMax;
            baselineOffsetMin = target.offsetMin;
            baselineOffsetMax = target.offsetMax;
            baselineCaptured = true;
        }

        private void ApplyCurrentSafeArea(bool force)
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            var safeArea = Screen.safeArea;
            if (!force && safeArea == lastSafeArea && size == lastScreenSize)
            {
                return;
            }
            ApplySafeArea(safeArea, size);
        }
    }

    public static class SafeAreaBootstrap
    {
        public readonly struct TargetSpec
        {
            public readonly string SceneName;
            public readonly string Path;
            public readonly SafeAreaFitter.Edges Edges;

            public TargetSpec(string sceneName, string path,
                SafeAreaFitter.Edges edges)
            {
                SceneName = sceneName;
                Path = path;
                Edges = edges;
            }
        }

        private static readonly TargetSpec[] TargetSpecs =
        {
            new TargetSpec("Intro", "UI Manager/GameObject/Text (1)",
                SafeAreaFitter.Edges.Bottom),
            new TargetSpec("Main", "monitor",
                SafeAreaFitter.Edges.Left | SafeAreaFitter.Edges.Right |
                SafeAreaFitter.Edges.Bottom),
            new TargetSpec("Main", "UI Manager/HpBar",
                SafeAreaFitter.Edges.Left | SafeAreaFitter.Edges.Right |
                SafeAreaFitter.Edges.Top),
            new TargetSpec("Main", "UI Manager/BannerRect",
                SafeAreaFitter.Edges.Left | SafeAreaFitter.Edges.Right |
                SafeAreaFitter.Edges.Top),
            new TargetSpec("Main", "UI Manager/RemainEnemy",
                SafeAreaFitter.Edges.Top),
            new TargetSpec("Main", "UI Manager/ArenaHud",
                SafeAreaFitter.Edges.Top),
            new TargetSpec("Main", "UI Manager/Image",
                SafeAreaFitter.Edges.Right | SafeAreaFitter.Edges.Top),
            new TargetSpec("Main", "UI Manager/Image (1)",
                SafeAreaFitter.Edges.Left | SafeAreaFitter.Edges.Right |
                SafeAreaFitter.Edges.Top),
            new TargetSpec("Main", "UI Manager/MyResource",
                SafeAreaFitter.Edges.Left | SafeAreaFitter.Edges.Top),
            new TargetSpec("Main", "UI Manager/Score",
                SafeAreaFitter.Edges.Top),
            new TargetSpec("Main", "UI Manager/MiniMap/MiniMap",
                SafeAreaFitter.Edges.Right | SafeAreaFitter.Edges.Top),
            new TargetSpec("Main", "UI Manager/PauseMenu",
                SafeAreaFitter.Edges.All),
            new TargetSpec("Main", "UI Manager/IslandMenu",
                SafeAreaFitter.Edges.All)
        };

        public static TargetSpec[] GetSpecs(string sceneName)
        {
            return TargetSpecs.Where(spec => string.Equals(
                    spec.SceneName, sceneName, StringComparison.Ordinal))
                .ToArray();
        }

        public static RectTransform FindTarget(Scene scene, string path)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (!canvas.isRootCanvas)
                    {
                        continue;
                    }
                    var child = canvas.transform.Find(path);
                    if (child is RectTransform rectTransform)
                    {
                        return rectTransform;
                    }
                }
            }
            return null;
        }

        public static void ApplyToScene(Scene scene)
        {
            foreach (var spec in GetSpecs(scene.name))
            {
                var target = FindTarget(scene, spec.Path);
                if (target == null)
                {
                    Debug.LogWarning("DOKDO safe-area target is missing: " +
                        scene.name + "/" + spec.Path);
                    continue;
                }
                var fitter = target.GetComponent<SafeAreaFitter>();
                if (fitter == null)
                {
                    fitter = target.gameObject.AddComponent<SafeAreaFitter>();
                }
                fitter.Configure(spec.Edges);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
#if UNITY_ANDROID
            Input.backButtonLeavesApp = false;
#endif
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyToScene(scene);
        }
    }
}
