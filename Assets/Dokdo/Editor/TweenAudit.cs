using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dokdo.Editor
{
    public static class TweenAudit
    {
        private const string MainScenePath = "Assets/0.my/Scenes/Main.unity";

        [Serializable]
        private sealed class AuditReport
        {
            public int schemaVersion = 1;
            public string unityVersion;
            public string dotweenVersion;
            public int animations;
            public int visualManagers;
            public int paths;
            public int pathWaypoints;
            public string[] smokeChecks;
            public string[] problems;
        }

        public static void Run()
        {
            var problems = new List<string>();
            var scene = EditorSceneManager.OpenScene(
                MainScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var animations = Components<DOTweenAnimation>(roots);
            var managers = Components<DOTweenVisualManager>(roots);
            var paths = Components<DOTweenPath>(roots);

            Compare(problems, "DOTweenAnimation count", animations.Length, 24);
            Compare(problems, "DOTweenVisualManager count", managers.Length, 29);
            Compare(problems, "DOTweenPath count", paths.Length, 5);

            var waypointCounts = paths.Select(path => path.wps.Count)
                .OrderBy(value => value)
                .ToArray();
            if (!waypointCounts.SequenceEqual(new[] { 5, 6, 6, 6, 6 }))
            {
                problems.Add(
                    "path waypoint counts differ: " +
                    string.Join(",", waypointCounts.Select(value => value.ToString())));
            }

            foreach (var manager in managers)
            {
                if (manager.preset != VisualManagerPreset.Custom ||
                    manager.onEnableBehaviour != OnEnableBehaviour.Restart ||
                    manager.onDisableBehaviour != OnDisableBehaviour.None)
                {
                    problems.Add(
                        "visual manager behavior differs: " +
                        HierarchyPath(manager.transform));
                }
            }

            foreach (var path in paths)
            {
                if (!path.autoPlay || path.autoKill || !path.isClosedPath ||
                    path.loops != -1 || path.orientType != OrientType.ToPath)
                {
                    problems.Add(
                        "path behavior differs: " + HierarchyPath(path.transform));
                }
            }

            var smokeChecks = RunSmokeChecks(problems);
            var report = new AuditReport
            {
                unityVersion = Application.unityVersion,
                dotweenVersion = DOTween.Version,
                animations = animations.Length,
                visualManagers = managers.Length,
                paths = paths.Length,
                pathWaypoints = paths.Sum(path => path.wps.Count),
                smokeChecks = smokeChecks,
                problems = problems.OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray()
            };

            var outputPath = Path.GetFullPath(Path.Combine(
                Application.dataPath, "../.artifacts/unity-tween-audit.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, JsonUtility.ToJson(report, true) + "\n");

            Debug.Log(string.Format(
                "DOKDO_TWEEN_AUDIT animations={0} managers={1} paths={2} " +
                "smoke={3} problems={4} report={5}",
                animations.Length, managers.Length, paths.Length,
                smokeChecks.Length, problems.Count, outputPath));

            if (problems.Count != 0)
            {
                throw new InvalidOperationException(
                    "DOKDO tween audit failed:\n" + string.Join("\n", problems));
            }
        }

        private static string[] RunSmokeChecks(ICollection<string> problems)
        {
            var passed = new List<string>();
            GameObject root = null;
            try
            {
                DOTween.Clear(true);
                DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
                root = new GameObject("DOKDO Tween Audit");

                CheckUiFade(root, passed, problems);
                CheckCameraZoom(root, passed, problems);
                CheckPickupMotion(root, passed, problems);
                CheckCombatPunch(root, passed, problems);
                CheckPath(root, passed, problems);
            }
            catch (Exception exception)
            {
                problems.Add(
                    "smoke setup failed: " + exception.GetType().Name + ": " +
                    exception.Message);
            }
            finally
            {
                DOTween.Clear(true);
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            return passed.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static void CheckUiFade(GameObject root, ICollection<string> passed,
            ICollection<string> problems)
        {
            var target = new GameObject("UI Fade");
            target.transform.SetParent(root.transform);
            var group = target.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            var tween = group.DOFade(1f, 1f).SetAutoKill(false);
            tween.Goto(1f, true);
            CheckApproximately(
                "ui fade", group.alpha, 1f, passed, problems);
            tween.Kill();
        }

        private static void CheckCameraZoom(GameObject root,
            ICollection<string> passed, ICollection<string> problems)
        {
            var target = new GameObject("Camera Zoom");
            target.transform.SetParent(root.transform);
            var camera = target.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            var tween = camera.DOOrthoSize(12f, 1f).SetAutoKill(false);
            tween.Goto(1f, true);
            CheckApproximately(
                "camera zoom", camera.orthographicSize, 12f, passed, problems);
            tween.Kill();
        }

        private static void CheckPickupMotion(GameObject root,
            ICollection<string> passed, ICollection<string> problems)
        {
            var target = new GameObject("Pickup Motion").transform;
            target.SetParent(root.transform);
            target.localPosition = Vector3.zero;
            var tween = target.DOLocalMove(
                new Vector3(3f, 2f, 1f), 1f).SetAutoKill(false);
            tween.Goto(1f, true);
            if (Vector3.Distance(target.localPosition, new Vector3(3f, 2f, 1f)) <
                0.0001f)
            {
                passed.Add("pickup motion");
            }
            else
            {
                problems.Add("pickup motion did not reach its target");
            }
            tween.Kill();
        }

        private static void CheckCombatPunch(GameObject root,
            ICollection<string> passed, ICollection<string> problems)
        {
            var target = new GameObject("Combat Punch").transform;
            target.SetParent(root.transform);
            target.localScale = Vector3.one;
            var completed = false;
            var tween = target.DOPunchScale(
                    Vector3.one * 0.3f, 1f, 5, 0f)
                .SetAutoKill(false)
                .OnComplete(() => completed = true);
            tween.Goto(0.05f, true);
            var changed = Vector3.Distance(target.localScale, Vector3.one) > 0.001f;
            tween.Complete(true);
            if (changed && completed)
            {
                passed.Add("combat punch");
            }
            else
            {
                problems.Add("combat punch did not animate and complete");
            }
            tween.Kill();
        }

        private static void CheckPath(GameObject root, ICollection<string> passed,
            ICollection<string> problems)
        {
            var target = new GameObject("Path Compatibility");
            target.transform.SetParent(root.transform);
            target.transform.localPosition = Vector3.zero;
            var path = target.AddComponent<DOTweenPath>();
            path.autoPlay = false;
            path.autoKill = false;
            path.duration = 1f;
            path.isLocal = true;
            path.pathType = PathType.Linear;
            path.pathMode = PathMode.Ignore;
            path.orientType = OrientType.None;
            path.wps = new List<Vector3>
            {
                Vector3.right,
                Vector3.right + Vector3.forward
            };
            path.CreateTween();
            var tween = path.GetTween();
            if (tween == null)
            {
                problems.Add("path compatibility did not create a tween");
                return;
            }

            tween.Goto(0.5f, true);
            if (target.transform.localPosition.sqrMagnitude > 0.01f)
            {
                passed.Add("path compatibility");
            }
            else
            {
                problems.Add("path compatibility did not move its target");
            }
            tween.Kill();
        }

        private static T[] Components<T>(IEnumerable<GameObject> roots)
            where T : Component
        {
            return roots.SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
        }

        private static void Compare(ICollection<string> problems, string name,
            int actual, int expected)
        {
            if (actual != expected)
            {
                problems.Add(string.Format(
                    "{0} differs: expected {1}, got {2}", name, expected, actual));
            }
        }

        private static void CheckApproximately(string name, float actual,
            float expected, ICollection<string> passed,
            ICollection<string> problems)
        {
            if (Mathf.Abs(actual - expected) < 0.0001f)
            {
                passed.Add(name);
            }
            else
            {
                problems.Add(string.Format(
                    "{0} differs: expected {1}, got {2}", name, expected, actual));
            }
        }

        private static string HierarchyPath(Transform transform)
        {
            var names = new List<string>();
            while (transform != null)
            {
                names.Add(transform.name);
                transform = transform.parent;
            }
            names.Reverse();
            return string.Join("/", names);
        }
    }
}
