using System.Runtime.InteropServices;
using Dokdo;
using UnityEngine;

namespace Dokdo.Web
{
    public static class BrowserSaveComposition
    {
        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SaveRuntime.Store = new BrowserSaveStore(
                new BrowserLocalStorage(), ShowWarning);
#endif
        }

        private static void ShowWarning(string message)
        {
            Debug.LogWarning(message);
#if UNITY_WEBGL && !UNITY_EDITOR
            DokdoBrowserWarning(message);
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void DokdoBrowserWarning(string message);
#endif
    }
}
