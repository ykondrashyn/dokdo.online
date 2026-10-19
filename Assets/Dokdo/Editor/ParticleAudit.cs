using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Dokdo.Editor
{
    public static class ParticleAudit
    {
        private static readonly string[] ExpectedShaderNames =
        {
            "Cartoon FX/Alpha Blended + Additive Soft",
            "Cartoon FX/Particles Additive Alpha8",
            "Hidden/Cartoon FX/Particles"
        };

        [Serializable]
        private sealed class AuditReport
        {
            public int schemaVersion = 1;
            public string unityVersion;
            public int materialCount;
            public ShaderReport[] shaders;
            public MaterialReport[] customMaterials;
            public string[] problems;
        }

        [Serializable]
        private sealed class ShaderReport
        {
            public string name;
            public string path;
            public bool supported;
            public int passCount;
            public string[] compilerErrors;
        }

        [Serializable]
        private sealed class MaterialReport
        {
            public string path;
            public string shader;
            public string mainTexture;
        }

        public static void Run()
        {
            var problems = new List<string>();
            var shaderReports = new List<ShaderReport>();
            var customShaders = new Dictionary<string, Shader>(
                StringComparer.Ordinal);

            foreach (var expectedName in ExpectedShaderNames)
            {
                var shader = Shader.Find(expectedName);
                if (shader == null)
                {
                    problems.Add("shader is missing: " + expectedName);
                    continue;
                }

                customShaders.Add(expectedName, shader);
                var path = AssetDatabase.GetAssetPath(shader);
                var errors = ShaderUtil.GetShaderMessages(shader)
                    .Where(message => string.Equals(
                        message.severity.ToString(), "Error",
                        StringComparison.OrdinalIgnoreCase))
                    .Select(message => message.message)
                    .OrderBy(message => message, StringComparer.Ordinal)
                    .ToArray();

                if (!shader.isSupported)
                {
                    problems.Add("shader is unsupported: " + expectedName);
                }
                if (shader.passCount == 0)
                {
                    problems.Add("shader has no passes: " + expectedName);
                }
                if (errors.Length != 0)
                {
                    problems.Add("shader has compiler errors: " + expectedName);
                }
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    problems.Add("shader asset path is invalid: " + expectedName);
                }
                else if (File.ReadAllText(path).Contains(
                    "DummyShaderTextExporter"))
                {
                    problems.Add("placeholder shader remains: " + expectedName);
                }

                RequireProperty(shader, expectedName, "_MainTex", problems);
                if (expectedName != "Cartoon FX/Particles Additive Alpha8")
                {
                    RequireProperty(shader, expectedName, "_BaseColor", problems);
                    RequireProperty(shader, expectedName, "_TintColor", problems);
                    RequireProperty(shader, expectedName, "_InvFade", problems);
                }

                shaderReports.Add(new ShaderReport
                {
                    name = expectedName,
                    path = path,
                    supported = shader.isSupported,
                    passCount = shader.passCount,
                    compilerErrors = errors
                });
            }

            var errorShader = Shader.Find("Hidden/InternalErrorShader");
            var materialPaths = AssetDatabase.FindAssets(
                    "t:Material", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            var customMaterials = new List<MaterialReport>();

            foreach (var path in materialPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    problems.Add("material could not be loaded: " + path);
                    continue;
                }
                if (material.shader == null || material.shader == errorShader)
                {
                    problems.Add("material uses an error shader: " + path);
                    continue;
                }
                if (!customShaders.ContainsKey(material.shader.name))
                {
                    continue;
                }

                var texture = material.HasProperty("_MainTex")
                    ? material.GetTexture("_MainTex")
                    : null;
                if (texture == null)
                {
                    problems.Add("custom particle material has no texture: " + path);
                }
                customMaterials.Add(new MaterialReport
                {
                    path = path,
                    shader = material.shader.name,
                    mainTexture = texture == null
                        ? null
                        : AssetDatabase.GetAssetPath(texture)
                });
            }

            Compare(problems, "custom particle material count",
                customMaterials.Count, 5);
            Compare(problems, "hybrid particle material count",
                customMaterials.Count(material => material.shader ==
                    "Cartoon FX/Alpha Blended + Additive Soft"), 1);
            Compare(problems, "Alpha8 particle material count",
                customMaterials.Count(material => material.shader ==
                    "Cartoon FX/Particles Additive Alpha8"), 4);

            var report = new AuditReport
            {
                unityVersion = Application.unityVersion,
                materialCount = materialPaths.Length,
                shaders = shaderReports.OrderBy(
                        shader => shader.name, StringComparer.Ordinal)
                    .ToArray(),
                customMaterials = customMaterials.ToArray(),
                problems = problems.OrderBy(
                        problem => problem, StringComparer.Ordinal)
                    .ToArray()
            };

            var outputPath = Path.GetFullPath(Path.Combine(
                Application.dataPath, "../.artifacts/unity-particle-audit.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, JsonUtility.ToJson(report, true) + "\n");

            Debug.Log(string.Format(
                "DOKDO_PARTICLE_AUDIT shaders={0} materials={1} " +
                "custom={2} problems={3} report={4}",
                shaderReports.Count, materialPaths.Length, customMaterials.Count,
                problems.Count, outputPath));

            if (problems.Count != 0)
            {
                throw new InvalidOperationException(
                    "DOKDO particle audit failed:\n" +
                    string.Join("\n", problems));
            }
        }

        private static void RequireProperty(Shader shader, string shaderName,
            string propertyName, ICollection<string> problems)
        {
            if (shader.FindPropertyIndex(propertyName) < 0)
            {
                problems.Add(string.Format(
                    "shader property is missing: {0} {1}",
                    shaderName, propertyName));
            }
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
