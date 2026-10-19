using System;
using System.Runtime.InteropServices;

namespace Dokdo.Web
{
    public sealed class BrowserLocalStorage : IBrowserKeyValueStore
    {
        public bool IsAvailable
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                var result = DokdoStorageAvailable();
                if (result == 1)
                {
                    return true;
                }
                if (result == 0)
                {
                    return false;
                }
                throw CreateException(DokdoStorageLastError());
#else
                return false;
#endif
            }
        }

        public bool TryGet(string key, out string value)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var pointer = DokdoStorageGet(key);
            var error = DokdoStorageLastError();
            if (error != 0)
            {
                throw CreateException(error);
            }
            if (pointer == IntPtr.Zero)
            {
                value = null;
                return false;
            }
            value = Marshal.PtrToStringUTF8(pointer);
            DokdoStorageFree(pointer);
            return true;
#else
            throw new BrowserStorageException(
                BrowserStorageError.Unavailable,
                "Browser localStorage is available only in a Web player.");
#endif
        }

        public void Set(string key, string value)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var result = DokdoStorageSet(key, value);
            if (result != 0)
            {
                throw CreateException(result);
            }
#else
            throw new BrowserStorageException(
                BrowserStorageError.Unavailable,
                "Browser localStorage is available only in a Web player.");
#endif
        }

        public void Remove(string key)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var result = DokdoStorageRemove(key);
            if (result != 0)
            {
                throw CreateException(result);
            }
#else
            throw new BrowserStorageException(
                BrowserStorageError.Unavailable,
                "Browser localStorage is available only in a Web player.");
#endif
        }

        private static BrowserStorageException CreateException(int code)
        {
            var error = code switch
            {
                1 => BrowserStorageError.QuotaExceeded,
                2 => BrowserStorageError.Security,
                3 => BrowserStorageError.Unavailable,
                _ => BrowserStorageError.Unknown
            };
            return new BrowserStorageException(
                error, "Browser localStorage failed: " + error + ".");
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int DokdoStorageAvailable();

        [DllImport("__Internal")]
        private static extern IntPtr DokdoStorageGet(string key);

        [DllImport("__Internal")]
        private static extern void DokdoStorageFree(IntPtr pointer);

        [DllImport("__Internal")]
        private static extern int DokdoStorageSet(string key, string value);

        [DllImport("__Internal")]
        private static extern int DokdoStorageRemove(string key);

        [DllImport("__Internal")]
        private static extern int DokdoStorageLastError();
#endif
    }
}
