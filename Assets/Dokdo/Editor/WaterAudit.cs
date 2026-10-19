using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.Water;

namespace Dokdo.Editor
{
    public static class WaterAudit
    {
        private const string ShaderName = "FX/Water";
        private const string MaterialPath =
            "Assets/Material/WaterProDaytime.mat";

        private static readonly string[] ScenePaths =
        {
            "Assets/0.my/Scenes/Intro.unity",
            "Assets/0.my/Scenes/Main.unity"
        };

        [Serializable]
        private sealed class AuditReport
        {
            public int schemaVersion = 1;
            public string unityVersion;
            public string shaderPath;
            public bool shaderSupported;
            public int shaderPasses;
            public string[] shaderCompilerErrors;
            public string materialPath;
            public TextureReport[] textures;
            public SceneReport[] scenes;
            public string[] smokeChecks;
            public string[] problems;
        }

        [Serializable]
        private sealed class TextureReport
        {
            public string property;
            public string path;
        }

        [Serializable]
        private sealed class SceneReport
        {
            public string path;
            public int waterComponents;
            public int infiniteWaterComponents;
            public int serializedTextureSize;
            public int effectiveTextureSize;
        }

        public static void Run()
        {
            var problems = new List<string>();
            var shader = Shader.Find(ShaderName);
            var errors = Array.Empty<string>();
            var shaderPath = string.Empty;
            if (shader == null)
            {
                problems.Add("water shader is missing");
            }
            else
            {
                shaderPath = AssetDatabase.GetAssetPath(shader);
                errors = ShaderUtil.GetShaderMessages(shader)
                    .Where(message => string.Equals(
                        message.severity.ToString(), "Error",
                        StringComparison.OrdinalIgnoreCase))
                    .Select(message => message.message)
                    .OrderBy(message => message, StringComparer.Ordinal)
                    .ToArray();
                if (!shader.isSupported)
                {
                    problems.Add("water shader is unsupported");
                }
                if (shader.passCount == 0)
                {
                    problems.Add("water shader has no passes");
                }
                if (errors.Length != 0)
                {
                    problems.Add("water shader has compiler errors");
                }
                if (File.ReadAllText(shaderPath).Contains(
                    "DummyShaderTextExporter"))
                {
                    problems.Add("water placeholder remains");
                }
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            var textureReports = InspectMaterial(material, shader, problems);
            var sceneReports = ScenePaths.Select(
                    path => InspectScene(path, material, problems))
                .ToArray();
            var smokeChecks = RunSmokeChecks(material, problems);

            var report = new AuditReport
            {
                unityVersion = Application.unityVersion,
                shaderPath = shaderPath,
                shaderSupported = shader != null && shader.isSupported,
                shaderPasses = shader == null ? 0 : shader.passCount,
                shaderCompilerErrors = errors,
                materialPath = MaterialPath,
                textures = textureReports,
                scenes = sceneReports,
                smokeChecks = smokeChecks,
                problems = problems.OrderBy(
                        problem => problem, StringComparer.Ordinal)
                    .ToArray()
            };

            var outputPath = Path.GetFullPath(Path.Combine(
                Application.dataPath, "../.artifacts/unity-water-audit.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, JsonUtility.ToJson(report, true) + "\n");

            Debug.Log(string.Format(
                "DOKDO_WATER_AUDIT shader={0} scenes={1} smoke={2} " +
                "problems={3} report={4}",
                report.shaderSupported, sceneReports.Length,
                smokeChecks.Length, problems.Count, outputPath));

            if (problems.Count != 0)
            {
                throw new InvalidOperationException(
                    "DOKDO water audit failed:\n" +
                    string.Join("\n", problems));
            }
        }

        private static TextureReport[] InspectMaterial(Material material,
            Shader shader, ICollection<string> problems)
        {
            if (material == null)
            {
                problems.Add("water material is missing");
                return Array.Empty<TextureReport>();
            }
            if (material.shader != shader)
            {
                problems.Add("water material shader differs");
            }

            var properties = new[]
            {
                "_BumpMap", "_Fresnel", "_ReflectiveColor", "_FoamTex"
            };
            var rows = new List<TextureReport>();
            foreach (var property in properties)
            {
                var texture = material.HasProperty(property)
                    ? material.GetTexture(property)
                    : null;
                if (texture == null)
                {
                    problems.Add("water texture is missing: " + property);
                }
                rows.Add(new TextureReport
                {
                    property = property,
                    path = texture == null
                        ? null
                        : AssetDatabase.GetAssetPath(texture)
                });
            }
            if (!material.HasProperty("_HorizonColor") ||
                !material.HasProperty("_WaterAlpha"))
            {
                problems.Add("water color/transparency properties are missing");
            }
            return rows.ToArray();
        }

        private static SceneReport InspectScene(string path, Material material,
            ICollection<string> problems)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var waters = Components<Water>(roots);
            var infiniteWaters = Components<InfiniteWater>(roots);
            Compare(problems, path + " water count", waters.Length, 1);
            Compare(problems, path + " infinite-water count",
                infiniteWaters.Length, path.EndsWith("Main.unity") ? 1 : 0);

            foreach (var water in waters)
            {
                Compare(problems, path + " serialized texture size",
                    water.textureSize, 1024);
                Compare(problems, path + " effective texture size",
                    water.EffectiveTextureSize, 256);
                if (!water.allowPlanarReflection ||
                    !water.allowReflectionOnMobile)
                {
                    problems.Add(path + " unexpectedly forces fallback");
                }
                var renderer = water.GetComponent<Renderer>();
                if (renderer == null || renderer.sharedMaterial != material)
                {
                    problems.Add(path + " water material differs");
                }
            }

            return new SceneReport
            {
                path = path,
                waterComponents = waters.Length,
                infiniteWaterComponents = infiniteWaters.Length,
                serializedTextureSize = waters.Length == 0
                    ? 0
                    : waters[0].textureSize,
                effectiveTextureSize = waters.Length == 0
                    ? 0
                    : waters[0].EffectiveTextureSize
            };
        }

        private static string[] RunSmokeChecks(Material sourceMaterial,
            ICollection<string> problems)
        {
            var passed = new List<string>();
            GameObject waterObject = null;
            GameObject shipObject = null;
            Material material = null;
            try
            {
                waterObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
                shipObject = new GameObject("DOKDO Water Audit Ship");
                material = new Material(sourceMaterial);
                waterObject.GetComponent<Renderer>().sharedMaterial = material;

                var water = waterObject.AddComponent<Water>();
                water.textureSize = 1024;
                water.maximumTextureSize = 256;
                if (water.EffectiveTextureSize == 256)
                {
                    passed.Add("reflection size cap");
                }
                else
                {
                    problems.Add("reflection size cap failed");
                }

                water.SetPlanarReflectionEnabled(false);
                if (water.IsUsingSimpleFallback)
                {
                    passed.Add("reflection-disabled fallback");
                }
                else
                {
                    problems.Add("reflection-disabled fallback failed");
                }

                var infinite = waterObject.AddComponent<InfiniteWater>();
                infinite.myShip = shipObject.transform;
                shipObject.transform.position = new Vector3(10f, 3f, -4f);
                infinite.RecenterIfNeeded();
                if (Vector3.Distance(waterObject.transform.position,
                    new Vector3(10f, 0f, -4f)) < 0.0001f)
                {
                    passed.Add("infinite recenter");
                }
                else
                {
                    problems.Add("infinite recenter failed");
                }
            }
            catch (Exception exception)
            {
                problems.Add("water smoke failed: " +
                    exception.GetType().Name + ": " + exception.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(waterObject);
                UnityEngine.Object.DestroyImmediate(shipObject);
                UnityEngine.Object.DestroyImmediate(material);
            }
            return passed.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static T[] Components<T>(IEnumerable<GameObject> roots)
            where T : Component
        {
            return roots.SelectMany(
                    root => root.GetComponentsInChildren<T>(true))
                .ToArray();
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
