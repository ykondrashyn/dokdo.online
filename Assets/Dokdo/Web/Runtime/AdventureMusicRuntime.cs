using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Dokdo.Web
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class AdventureMusicRuntime : MonoBehaviour
    {
        public const string MainSceneName = "Main";
        public const string ClipResourcePath =
            "Audio/bgm_Adventure_Theme";
        public const string MixerGroupName = "Bgm";
        public const float SourceVolume = 0.8f;
        public static bool PlaybackEnabled => false;

        private AudioSource source;
        private bool shouldPlay;
        private bool pausedForGame;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            ConfigureSource(source,
                Resources.Load<AudioClip>(ClipResourcePath));
            SceneManager.sceneLoaded += OnSceneLoaded;
            RefreshScene(SceneManager.GetActiveScene().name);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            if (!shouldPlay || source == null || source.clip == null)
            {
                return;
            }

            EnsureMixerRouting();
            if (Time.timeScale <= 0f)
            {
                if (source.isPlaying)
                {
                    source.Pause();
                }
                pausedForGame = true;
                return;
            }

            if (pausedForGame)
            {
                source.UnPause();
                pausedForGame = false;
            }
            if (!source.isPlaying)
            {
                source.Play();
            }
        }

        public static bool IsMusicScene(string sceneName)
        {
            return string.Equals(sceneName, MainSceneName,
                StringComparison.Ordinal);
        }

        public static void ConfigureSource(AudioSource target,
            AudioClip clip)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            target.playOnAwake = false;
            target.loop = true;
            target.spatialBlend = 0f;
            target.volume = SourceVolume;
            target.ignoreListenerPause = false;
            target.clip = clip;
        }

        public void RefreshScene(string sceneName)
        {
            shouldPlay = IsMusicScene(sceneName);
            pausedForGame = false;
            if (!shouldPlay)
            {
                source.Stop();
                return;
            }

            EnsureMixerRouting();
            if (source.clip != null && Time.timeScale > 0f)
            {
                source.Play();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshScene(scene.name);
        }

        private void EnsureMixerRouting()
        {
            if (source.outputAudioMixerGroup != null)
            {
                return;
            }
            source.outputAudioMixerGroup = FindBgmGroup();
        }

        private static AudioMixerGroup FindBgmGroup()
        {
            var gameManagerType = Type.GetType(
                "GameManager, Assembly-CSharp", false);
            var instanceField = gameManagerType?.GetField(
                "Instance", BindingFlags.Public | BindingFlags.Static);
            var mixerField = gameManagerType?.GetField(
                "Mixer", BindingFlags.Public | BindingFlags.Instance);
            var gameManager = instanceField?.GetValue(null);
            var mixer = gameManager == null
                ? null
                : mixerField?.GetValue(gameManager) as AudioMixer;
            if (mixer == null)
            {
                return null;
            }

            foreach (var group in mixer.FindMatchingGroups(MixerGroupName))
            {
                if (group.name == MixerGroupName)
                {
                    return group;
                }
            }
            return null;
        }
    }
}
