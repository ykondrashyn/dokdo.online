using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dokdo.Editor
{
    public static class UiAudit
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/0.my/Scenes/Intro.unity",
            "Assets/0.my/Scenes/Main.unity"
        };

        private static readonly string[] MajorMainPanels =
        {
            "Panel_Cong",
            "Panel_Store",
            "Panel_Ask",
            "GetIslandAlert",
            "DeadPanelArena",
            "DeadPanel",
            "Panel_Store 2",
            "UI Manager/Panel_ReReward",
            "UI Manager/PauseMenu",
            "UI Manager/Panel_Assi",
            "UI Manager/Panel_Cloud",
            "UI Manager/Panel_DailyReward"
        };

        private static readonly string[] FullBleedExclusions =
        {
            "UI Manager/Loading",
            "UI Manager/LoadingTemp",
            "UI Manager/Touch"
        };

        private const string JoystickTexturePath =
            "Assets/Texture2D/UI_ring003.png";
        private const string JoystickMaterialPath =
            "Assets/Material/UI_ring001.mat";

        [Serializable]
        private sealed class AuditReport
        {
            public int schemaVersion = 1;
            public string unityVersion;
            public string orientation;
            public SceneReport[] scenes;
            public JoystickReport joystick;
            public string[] smokeChecks;
            public string[] problems;
        }

        [Serializable]
        private sealed class SceneReport
        {
            public string path;
            public int rootCanvases;
            public Vector2 referenceResolution;
            public float matchWidthOrHeight;
            public TargetReport[] safeAreaTargets;
            public string[] majorPanels;
            public string[] fullBleedExclusions;
        }

        [Serializable]
        private sealed class JoystickReport
        {
            public string texturePath;
            public bool mipmaps;
            public string wrapMode;
            public bool alphaIsTransparency;
            public bool webGlOverridden;
            public int webGlMaxTextureSize;
            public string webGlFormat;
            public string webGlCompression;
            public string materialPath;
            public string mesh;
            public int meshVertices;
            public int bones;
            public string skinWeights;
            public string rendererQuality;
        }

        [Serializable]
        private sealed class TargetReport
        {
            public string path;
            public string edges;
            public bool resolved;
            public Vector2 anchorMin;
            public Vector2 anchorMax;
        }

        public static void Run()
        {
            var problems = new List<string>();
            if (PlayerSettings.defaultInterfaceOrientation !=
                UIOrientation.Portrait)
            {
                problems.Add("player orientation is not fixed portrait");
            }

            var sceneReports = ScenePaths.Select(
                    path => InspectScene(path, problems))
                .ToArray();
            var joystick = InspectJoystick(problems);
            var smokeChecks = RunSmokeChecks(problems);
            var report = new AuditReport
            {
                unityVersion = Application.unityVersion,
                orientation =
                    PlayerSettings.defaultInterfaceOrientation.ToString(),
                scenes = sceneReports,
                joystick = joystick,
                smokeChecks = smokeChecks,
                problems = problems.OrderBy(
                        problem => problem, StringComparer.Ordinal)
                    .ToArray()
            };

            var outputPath = Path.GetFullPath(Path.Combine(
                Application.dataPath, "../.artifacts/unity-ui-audit.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, JsonUtility.ToJson(report, true) + "\n");

            Debug.Log(string.Format(
                "DOKDO_UI_AUDIT scenes={0} targets={1} smoke={2} " +
                "joystick={3} problems={4} report={5}",
                sceneReports.Length,
                sceneReports.Sum(scene => scene.safeAreaTargets.Length),
                smokeChecks.Length, joystick.mesh,
                problems.Count, outputPath));
            if (problems.Count != 0)
            {
                throw new InvalidOperationException(
                    "DOKDO UI audit failed:\n" +
                    string.Join("\n", problems));
            }
        }

        private static JoystickReport InspectJoystick(
            ICollection<string> problems)
        {
            var importer = AssetImporter.GetAtPath(JoystickTexturePath)
                as TextureImporter;
            var webGl = importer == null
                ? new TextureImporterPlatformSettings()
                : importer.GetPlatformTextureSettings(
                    JoystickTexturePostprocessor.WebGlPlatform);
            if (importer == null)
            {
                problems.Add("joystick texture importer is missing");
            }
            else
            {
                if (importer.mipmapEnabled)
                {
                    problems.Add("joystick texture mipmaps are enabled");
                }
                if (importer.wrapMode != TextureWrapMode.Clamp)
                {
                    problems.Add("joystick texture does not clamp its edges");
                }
                if (!importer.alphaIsTransparency)
                {
                    problems.Add(
                        "joystick texture alpha transparency is disabled");
                }
                if (!webGl.overridden)
                {
                    problems.Add("joystick WebGL override is disabled");
                }
                if (webGl.maxTextureSize !=
                    JoystickTexturePostprocessor.SourceSize)
                {
                    problems.Add("joystick WebGL texture size differs");
                }
                if (webGl.format != TextureImporterFormat.RGBA32)
                {
                    problems.Add("joystick WebGL texture is not RGBA32");
                }
                if (webGl.textureCompression !=
                    TextureImporterCompression.Uncompressed)
                {
                    problems.Add("joystick WebGL texture is compressed");
                }
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(
                JoystickMaterialPath);
            if (material == null)
            {
                problems.Add("joystick material is missing");
            }
            else if (AssetDatabase.GetAssetPath(material.mainTexture) !=
                JoystickTexturePath)
            {
                problems.Add("joystick material texture differs");
            }
            if (QualitySettings.skinWeights == SkinWeights.OneBone)
            {
                problems.Add("joystick quality discards secondary bone weights");
            }

            var mainScene = SceneManager.GetSceneByPath(ScenePaths[1]);
            var controllers = mainScene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<ControllerView>(true))
                .ToArray();
            Compare(problems, "Main joystick controller count",
                controllers.Length, 1);
            var renderer = controllers.Length == 1 &&
                controllers[0].handleArea != null
                ? controllers[0].handleArea.GetComponentInChildren<
                    SkinnedMeshRenderer>(true)
                : null;
            if (renderer == null)
            {
                problems.Add("joystick skinned renderer is missing");
            }
            else
            {
                if (renderer.sharedMesh == null ||
                    renderer.sharedMesh.name != "Sphere001" ||
                    renderer.sharedMesh.vertexCount != 296)
                {
                    problems.Add("joystick pointer mesh differs");
                }
                if (renderer.bones.Length != 2)
                {
                    problems.Add("joystick pointer bone count differs");
                }
                if (renderer.quality == SkinQuality.Bone1)
                {
                    problems.Add("joystick renderer uses one-bone skinning");
                }
                if (AssetDatabase.GetAssetPath(renderer.sharedMaterial) !=
                    JoystickMaterialPath)
                {
                    problems.Add("joystick renderer material differs");
                }
            }

            return new JoystickReport
            {
                texturePath = JoystickTexturePath,
                mipmaps = importer != null && importer.mipmapEnabled,
                wrapMode = importer == null
                    ? null
                    : importer.wrapMode.ToString(),
                alphaIsTransparency = importer != null &&
                    importer.alphaIsTransparency,
                webGlOverridden = importer != null && webGl.overridden,
                webGlMaxTextureSize = importer == null
                    ? 0
                    : webGl.maxTextureSize,
                webGlFormat = importer == null
                    ? null
                    : webGl.format.ToString(),
                webGlCompression = importer == null
                    ? null
                    : webGl.textureCompression.ToString(),
                materialPath = material == null
                    ? null
                    : AssetDatabase.GetAssetPath(material),
                mesh = renderer == null || renderer.sharedMesh == null
                    ? null
                    : renderer.sharedMesh.name,
                meshVertices = renderer == null ||
                    renderer.sharedMesh == null
                    ? 0
                    : renderer.sharedMesh.vertexCount,
                bones = renderer == null ? 0 : renderer.bones.Length,
                skinWeights = QualitySettings.skinWeights.ToString(),
                rendererQuality = renderer == null
                    ? null
                    : renderer.quality.ToString()
            };
        }

        private static SceneReport InspectScene(string path,
            ICollection<string> problems)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var canvases = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
                .Where(canvas => canvas.isRootCanvas)
                .ToArray();
            var scaledCanvases = canvases.Where(
                    canvas => canvas.GetComponent<CanvasScaler>() != null)
                .ToArray();
            Compare(problems, path + " scaled root Canvas count",
                scaledCanvases.Length, 1);

            var referenceResolution = Vector2.zero;
            var matchWidthOrHeight = -1f;
            if (scaledCanvases.Length != 0)
            {
                var scaler = scaledCanvases[0].GetComponent<CanvasScaler>();
                referenceResolution = scaler.referenceResolution;
                matchWidthOrHeight = scaler.matchWidthOrHeight;
                if (scaler.uiScaleMode !=
                    CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    problems.Add(path + " CanvasScaler mode differs");
                }
                if (referenceResolution != new Vector2(720f, 1280f))
                {
                    problems.Add(path + " reference resolution differs");
                }
                if (Mathf.Abs(matchWidthOrHeight - 1f) > 0.0001f)
                {
                    problems.Add(path + " recovered screen match differs");
                }
            }

            var specs = SafeAreaBootstrap.GetSpecs(scene.name);
            Compare(problems, path + " safe-area spec count", specs.Length,
                scene.name == "Intro" ? 1 : 12);
            var targetReports = specs.Select(spec =>
            {
                var target = SafeAreaBootstrap.FindTarget(scene, spec.Path);
                var resolved = target != null;
                if (!resolved)
                {
                    problems.Add(path + " safe-area target missing: " + spec.Path);
                }
                return new TargetReport
                {
                    path = spec.Path,
                    edges = spec.Edges.ToString(),
                    resolved = resolved,
                    anchorMin = resolved ? target.anchorMin : Vector2.zero,
                    anchorMax = resolved ? target.anchorMax : Vector2.zero
                };
            }).ToArray();

            SafeAreaBootstrap.ApplyToScene(scene);
            var fitters = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<SafeAreaFitter>(true))
                .ToArray();
            Compare(problems, path + " installed SafeAreaFitter count",
                fitters.Length, specs.Length);

            var majorPanels = Array.Empty<string>();
            var exclusions = Array.Empty<string>();
            if (scene.name == "Main")
            {
                majorPanels = MajorMainPanels.Where(panel =>
                    SafeAreaBootstrap.FindTarget(scene, panel) != null).ToArray();
                Compare(problems, path + " major-panel count",
                    majorPanels.Length, MajorMainPanels.Length);

                exclusions = FullBleedExclusions.Where(exclusion =>
                {
                    var target = SafeAreaBootstrap.FindTarget(scene, exclusion);
                    if (target == null)
                    {
                        problems.Add(path + " full-bleed root missing: " + exclusion);
                        return false;
                    }
                    if (target.GetComponent<SafeAreaFitter>() != null)
                    {
                        problems.Add(path + " full-bleed root is fitted: " +
                            exclusion);
                        return false;
                    }
                    return true;
                }).ToArray();
            }

            return new SceneReport
            {
                path = path,
                rootCanvases = canvases.Length,
                referenceResolution = referenceResolution,
                matchWidthOrHeight = matchWidthOrHeight,
                safeAreaTargets = targetReports,
                majorPanels = majorPanels,
                fullBleedExclusions = exclusions
            };
        }

        private static string[] RunSmokeChecks(ICollection<string> problems)
        {
            var passed = new List<string>();
            GameObject target = null;
            try
            {
                target = new GameObject(
                    "DOKDO Safe Area Audit", typeof(RectTransform));
                var rect = target.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                var fitter = target.AddComponent<SafeAreaFitter>();
                fitter.Configure(SafeAreaFitter.Edges.All);
                fitter.ApplySafeArea(
                    new Rect(24f, 80f, 672f, 1440f),
                    new Vector2Int(720, 1600));

                if (Vector2.Distance(rect.anchorMin,
                        new Vector2(24f / 720f, 0.05f)) < 0.0001f &&
                    Vector2.Distance(rect.anchorMax,
                        new Vector2(696f / 720f, 0.95f)) < 0.0001f)
                {
                    passed.Add("cutout and gesture insets");
                }
                else
                {
                    problems.Add("safe-area normalized anchors differ");
                }

                fitter.RestoreBaseline();
                if (rect.anchorMin == Vector2.zero &&
                    rect.anchorMax == Vector2.one)
                {
                    passed.Add("safe-area baseline restore");
                }
                else
                {
                    problems.Add("safe-area baseline restore failed");
                }

                UnityEngine.Object.DestroyImmediate(fitter);
                rect.anchorMin = new Vector2(0.4f, 0.9f);
                rect.anchorMax = new Vector2(0.6f, 1f);
                var topTarget = target.AddComponent<SafeAreaFitter>();
                topTarget.Configure(SafeAreaFitter.Edges.Top);
                topTarget.ApplySafeArea(
                    new Rect(24f, 80f, 672f, 1440f),
                    new Vector2Int(720, 1600));
                if (Vector2.Distance(rect.anchorMin,
                        new Vector2(0.4f, 0.85f)) < 0.0001f &&
                    Vector2.Distance(rect.anchorMax,
                        new Vector2(0.6f, 0.95f)) < 0.0001f)
                {
                    passed.Add("fixed top target translation");
                }
                else
                {
                    problems.Add("fixed top target translation differs");
                }

                UnityEngine.Object.DestroyImmediate(topTarget);
                rect.anchorMin = new Vector2(0f, 0.8f);
                rect.anchorMax = new Vector2(1f, 1f);
                var spanningTarget = target.AddComponent<SafeAreaFitter>();
                spanningTarget.Configure(
                    SafeAreaFitter.Edges.Left | SafeAreaFitter.Edges.Right);
                spanningTarget.ApplySafeArea(
                    new Rect(24f, 80f, 672f, 1440f),
                    new Vector2Int(720, 1600));
                if (Vector2.Distance(rect.anchorMin,
                        new Vector2(24f / 720f, 0.8f)) < 0.0001f &&
                    Vector2.Distance(rect.anchorMax,
                        new Vector2(696f / 720f, 1f)) < 0.0001f)
                {
                    passed.Add("spanning target contraction");
                }
                else
                {
                    problems.Add("spanning target contraction differs");
                }
            }
            catch (Exception exception)
            {
                problems.Add("safe-area smoke failed: " +
                    exception.GetType().Name + ": " + exception.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
            return passed.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static void Compare(ICollection<string> problems, string name,
            int actual, int expected)
        {
            if (actual != expected)
            {
                problems.Add(string.Format(
                    "{0} differs: expected {1}, got {2}",
                    name, expected, actual));
            }
        }
    }
}
