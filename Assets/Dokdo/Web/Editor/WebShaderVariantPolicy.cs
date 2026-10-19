using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dokdo.Web.Editor
{
    /// <summary>
    /// Excludes two recovered soft-particle variants whose Unity 5 macro
    /// expands to an undeclared vertex identifier in Unity 6 WebGL GLSL.
    /// The non-soft variants preserve the shaders instead of replacing their
    /// materials or recovered source assets.
    /// </summary>
    public sealed class WebShaderVariantPolicy : IPreprocessShaders
    {
        private static readonly HashSet<string> AffectedShaders = new()
        {
            "Cartoon FX/Alpha Blended + Additive Soft",
            "Hidden/Cartoon FX/Particles"
        };

        public int callbackOrder => 0;

        public void OnProcessShader(
            Shader shader,
            ShaderSnippetData snippet,
            IList<ShaderCompilerData> data)
        {
            if (EditorUserBuildSettings.activeBuildTarget !=
                    BuildTarget.WebGL ||
                !AffectedShaders.Contains(shader.name))
            {
                return;
            }

            var softParticles = new ShaderKeyword(
                shader, "SOFTPARTICLES_ON");
            for (var index = data.Count - 1; index >= 0; index--)
            {
                if (data[index].shaderKeywordSet.IsEnabled(softParticles))
                {
                    data.RemoveAt(index);
                }
            }
        }
    }
}
