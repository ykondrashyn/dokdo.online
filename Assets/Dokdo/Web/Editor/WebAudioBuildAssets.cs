using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Dokdo.Web.Editor
{
    public static class WebAudioBuildAssets
    {
        private const string AudioDirectory = "Assets/AudioClip";
        public const string AdventureThemePath =
            "Assets/Dokdo/Web/Runtime/Resources/Audio/" +
            "bgm_Adventure_Theme.ogg";
        public const string AdventureThemeSha256 =
            "6682762d0f8565ddf6556ba34a0db0eb8918cbee9964470cf802b2c49e6cacbd";
        public const int ExpectedClipCount = 19;
        private const string MixerPath =
            "Assets/AudioMixerController/AudioManager.mixer";
        private static readonly string[] SupportedExtensions =
        {
            ".aif",
            ".aiff",
            ".mp3",
            ".ogg",
            ".wav"
        };
        private static readonly string[] ReferenceAssetPaths =
        {
            "Assets/0.my/Scenes/load.unity",
            "Assets/0.my/Scenes/Intro.unity",
            "Assets/0.my/Scenes/Main.unity",
            "Assets/GameObject/CrashEffect.prefab",
            "Assets/GameObject/Enemy.prefab",
            "Assets/GameObject/ShipBoom2.prefab",
            "Assets/GameObject/WaterDrop.prefab",
            "Assets/GameObject/boom1.prefab"
        };

        public static string[] SourcePaths()
        {
            return Directory.EnumerateFiles(AudioDirectory)
                .Where(path => SupportedExtensions.Contains(
                    Path.GetExtension(path),
                    StringComparer.OrdinalIgnoreCase))
                .Append(AdventureThemePath)
                .Select(path => path.Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        public static int EnsureImported()
        {
            string[] paths = SourcePaths();
            int reimported = 0;
            foreach (string path in paths)
            {
                if (LoadClip(path) != null)
                {
                    continue;
                }
                AssetDatabase.ImportAsset(path,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
                reimported++;
            }

            if (reimported > 0)
            {
                ReimportReferenceAssets();
            }

            string[] missing = MissingPaths(paths);
            if (missing.Length > 0)
            {
                throw new InvalidOperationException(
                    "Web audio import failed. Confirm that ffmpeg with the " +
                    "AAC encoder is available on PATH, then reimport:\n- " +
                    string.Join("\n- ", missing));
            }

            Debug.Log("DOKDO_WEB_AUDIO assets=" + paths.Length +
                " reimported=" + reimported + " result=imported");
            return reimported;
        }

        public static void ReimportAll()
        {
            foreach (string path in SourcePaths())
            {
                ForceReimport(path);
            }
            ReimportReferenceAssets();
            EnsureImported();
            Debug.Log("DOKDO_WEB_AUDIO result=force-reimported");
        }

        public static void Audit(List<string> problems)
        {
            string[] paths = SourcePaths();
            string[] missing = MissingPaths(paths);
            if (paths.Length != ExpectedClipCount)
            {
                problems.Add("expected " + ExpectedClipCount +
                    " audio sources, found " + paths.Length);
            }
            if (missing.Length > 0)
            {
                problems.Add("audio clips are unavailable: " +
                    string.Join(", ", missing));
            }
            foreach (string parameter in new[] { "FxVol", "BgmVol" })
            {
                if (!HasMixerParameter(parameter))
                {
                    problems.Add("audio mixer parameter is unavailable: " +
                        parameter);
                }
            }
            AuditAdventureTheme(problems);
        }

        public static bool HasMixerParameter(string parameter)
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            return mixer != null && mixer.GetFloat(parameter, out _);
        }

        public static AudioMixerGroup BgmMixerGroup()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            return mixer?.FindMatchingGroups("Bgm")
                .SingleOrDefault(group => group.name == "Bgm");
        }

        private static void AuditAdventureTheme(List<string> problems)
        {
            if (!File.Exists(AdventureThemePath))
            {
                problems.Add("Adventure Theme source is unavailable");
                return;
            }
            if (Digest(AdventureThemePath) != AdventureThemeSha256)
            {
                problems.Add("Adventure Theme source digest differs");
            }
            var clip = LoadClip(AdventureThemePath);
            if (clip != null && (clip.frequency != 44100 ||
                clip.channels != 1 ||
                Mathf.Abs(clip.length - 164.257959f) > 0.02f))
            {
                problems.Add("Adventure Theme must decode as 44.1-kHz " +
                    "mono audio lasting 164.258 seconds");
            }
            if (BgmMixerGroup() == null)
            {
                problems.Add("Bgm mixer group is unavailable");
            }
        }

        private static string Digest(string path)
        {
            using var algorithm = SHA256.Create();
            using var stream = File.OpenRead(path);
            return string.Concat(algorithm.ComputeHash(stream)
                .Select(value => value.ToString("x2")));
        }

        private static string[] MissingPaths(IEnumerable<string> paths)
        {
            return paths.Where(path => LoadClip(path) == null)
                .ToArray();
        }

        private static AudioClip LoadClip(string path)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static void ReimportReferenceAssets()
        {
            foreach (string path in ReferenceAssetPaths)
            {
                ForceReimport(path);
            }
        }

        private static void ForceReimport(string path)
        {
            AssetDatabase.ImportAsset(path,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }
    }
}
