using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dokdo;
using NUnit.Framework;

namespace Dokdo.Web.Tests
{
    public sealed class BrowserSaveStoreTests
    {
        private static readonly DateTime SaveTime =
            new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void FreshSaveSurvivesStoreRecreation()
        {
            var storage = new FakeBrowserKeyValueStore();
            var firstStore = new BrowserSaveStore(storage);
            var data = SaveDataV1.CreateFresh("English", SaveTime);
            data.resources[0] = 817;
            firstStore.Save(data);

            var reloadedStore = new BrowserSaveStore(storage);
            Assert.That(reloadedStore.TryLoad(
                out var loaded, out var error), Is.True, error);
            Assert.That(loaded.resources[0], Is.EqualTo(817));
        }

        [Test]
        public void ValidBackupRecoversCorruptPrimary()
        {
            var storage = new FakeBrowserKeyValueStore();
            var store = new BrowserSaveStore(storage);
            var first = SaveDataV1.CreateFresh("English", SaveTime);
            first.resources[0] = 111;
            store.Save(first);
            var second = SaveDataV1.CreateFresh("English", SaveTime);
            second.resources[0] = 222;
            store.Save(second);
            storage.SetRaw(WebBuildConstants.PrimarySaveKey, "corrupt");

            Assert.That(store.TryLoad(out var recovered, out var error),
                Is.True, error);
            Assert.That(recovered.resources[0], Is.EqualTo(111));
            Assert.That(error, Does.Contain("backup"));
            Assert.That(JsonFileSaveStore.TryReadText(
                storage.GetRaw(WebBuildConstants.PrimarySaveKey),
                out var restored, out _), Is.True);
            Assert.That(restored.resources[0], Is.EqualTo(111));
        }

        [Test]
        public void CorruptionIsRejectedWithoutRotatingIt()
        {
            var storage = new FakeBrowserKeyValueStore();
            storage.SetRaw(WebBuildConstants.PrimarySaveKey, "corrupt");
            var store = new BrowserSaveStore(storage);

            Assert.That(store.TryLoad(out _, out var error), Is.False);
            Assert.That(error, Does.Contain("Primary"));

            var fresh = SaveDataV1.CreateFresh("English", SaveTime);
            store.Save(fresh);
            Assert.That(storage.Contains(WebBuildConstants.BackupSaveKey),
                Is.False, "a corrupt primary must never become the backup");
        }

        [Test]
        public void QuotaFailureFallsBackAndWarnsOnce()
        {
            var storage = new FakeBrowserKeyValueStore
            {
                SetError = BrowserStorageError.QuotaExceeded
            };
            var warnings = new List<string>();
            var store = new BrowserSaveStore(storage, warnings.Add);
            var data = SaveDataV1.CreateFresh("English", SaveTime);
            data.revivalCount = 19;

            Assert.DoesNotThrow(() => store.Save(data));
            Assert.That(store.IsUsingSessionFallback, Is.True);
            Assert.That(store.TryLoad(out var loaded, out var error),
                Is.True, error);
            Assert.That(loaded.revivalCount, Is.EqualTo(19));
            Assert.That(warnings, Has.Count.EqualTo(1));
            store.Save(data);
            Assert.That(warnings, Has.Count.EqualTo(1));
        }

        [Test]
        public void SecurityDenialStartsInSessionFallback()
        {
            var storage = new FakeBrowserKeyValueStore
            {
                AvailabilityError = BrowserStorageError.Security
            };
            var warnings = new List<string>();
            var store = new BrowserSaveStore(storage, warnings.Add);

            store.Save(SaveDataV1.CreateFresh("English", SaveTime));

            Assert.That(store.IsUsingSessionFallback, Is.True);
            Assert.That(warnings.Single(), Does.Contain("closing the tab"));
            Assert.That(store.TryLoad(out _, out var error), Is.True, error);
        }

        [TestCase(BrowserStorageError.QuotaExceeded)]
        [TestCase(BrowserStorageError.Security)]
        [TestCase(BrowserStorageError.Unknown)]
        public void RewardWriteFailureRetainsBalanceAndReceiptForSessionExport(
            BrowserStorageError failure)
        {
            const string hunt = "0123456789abcdef";
            var storage = new FakeBrowserKeyValueStore();
            var warnings = new List<string>();
            var store = new BrowserSaveStore(storage, warnings.Add);
            var before = SaveDataV1.CreateFresh("English", SaveTime);
            before.resources[5] = 12;
            store.Save(before);
            storage.SetError = failure;

            Assert.That(store.GrantTreasure(hunt, 317, before, out var rewarded),
                Is.EqualTo(TreasureRewardResult.Granted));
            Assert.That(store.IsUsingSessionFallback, Is.True);
            Assert.That(rewarded.resources[5], Is.EqualTo(329));
            Assert.That(store.GrantTreasure(hunt, 317, rewarded, out _),
                Is.EqualTo(TreasureRewardResult.AlreadyClaimed));
            store.Save(rewarded);
            Assert.That(warnings, Has.Count.EqualTo(1));
            Assert.That(warnings[0], Does.Contain("closing the tab"));

            // The failed write leaves the previous durable balance intact.
            storage.SetError = null;
            var reloaded = new BrowserSaveStore(storage);
            Assert.That(reloaded.TryLoad(out var durable, out _), Is.True);
            Assert.That(durable.resources[5], Is.EqualTo(12));
            Assert.That(reloaded.TryImport(store.Export(), out var imported, out var error),
                Is.True, error);
            Assert.That(imported.resources[5], Is.EqualTo(329));
            Assert.That(reloaded.GrantTreasure(hunt, 317, imported, out _),
                Is.EqualTo(TreasureRewardResult.AlreadyClaimed));
        }

        [Test]
        public void ExportImportResetRemainAndroidCompatible()
        {
            var source = new JsonFileSaveStore(Path.Combine(
                Path.GetTempPath(), "dokdo-web-compat-" +
                Guid.NewGuid().ToString("N")));
            try
            {
                var data = SaveDataV1.CreateFresh("Japanese", SaveTime);
                data.bestArenaKill = 41;
                source.Save(data);
                var androidEnvelope = source.Export();

                var storage = new FakeBrowserKeyValueStore();
                var browser = new BrowserSaveStore(storage);
                Assert.That(browser.TryImport(androidEnvelope,
                    out var imported, out var error), Is.True, error);
                Assert.That(imported.bestArenaKill, Is.EqualTo(41));
                Assert.That(JsonFileSaveStore.TryReadText(browser.Export(),
                    out var exported, out error), Is.True, error);
                Assert.That(exported.language, Is.EqualTo("Japanese"));

                browser.Reset();
                Assert.That(storage.Contains(
                    WebBuildConstants.PrimarySaveKey), Is.False);
                Assert.That(storage.Contains(
                    WebBuildConstants.BackupSaveKey), Is.False);
            }
            finally
            {
                if (Directory.Exists(source.PrimaryPath.Replace(
                        JsonFileSaveStore.FileName, string.Empty)))
                {
                    Directory.Delete(Path.GetDirectoryName(
                        source.PrimaryPath), true);
                }
            }
        }

        [Test]
        public void OversizeEnvelopeFailsBeforeStorageWrite()
        {
            var storage = new FakeBrowserKeyValueStore();
            var store = new BrowserSaveStore(storage);

            Assert.That(store.TryImport(
                new string('x', WebBuildConstants.MaximumSaveBytes + 1),
                out _, out var error), Is.False);
            Assert.That(error, Does.Contain("256 KiB"));
            Assert.That(storage.Count, Is.Zero);

            store.Save(SaveDataV1.CreateFresh("English", SaveTime));
            Assert.That(System.Text.Encoding.UTF8.GetByteCount(
                    store.Export()),
                Is.LessThan(WebBuildConstants.MaximumSaveBytes));
        }

        private sealed class FakeBrowserKeyValueStore :
            IBrowserKeyValueStore
        {
            private readonly Dictionary<string, string> values = new();

            public BrowserStorageError? AvailabilityError { get; set; }
            public BrowserStorageError? GetError { get; set; }
            public BrowserStorageError? SetError { get; set; }
            public BrowserStorageError? RemoveError { get; set; }
            public int Count => values.Count;

            public bool IsAvailable
            {
                get
                {
                    ThrowIfSet(AvailabilityError);
                    return true;
                }
            }

            public bool TryGet(string key, out string value)
            {
                ThrowIfSet(GetError);
                return values.TryGetValue(key, out value);
            }

            public void Set(string key, string value)
            {
                ThrowIfSet(SetError);
                values[key] = value;
            }

            public void Remove(string key)
            {
                ThrowIfSet(RemoveError);
                values.Remove(key);
            }

            public void SetRaw(string key, string value)
            {
                values[key] = value;
            }

            public string GetRaw(string key)
            {
                return values[key];
            }

            public bool Contains(string key)
            {
                return values.ContainsKey(key);
            }

            private static void ThrowIfSet(BrowserStorageError? error)
            {
                if (error.HasValue)
                {
                    throw new BrowserStorageException(
                        error.Value, "Injected browser storage failure.");
                }
            }
        }
    }
}
