using System;

namespace Dokdo.Web
{
    public interface IBrowserKeyValueStore
    {
        bool IsAvailable { get; }
        bool TryGet(string key, out string value);
        void Set(string key, string value);
        void Remove(string key);
    }

    public enum BrowserStorageError
    {
        Unavailable,
        QuotaExceeded,
        Security,
        Unknown
    }

    public sealed class BrowserStorageException : Exception
    {
        public BrowserStorageException(
            BrowserStorageError error,
            string message) : base(message)
        {
            Error = error;
        }

        public BrowserStorageError Error { get; }
    }
}
