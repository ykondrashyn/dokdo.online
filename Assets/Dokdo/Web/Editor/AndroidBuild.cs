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
    public static class AndroidBuild
    {
        public const string ApplicationId = "com.reconstruction.dokdo.standalone";
        public const string Version = "1.16.6-public.1";
        public const int VersionCode = 1160601;
        public const string Output = "Builds/Android/dokdo-standalone.apk";

        // Also usable from the Editor on Windows and macOS. Python's wrapper
        // additionally restores settings and records the source/test hashes.
        [MenuItem("DOKDO/Android/Configure APK build")]
        public static void Configure()
        {
            if (Application.unityVersion != "6000.3.18f1")
                throw new InvalidOperationException("Use Unity 6000.3.18f1.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Install Android Build Support, SDK & NDK Tools and OpenJDK with Unity Hub.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Switch to Android in File > Build Profiles first.");

            var android = NamedBuildTarget.Android;
            PlayerSettings.companyName = "DOKDO Reconstruction";
            PlayerSettings.productName = "DOKDO Standalone";
            PlayerSettings.SetApplicationIdentifier(android, ApplicationId);
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.Android.bundleVersionCode = VersionCode;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(android, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetManagedStrippingLevel(android, ManagedStrippingLevel.Low);
            PlayerSettings.SetScriptingDefineSymbols(android, "DOKDO_STANDALONE");
            StandaloneBuild.SetPortableCompilerPaths();
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.runInBackground = false;
            PlayerSettings.Android.splitApplicationBinary = false;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.keystoreName = "";
            PlayerSettings.Android.keyaliasName = "";
            PlayerSettings.Android.keystorePass = "";
            PlayerSettings.Android.keyaliasPass = "";
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ETC2;
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Disabled;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorBuildSettings.scenes = SceneIds.BuildPaths
                .Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
        }

        [MenuItem("DOKDO/Android/Build local APK")]
        public static void Build()
        {
            if (File.Exists(Output))
                throw new InvalidOperationException("Move the previous APK out of Builds/Android before rebuilding.");
            Configure();
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = SceneIds.BuildPaths.ToArray(), target = BuildTarget.Android,
                subtarget = (int)MobileTextureSubtarget.ETC2,
                locationPathName = Output, options = BuildOptions.NoUniqueIdentifier
            });
            if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0)
            {
                StandaloneBuild.InspectReport();
                throw new InvalidOperationException("Android build failed: " + report.summary.result);
            }
            Debug.Log("DOKDO_STANDALONE_BUILD_OK Android");
        }
    }
}
