#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dokdo
{
    public sealed class RuntimeSmokeRunner : MonoBehaviour
    {
        public const string CommandLineFlag = "-dokdo-runtime-smoke";
        public const string SuccessMarker = "DOKDO_RUNTIME_SMOKE passed";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallFromCommandLine()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), CommandLineFlag) < 0)
            {
                return;
            }

            var host = new GameObject(nameof(RuntimeSmokeRunner));
            DontDestroyOnLoad(host);
            host.AddComponent<RuntimeSmokeRunner>();
        }

        private IEnumerator Start()
        {
            foreach (var sceneId in SceneIds.BuildOrder)
            {
                var operation = SceneManager.LoadSceneAsync(sceneId, LoadSceneMode.Single);
                if (operation == null)
                {
                    throw new InvalidOperationException($"Could not load scene '{sceneId}'.");
                }

                while (!operation.isDone)
                {
                    yield return null;
                }

                yield return null;
                if (SceneManager.GetActiveScene().name != sceneId)
                {
                    throw new InvalidOperationException(
                        $"Expected active scene '{sceneId}', got " +
                        $"'{SceneManager.GetActiveScene().name}'.");
                }
            }

            Debug.Log(SuccessMarker);
            Destroy(gameObject);
        }
    }
}
#endif
