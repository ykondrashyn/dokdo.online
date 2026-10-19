using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Dokdo;
using UnityEngine;

namespace Dokdo.Web
{
    public enum TreasureRewardResult { Granted, AlreadyClaimed }

    // Browser-only save extension. Keeping the claim inside the checksummed
    // payload makes the resource balance and its receipt one storage write.
    // Older save readers can still read the ordinary SaveDataV1 fields.
    public static class BrowserTreasureRewards
    {
        public const int GoldResourceIndex = 5;
        public const int MinimumGoldBars = 100;
        public const int MaximumGoldBars = 500;
        public const int MaximumClaims = 256;

        [Serializable]
        private sealed class Claims
        {
            public string[] webTreasureRewards = Array.Empty<string>();
        }

        public static bool ValidHuntId(string value)
        {
            if (value == null || value.Length != 16) return false;
            foreach (var ch in value)
                if (!(ch >= '0' && ch <= '9') && !(ch >= 'a' && ch <= 'f'))
                    return false;
            return true;
        }

        public static void ValidateReward(string huntId, int goldBars)
        {
            if (!ValidHuntId(huntId) || goldBars < MinimumGoldBars ||
                goldBars > MaximumGoldBars)
                throw new ArgumentException("Invalid Treasure Hunt reward.");
        }

        public static string[] ReadClaims(string envelopeJson)
        {
            var envelope = JsonUtility.FromJson<SaveEnvelope>(envelopeJson);
            var claims = JsonUtility.FromJson<Claims>(envelope.payload)
                ?.webTreasureRewards ?? Array.Empty<string>();
            if (claims.Length > MaximumClaims)
                throw new InvalidDataException("Too many Treasure Hunt receipts.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in claims)
                if (!ValidHuntId(id) || !seen.Add(id))
                    throw new InvalidDataException("Invalid Treasure Hunt receipt.");
            return claims;
        }

        public static string[] MergeClaims(params IEnumerable<string>[] sources)
        {
            var claims = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var source in sources)
                foreach (var id in source)
                    if (seen.Add(id)) claims.Add(id);
            if (claims.Count > MaximumClaims)
                claims.RemoveRange(0, claims.Count - MaximumClaims);
            return claims.ToArray();
        }

        public static string Serialize(SaveDataV1 data, string[] claims)
        {
            var ordinary = JsonFileSaveStore.SerializeEnvelope(data);
            if (claims.Length == 0) return ordinary;
            var envelope = JsonUtility.FromJson<SaveEnvelope>(ordinary);
            var extension = JsonUtility.ToJson(new Claims { webTreasureRewards = claims });
            envelope.payload = envelope.payload.Substring(0, envelope.payload.Length - 1) +
                "," + extension.Substring(1);
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(envelope.payload));
            var checksum = new StringBuilder(hash.Length * 2);
            foreach (var value in hash) checksum.Append(value.ToString("x2"));
            envelope.checksum = checksum.ToString();
            return JsonUtility.ToJson(envelope, true);
        }
    }
}
