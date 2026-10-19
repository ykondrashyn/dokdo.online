using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Dokdo
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public sealed class SystemUtcClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    public interface ISaveStore
    {
        bool TryLoad(out SaveDataV1 data, out string error);
        void Save(SaveDataV1 data);
        string Export();
        bool TryImport(string envelopeJson, out SaveDataV1 data, out string error);
        void Reset();
    }

    public static class SaveTime
    {
        public static int ElapsedSeconds(
            DateTime utcNow,
            DateTime previousUtc,
            int maximumSeconds)
        {
            if (maximumSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumSeconds));
            }

            var elapsed = (utcNow.ToUniversalTime() -
                previousUtc.ToUniversalTime()).TotalSeconds;
            if (elapsed <= 0d)
            {
                return 0;
            }

            return (int)Math.Min(Math.Floor(elapsed), maximumSeconds);
        }

        public static bool TryParseUtc(string value, out DateTime utc)
        {
            if (DateTime.TryParseExact(
                value,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed))
            {
                utc = parsed.ToUniversalTime();
                return true;
            }

            utc = default;
            return false;
        }
    }

    public sealed class JsonFileSaveStore : ISaveStore
    {
        public const string FileName = "save-v1.json";
        public const string BackupFileName = "save-v1.backup.json";

        private readonly string directory;

        public JsonFileSaveStore(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("A save directory is required.",
                    nameof(directory));
            }

            this.directory = directory;
        }

        public string PrimaryPath => Path.Combine(directory, FileName);
        public string BackupPath => Path.Combine(directory, BackupFileName);

        public bool TryLoad(out SaveDataV1 data, out string error)
        {
            if (TryReadFile(PrimaryPath, out data, out var primaryError))
            {
                error = string.Empty;
                return true;
            }

            if (TryReadFile(BackupPath, out data, out var backupError))
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
            SaveDataValidator.Validate(data);
            Directory.CreateDirectory(directory);
            var envelopeJson = SerializeEnvelope(data);
            var temporaryPath = PrimaryPath + ".tmp";

            try
            {
                File.WriteAllText(temporaryPath, envelopeJson,
                    new UTF8Encoding(false));
                if (!TryReadText(envelopeJson, out _, out var writtenError))
                {
                    throw new InvalidDataException(
                        "Temporary save validation failed: " + writtenError);
                }

                if (!File.Exists(PrimaryPath))
                {
                    File.Move(temporaryPath, PrimaryPath);
                }
                else if (TryReadFile(PrimaryPath, out _, out _))
                {
                    File.Replace(temporaryPath, PrimaryPath, BackupPath);
                }
                else
                {
                    File.Replace(temporaryPath, PrimaryPath, null);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        public string Export()
        {
            if (TryReadFile(PrimaryPath, out _, out _))
            {
                return File.ReadAllText(PrimaryPath, Encoding.UTF8);
            }

            if (TryReadFile(BackupPath, out _, out _))
            {
                return File.ReadAllText(BackupPath, Encoding.UTF8);
            }

            throw new InvalidDataException("No valid save is available to copy.");
        }

        public bool TryImport(
            string envelopeJson,
            out SaveDataV1 data,
            out string error)
        {
            if (!TryReadText(envelopeJson, out data, out error))
            {
                return false;
            }

            Save(data);
            return true;
        }

        public void Reset()
        {
            DeleteIfPresent(PrimaryPath);
            DeleteIfPresent(BackupPath);
            DeleteIfPresent(PrimaryPath + ".tmp");
        }

        public static string SerializeEnvelope(SaveDataV1 data)
        {
            SaveDataValidator.Validate(data);
            var payload = JsonUtility.ToJson(data, false);
            var envelope = new SaveEnvelope
            {
                payload = payload,
                checksum = ComputeChecksum(payload)
            };
            return JsonUtility.ToJson(envelope, true);
        }

        public static bool TryReadText(
            string envelopeJson,
            out SaveDataV1 data,
            out string error)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(envelopeJson))
            {
                error = "Save text is empty.";
                return false;
            }

            SaveEnvelope envelope;
            try
            {
                envelope = JsonUtility.FromJson<SaveEnvelope>(envelopeJson);
            }
            catch (Exception exception)
            {
                error = "Envelope JSON is invalid: " + exception.Message;
                return false;
            }

            if (envelope == null || envelope.schemaVersion != SaveDataV1.SchemaVersion)
            {
                error = "Unsupported save schema version.";
                return false;
            }

            if (string.IsNullOrEmpty(envelope.payload) ||
                !FixedTimeEquals(envelope.checksum, ComputeChecksum(envelope.payload)))
            {
                error = "Save checksum does not match its payload.";
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<SaveDataV1>(envelope.payload);
                SaveDataValidator.Validate(data);
            }
            catch (Exception exception)
            {
                data = null;
                error = "Save payload is invalid: " + exception.Message;
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static string ComputeChecksum(string payload)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var builder = new StringBuilder(hash.Length * 2);
            foreach (var value in hash)
            {
                builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            var difference = 0;
            for (var i = 0; i < left.Length; i++)
            {
                difference |= left[i] ^ right[i];
            }

            return difference == 0;
        }

        private static string JoinErrors(string primary, string backup)
        {
            if (string.IsNullOrEmpty(primary) && string.IsNullOrEmpty(backup))
            {
                return "No save file exists.";
            }

            return "Primary: " + primary + "; backup: " + backup;
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private bool TryReadFile(
            string path,
            out SaveDataV1 data,
            out string error)
        {
            if (!File.Exists(path))
            {
                data = null;
                error = "File does not exist.";
                return false;
            }

            try
            {
                return TryReadText(File.ReadAllText(path, Encoding.UTF8),
                    out data, out error);
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
            try
            {
                Directory.CreateDirectory(directory);
                var temporaryPath = PrimaryPath + ".tmp";
                File.Copy(BackupPath, temporaryPath, true);
                if (File.Exists(PrimaryPath))
                {
                    File.Replace(temporaryPath, PrimaryPath, null);
                }
                else
                {
                    File.Move(temporaryPath, PrimaryPath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Could not restore the primary save: " +
                    exception.Message);
            }
        }
    }

    public static class SaveRuntime
    {
        private static ISaveStore store;
        private static IClock clock;

        public static ISaveStore Store
        {
            get => store ??= new JsonFileSaveStore(Application.persistentDataPath);
            set => store = value ?? throw new ArgumentNullException(nameof(value));
        }

        public static IClock Clock
        {
            get => clock ??= new SystemUtcClock();
            set => clock = value ?? throw new ArgumentNullException(nameof(value));
        }

        public static SaveDataV1 Current { get; set; }

        public static void ResetDefaults()
        {
            store = null;
            clock = null;
            Current = null;
        }
    }

    internal static class SaveDataValidator
    {
        private const int MaximumValue = 2_000_000_000;

        public static void Validate(SaveDataV1 data)
        {
            if (data == null)
            {
                throw new InvalidDataException("Save payload is missing.");
            }

            RequireLength(data.islandDone, SaveDataV1.IslandCount,
                nameof(data.islandDone));
            RequireLength(data.resources, SaveDataV1.ResourceCount,
                nameof(data.resources));
            RequireLength(data.islandGoods, SaveDataV1.IslandCount,
                nameof(data.islandGoods));
            RequireLength(data.assistantLevels, SaveDataV1.AssistantCount,
                nameof(data.assistantLevels));
            RequireLength(data.seagullLevels, SaveDataV1.SeagullCount,
                nameof(data.seagullLevels));

            for (var i = 0; i < data.islandGoods.Length; i++)
            {
                if (data.islandGoods[i] == null)
                {
                    throw new InvalidDataException($"Island goods {i} is missing.");
                }

                RequireLength(data.islandGoods[i].goods,
                    SaveDataV1.GoodsPerIsland, $"islandGoods[{i}].goods");
                RequireNonNegative(data.islandGoods[i].goods,
                    $"islandGoods[{i}].goods");
            }

            RequireLevel(data.levelBody, nameof(data.levelBody));
            RequireLevel(data.levelCannon, nameof(data.levelCannon));
            RequireLevel(data.levelFishing, nameof(data.levelFishing));
            RequireLevel(data.levelSail, nameof(data.levelSail));
            RequireLevel(data.levelSteering, nameof(data.levelSteering));
            RequireNonNegative(data.resources, nameof(data.resources));
            RequireNonNegative(data.assistantLevels, nameof(data.assistantLevels));
            RequireNonNegative(data.seagullLevels, nameof(data.seagullLevels));

            RequireRange(data.lastIslandNumber, 0, SaveDataV1.IslandCount - 1,
                nameof(data.lastIslandNumber));
            RequireRange(data.targetIslandNumber, 0, SaveDataV1.IslandCount - 1,
                nameof(data.targetIslandNumber));
            RequireRange(data.currentWorld, 0, 1, nameof(data.currentWorld));
            RequireRange(data.worldLastDokdo, 0, SaveDataV1.IslandCount - 1,
                nameof(data.worldLastDokdo));
            RequireRange(data.worldLastItaly, 0, SaveDataV1.IslandCount - 1,
                nameof(data.worldLastItaly));
            RequireRange(data.dailyRewardDay, 0, 10, nameof(data.dailyRewardDay));

            RequireNonNegative(data.currentHp, nameof(data.currentHp));
            RequireNonNegative(data.deathCount, nameof(data.deathCount));
            RequireNonNegative(data.revivalCount, nameof(data.revivalCount));
            RequireNonNegative(data.bestArenaKill, nameof(data.bestArenaKill));
            RequireNonNegative(data.bestColosseumKill,
                nameof(data.bestColosseumKill));
            RequireNonNegative(data.nessyLevel, nameof(data.nessyLevel));
            RequireFiniteNonNegative(data.marketAdCooldown,
                nameof(data.marketAdCooldown));
            RequireFiniteNonNegative(data.marketResetCooldown,
                nameof(data.marketResetCooldown));
            RequireFiniteNonNegative(data.freeCoinCooldown,
                nameof(data.freeCoinCooldown));

            if (string.IsNullOrWhiteSpace(data.language) || data.language.Length > 64)
            {
                throw new InvalidDataException("Language is invalid.");
            }

            RequireUtc(data.savedAtUtc, nameof(data.savedAtUtc));
            RequireUtc(data.freeCoinAvailableAtUtc,
                nameof(data.freeCoinAvailableAtUtc));
            if (!string.IsNullOrEmpty(data.lastDailyRewardDateUtc))
            {
                RequireUtc(data.lastDailyRewardDateUtc,
                    nameof(data.lastDailyRewardDateUtc));
            }
        }

        private static void RequireLength<T>(T[] values, int length, string name)
        {
            if (values == null || values.Length != length)
            {
                throw new InvalidDataException(
                    $"{name} must contain exactly {length} values.");
            }
        }

        private static void RequireLevel(int value, string name)
        {
            RequireRange(value, 1, MaximumValue, name);
        }

        private static void RequireNonNegative(int[] values, string name)
        {
            for (var i = 0; i < values.Length; i++)
            {
                RequireNonNegative(values[i], $"{name}[{i}]");
            }
        }

        private static void RequireNonNegative(int value, string name)
        {
            RequireRange(value, 0, MaximumValue, name);
        }

        private static void RequireRange(int value, int minimum, int maximum,
            string name)
        {
            if (value < minimum || value > maximum)
            {
                throw new InvalidDataException(
                    $"{name} is outside {minimum}..{maximum}.");
            }
        }

        private static void RequireFiniteNonNegative(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw new InvalidDataException(name + " is invalid.");
            }
        }

        private static void RequireUtc(string value, string name)
        {
            if (!SaveTime.TryParseUtc(value, out _))
            {
                throw new InvalidDataException(name + " is not an ISO UTC timestamp.");
            }
        }
    }
}
