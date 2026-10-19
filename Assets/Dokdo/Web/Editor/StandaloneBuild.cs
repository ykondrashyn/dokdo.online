using System;
using System.IO;
using System.Linq;
using Dokdo;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dokdo.Web.Editor
{
    public static class StandaloneBuild
    {
        public static void Compile() => Debug.Log("DOKDO_STANDALONE_COMPILE_OK");
        public static void Desktop() => Build("Desktop", WebGLTextureSubtarget.DXT);
        public static void Mobile() => Build("Mobile", WebGLTextureSubtarget.ASTC);
        public static void InspectReport()
        {
            var report = BuildReport.GetLatestReport();
            if (report == null) throw new InvalidOperationException("No saved build report");
            Debug.Log("DOKDO_BUILD_REPORT result=" + report.summary.result +
                " errors=" + report.summary.totalErrors + " warnings=" + report.summary.totalWarnings);
            foreach (var step in report.steps)
                foreach (var message in step.messages)
                    if (message.type == LogType.Error || message.type == LogType.Exception)
                        Debug.Log("DOKDO_BUILD_REPORT_DETAIL " + step.name + ": " + message.content);
        }
        private static void Build(string name, WebGLTextureSubtarget textures)
        {
            var web = NamedBuildTarget.WebGL;
            PlayerSettings.companyName = "DOKDO Reconstruction";
            PlayerSettings.productName = "DOKDO Standalone";
            PlayerSettings.bundleVersion = "1.16.6-web9-r6-public.1";
            PlayerSettings.runInBackground = false;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.SetScriptingBackend(web, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(web, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetManagedStrippingLevel(web, ManagedStrippingLevel.Low);
            PlayerSettings.SetScriptingDefineSymbols(web, "DOKDO_STANDALONE");
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.WebGL.template = "PROJECT:DokdoStandalone";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.dataCaching = false;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.initialMemorySize = WebBuildConstants.InitialMemoryMiB;
            PlayerSettings.WebGL.maximumMemorySize = WebBuildConstants.MaximumMemoryMiB;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.geometricMemoryGrowthStep = WebBuildConstants.MemoryGrowthFactor;
            PlayerSettings.WebGL.memoryGeometricGrowthCap = WebBuildConstants.MemoryGrowthCapMiB;
            PlayerSettings.WebGL.wasm2023 = true;
            PlayerSettings.WebGL.webAssemblyBigInt = true;
            PlayerSettings.WebGL.webAssemblyTable = true;
            foreach (LogType kind in Enum.GetValues(typeof(LogType)))
                PlayerSettings.SetStackTraceLogType(kind, StackTraceLogType.None);
            EditorUserBuildSettings.webGLBuildSubtarget = textures;
            EditorUserBuildSettings.SetPlatformSettings("WebGL", "CodeOptimization", "DiskSizeLTO");
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.connectProfiler = false;
            WebAudioBuildAssets.EnsureImported();
            var problems = new System.Collections.Generic.List<string>();
            WebAudioBuildAssets.Audit(problems);
            if (problems.Count > 0) throw new InvalidOperationException(string.Join("\n", problems));
            var output = "Builds/Standalone/" + name;
            if (Directory.Exists(output)) Directory.Delete(output, true);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = SceneIds.BuildPaths.ToArray(), target = BuildTarget.WebGL,
                locationPathName = output, options = BuildOptions.NoUniqueIdentifier
            });
            if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0)
            {
                InspectReport();
                throw new InvalidOperationException("Standalone build failed: " + report.summary.result);
            }
            Debug.Log("DOKDO_STANDALONE_BUILD_OK " + name);
        }
    }
}
