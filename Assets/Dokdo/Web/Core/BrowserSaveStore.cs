using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Dokdo;

namespace Dokdo.Web
{
    public sealed class BrowserSaveStore : ISaveStore
    {
        private const string SessionWarning =
            "Persistent browser storage is unavailable. Progress will be " +
            "kept only for this session; closing the tab will lose it.";

        private readonly IBrowserKeyValueStore persistentStore;
        private readonly MemoryKeyValueStore sessionStore = new();
        private readonly Action<string> warningSink;
        private bool sessionFallback;
        private bool warningShown;
        private string[] treasureClaims = Array.Empty<string>();
        private bool conflictWarningShown;
        public string PendingTreasureApplication { get; private set; }

        public BrowserSaveStore(
            IBrowserKeyValueStore persistentStore,
            Action<string> warningSink = null)
        {
            this.persistentStore = persistentStore ??
                throw new ArgumentNullException(nameof(persistentStore));
            this.warningSink = warningSink;
            try
            {
                if (!persistentStore.IsAvailable)
                {
                    SwitchToSession(BrowserStorageError.Unavailable);
                }
            }
            catch (BrowserStorageException exception)
            {
                SwitchToSession(exception.Error);
            }
            catch (Exception)
            {
                SwitchToSession(BrowserStorageError.Unknown);
            }
        }

        public bool IsUsingSessionFallback => sessionFallback;

        public bool TryLoad(out SaveDataV1 data, out string error)
        {
            if (TryReadKey(WebBuildConstants.PrimarySaveKey,
                    out data, out var primaryError))
            {
                error = string.Empty;
                return true;
            }

            if (TryReadKey(WebBuildConstants.BackupSaveKey,
                    out data, out var backupError))
            {
                error = string.IsNullOrEmpty(primaryError)
                    ? "Loaded the valid backup save."
                    : "Primary save rejected: " + primaryError +
                        "; loaded the valid backup save.";
                RestorePrimaryFromBackup();
                return true;
            }

            data = null;
            error = JoinErrors(primaryError, backupError);
            return false;
        }

        public void Save(SaveDataV1 data)
        {
            RequireAppliedReward();
            SaveWithClaims(data, ReadCurrentClaims());
        }

        public TreasureRewardResult GrantTreasure(
            string huntId, int goldBars, SaveDataV1 current, out SaveDataV1 rewarded)
        {
            BrowserTreasureRewards.ValidateReward(huntId, goldBars);
            RequireAppliedReward();
            var claims = ReadCurrentClaims();
            rewarded = null;
            if (Array.IndexOf(claims, huntId) >= 0)
                return TreasureRewardResult.AlreadyClaimed;

            // Validate and copy so a failed save never changes the caller's
            // live balance. Grant the gold resource, never the coin slot.
            var before = JsonFileSaveStore.SerializeEnvelope(current);
            if (!JsonFileSaveStore.TryReadText(before, out rewarded, out var error))
                throw new InvalidDataException(error);
            var index = BrowserTreasureRewards.GoldResourceIndex;
            rewarded.resources[index] = checked(rewarded.resources[index] + goldBars);
            SaveWithClaims(rewarded, BrowserTreasureRewards.MergeClaims(
                claims, new[] { huntId }));
            return TreasureRewardResult.Granted;
        }

        public void BeginTreasureApplication(string huntId)
        {
            if (!BrowserTreasureRewards.ValidHuntId(huntId))
                throw new ArgumentException("Invalid Treasure Hunt identity.");
            RequireAppliedReward();
            PendingTreasureApplication = huntId;
        }

        public void CompleteTreasureApplication(string huntId)
        {
            if (PendingTreasureApplication == huntId) PendingTreasureApplication = null;
        }

        private void RequireAppliedReward()
        {
            if (PendingTreasureApplication != null)
                throw new InvalidOperationException("The collected treasure is still being applied.");
        }

        private string[] ReadCurrentClaims(bool allowExternal = false)
        {
            foreach (var key in new[] { WebBuildConstants.PrimarySaveKey,
                         WebBuildConstants.BackupSaveKey })
            {
                if (TryGet(key, out var value) &&
                    JsonFileSaveStore.TryReadText(value, out _, out _))
                {
                    var stored = BrowserTreasureRewards.ReadClaims(value);
                    foreach (var id in stored)
                    {
                        if (allowExternal || Array.IndexOf(treasureClaims, id) >= 0) continue;
                        const string message = "Your progress changed in another tab. " +
                            "Reload this tab to use the latest save.";
                        if (!conflictWarningShown)
                        {
                            conflictWarningShown = true;
                            warningSink?.Invoke(message);
                        }
                        throw new InvalidOperationException(message);
                    }
                    return BrowserTreasureRewards.MergeClaims(stored, treasureClaims);
                }
            }
            return treasureClaims;
        }

        private void SaveWithClaims(SaveDataV1 data, string[] claims)
        {
            var envelope = BrowserTreasureRewards.Serialize(data, claims);
            RequireBounded(envelope);

            if (TryGet(WebBuildConstants.PrimarySaveKey,
                    out var primary) &&
                JsonFileSaveStore.TryReadText(primary, out _, out _))
            {
                Set(WebBuildConstants.BackupSaveKey, primary);
            }
            Set(WebBuildConstants.PrimarySaveKey, envelope);
            treasureClaims = claims;
        }

        public string Export()
        {
            if (TryGet(WebBuildConstants.PrimarySaveKey, out var primary) &&
                JsonFileSaveStore.TryReadText(primary, out _, out _))
            {
                return primary;
            }
            if (TryGet(WebBuildConstants.BackupSaveKey, out var backup) &&
                JsonFileSaveStore.TryReadText(backup, out _, out _))
            {
                return backup;
            }
            throw new InvalidDataException(
                "No valid save is available to copy.");
        }

        public bool TryImport(
            string envelopeJson,
            out SaveDataV1 data,
            out string error)
        {
            try
            {
                RequireBounded(envelopeJson);
            }
            catch (InvalidDataException exception)
            {
                data = null;
                error = exception.Message;
                return false;
            }

            if (!JsonFileSaveStore.TryReadText(
                    envelopeJson, out data, out error))
            {
                return false;
            }
            try
            {
                RequireAppliedReward();
                SaveWithClaims(data, BrowserTreasureRewards.MergeClaims(
                    ReadCurrentClaims(allowExternal: true), BrowserTreasureRewards.ReadClaims(envelopeJson)));
            }
            catch (Exception exception)
            {
                data = null;
                error = exception.Message;
                return false;
            }
            return true;
        }

        public void Reset()
        {
            Remove(WebBuildConstants.PrimarySaveKey);
            Remove(WebBuildConstants.BackupSaveKey);
            treasureClaims = Array.Empty<string>();
            PendingTreasureApplication = null;
        }

        private bool TryReadKey(
            string key,
            out SaveDataV1 data,
            out string error)
        {
            if (!TryGet(key, out var envelope))
            {
                data = null;
                error = "Value does not exist.";
                return false;
            }
            if (Encoding.UTF8.GetByteCount(envelope) >
                WebBuildConstants.MaximumSaveBytes)
            {
                data = null;
                error = "Save exceeds the 256 KiB browser limit.";
                return false;
            }
            if (!JsonFileSaveStore.TryReadText(envelope, out data, out error))
                return false;
            try
            {
                treasureClaims = BrowserTreasureRewards.ReadClaims(envelope);
                return true;
            }
            catch (Exception exception)
            {
                data = null;
                error = exception.Message;
                return false;
            }
        }

        private void RestorePrimaryFromBackup()
        {
            if (TryGet(WebBuildConstants.BackupSaveKey, out var backup))
            {
                Set(WebBuildConstants.PrimarySaveKey, backup);
            }
        }

        private bool TryGet(string key, out string value)
        {
            try
            {
                return ActiveStore.TryGet(key, out value);
            }
            catch (BrowserStorageException exception)
            {
                SwitchToSession(exception.Error);
                return sessionStore.TryGet(key, out value);
            }
            catch (Exception)
            {
                SwitchToSession(BrowserStorageError.Unknown);
                return sessionStore.TryGet(key, out value);
            }
        }

        private void Set(string key, string value)
        {
            try
            {
                ActiveStore.Set(key, value);
            }
            catch (BrowserStorageException exception)
            {
                SwitchToSession(exception.Error);
                sessionStore.Set(key, value);
            }
            catch (Exception)
            {
                SwitchToSession(BrowserStorageError.Unknown);
                sessionStore.Set(key, value);
            }
        }

        private void Remove(string key)
        {
            try
            {
                ActiveStore.Remove(key);
            }
            catch (BrowserStorageException exception)
            {
                SwitchToSession(exception.Error);
                sessionStore.Remove(key);
            }
            catch (Exception)
            {
                SwitchToSession(BrowserStorageError.Unknown);
                sessionStore.Remove(key);
            }
        }

        private IBrowserKeyValueStore ActiveStore => sessionFallback
            ? sessionStore
            : persistentStore;

        private void SwitchToSession(BrowserStorageError error)
        {
            sessionFallback = true;
            if (warningShown)
            {
                return;
            }
            warningShown = true;
            warningSink?.Invoke(SessionWarning + " (" + error + ")");
        }

        private static void RequireBounded(string envelope)
        {
            if (string.IsNullOrWhiteSpace(envelope))
            {
                throw new InvalidDataException("Save text is empty.");
            }
            var byteCount = Encoding.UTF8.GetByteCount(envelope);
            if (byteCount > WebBuildConstants.MaximumSaveBytes)
            {
                throw new InvalidDataException(
                    "Save exceeds the 256 KiB browser limit: " +
                    byteCount + " bytes.");
            }
        }

        private static string JoinErrors(string primary, string backup)
        {
            return "Primary: " + primary + "; backup: " + backup;
        }

        private sealed class MemoryKeyValueStore : IBrowserKeyValueStore
        {
            private readonly Dictionary<string, string> values = new();

            public bool IsAvailable => true;

            public bool TryGet(string key, out string value)
            {
                return values.TryGetValue(key, out value);
            }

            public void Set(string key, string value)
            {
                values[key] = value;
            }

            public void Remove(string key)
            {
                values.Remove(key);
            }
        }
    }
}
