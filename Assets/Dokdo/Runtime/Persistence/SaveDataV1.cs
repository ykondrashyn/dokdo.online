using System;

namespace Dokdo
{
    [Serializable]
    public sealed class IslandGoodsSaveV1
    {
        public int[] goods = new int[SaveDataV1.GoodsPerIsland];
    }

    [Serializable]
    public sealed class SaveDataV1
    {
        public const int SchemaVersion = 1;
        public const int IslandCount = 47;
        public const int GoodsPerIsland = 6;
        public const int ResourceCount = 7;
        public const int AssistantCount = 3;
        public const int SeagullCount = 2;

        public bool pickboat = true;
        public bool hiddenUnlocked;
        public bool tutorialComplete;
        public bool noAds = true;
        public bool allIslands;
        public bool[] islandDone = new bool[IslandCount];

        public int levelBody = 1;
        public int levelCannon = 1;
        public int levelFishing = 1;
        public int levelSail = 1;
        public int levelSteering = 1;
        public int[] resources = new int[ResourceCount];
        public int lastIslandNumber;
        public int targetIslandNumber = 1;
        public int emblemNumber;
        public int currentHp = 40;
        public int deathCount;
        public int revivalCount = 3;
        public int bestArenaKill;
        public int bestColosseumKill;
        public int currentWorld;
        public int worldLastDokdo;
        public int worldLastItaly = 28;

        public IslandGoodsSaveV1[] islandGoods =
            new IslandGoodsSaveV1[IslandCount];
        public int[] assistantLevels = new int[AssistantCount];
        public int[] seagullLevels = new int[SeagullCount];
        public int nessyLevel;

        public float marketAdCooldown;
        public float marketResetCooldown;
        public float freeCoinCooldown;

        public int dailyRewardDay;
        public string lastDailyRewardDateUtc = string.Empty;
        public string freeCoinAvailableAtUtc = DateTime.UnixEpoch.ToString("O");
        public string language = "English";
        public string savedAtUtc = DateTime.UnixEpoch.ToString("O");

        public static SaveDataV1 CreateFresh(string language, DateTime utcNow)
        {
            var data = new SaveDataV1
            {
                language = string.IsNullOrWhiteSpace(language) ? "English" : language,
                savedAtUtc = utcNow.ToUniversalTime().ToString("O")
            };
            data.resources[0] = 500;
            data.islandDone[0] = true;
            data.islandDone[28] = true;
            for (var i = 0; i < data.islandGoods.Length; i++)
            {
                data.islandGoods[i] = new IslandGoodsSaveV1();
            }

            return data;
        }
    }
}
