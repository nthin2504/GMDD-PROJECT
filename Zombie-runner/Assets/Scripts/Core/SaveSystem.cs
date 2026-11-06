using UnityEngine;

namespace ZombieRunner
{
    /// <summary>
    /// Persistent metagame data. Uses PlayerPrefs so it works on every platform
    /// with no extra setup (WebGL uses IndexedDB, standalone uses the registry /
    /// plist, mobile uses NSUserDefaults / SharedPreferences).
    /// </summary>
    public static class SaveSystem
    {
        const string KeyCoins = "ZR_COINSAVE";
        const string KeyDistance = "ZR_DISTSAVE";
        const string KeyBest = "ZR_BEST";
        const string KeyBestScore = "ZR_BESTSCORE";
        const string KeyTokens = "ZR_TOKENS";
        const string KeyUpgHp = "ZR_UPG_HP";
        const string KeyUpgMag = "ZR_UPG_MAG";
        const string KeyUpgDmg = "ZR_UPG_DMG";
        const string KeyUpgMagnet = "ZR_UPG_MAGNET";

        public static int Coins { get; private set; }
        public static int LifetimeDistance { get; private set; }
        public static int BestDistance { get; private set; }
        public static int BestScore { get; private set; }
        public static int Tokens { get; private set; }

        static readonly int[] UpgradeLevels = new int[4];
        static bool s_loaded;

        // Indices match Upgrades.All
        public const int UpgHp = 0;
        public const int UpgMag = 1;
        public const int UpgDmg = 2;
        public const int UpgMagnet = 3;

        public static void Load()
        {
            if (s_loaded) return;
            s_loaded = true;
            Coins = PlayerPrefs.GetInt(KeyCoins, 0);
            LifetimeDistance = PlayerPrefs.GetInt(KeyDistance, 0);
            BestDistance = PlayerPrefs.GetInt(KeyBest, 0);
            BestScore = PlayerPrefs.GetInt(KeyBestScore, 0);
            Tokens = Mathf.Clamp(PlayerPrefs.GetInt(KeyTokens, 0), 0, Upgrades.TokenMax);
            UpgradeLevels[UpgHp] = PlayerPrefs.GetInt(KeyUpgHp, 0);
            UpgradeLevels[UpgMag] = PlayerPrefs.GetInt(KeyUpgMag, 0);
            UpgradeLevels[UpgDmg] = PlayerPrefs.GetInt(KeyUpgDmg, 0);
            UpgradeLevels[UpgMagnet] = PlayerPrefs.GetInt(KeyUpgMagnet, 0);
            for (int i = 0; i < UpgradeLevels.Length; i++)
            {
                UpgradeLevels[i] = Mathf.Clamp(UpgradeLevels[i], 0, Upgrades.All[i].Costs.Length);
            }
        }

        public static void Persist()
        {
            PlayerPrefs.SetInt(KeyCoins, Coins);
            PlayerPrefs.SetInt(KeyDistance, LifetimeDistance);
            PlayerPrefs.SetInt(KeyBest, BestDistance);
            PlayerPrefs.SetInt(KeyBestScore, BestScore);
            PlayerPrefs.SetInt(KeyTokens, Tokens);
            PlayerPrefs.SetInt(KeyUpgHp, UpgradeLevels[UpgHp]);
            PlayerPrefs.SetInt(KeyUpgMag, UpgradeLevels[UpgMag]);
            PlayerPrefs.SetInt(KeyUpgDmg, UpgradeLevels[UpgDmg]);
            PlayerPrefs.SetInt(KeyUpgMagnet, UpgradeLevels[UpgMagnet]);
            PlayerPrefs.Save();
        }

        /// <summary>Bank a finished run. Only the un-banked delta should be passed in.</summary>
        public static void Commit(int runCoins, int runDistance, int runScore)
        {
            Coins += Mathf.Max(0, runCoins);
            LifetimeDistance += Mathf.Max(0, runDistance);
            BestDistance = Mathf.Max(BestDistance, Mathf.RoundToInt(runDistance));
            BestScore = Mathf.Max(BestScore, Mathf.RoundToInt(runScore));
            Persist();
        }

        public static int LevelOf(int upgradeIndex)
        {
            Load();
            return UpgradeLevels[upgradeIndex];
        }

        public static bool IsMax(int upgradeIndex)
        {
            return LevelOf(upgradeIndex) >= Upgrades.All[upgradeIndex].Costs.Length;
        }

        /// <summary>Next level price, or -1 when the upgrade is maxed.</summary>
        public static int CostOf(int upgradeIndex)
        {
            int level = LevelOf(upgradeIndex);
            int[] costs = Upgrades.All[upgradeIndex].Costs;
            return level >= costs.Length ? -1 : costs[level];
        }

        public static bool Buy(int upgradeIndex)
        {
            Load();
            int cost = CostOf(upgradeIndex);
            if (cost < 0 || Coins < cost) return false;
            Coins -= cost;
            UpgradeLevels[upgradeIndex]++;
            Persist();
            return true;
        }

        public static bool BuyToken()
        {
            Load();
            if (Tokens >= Upgrades.TokenMax || Coins < Upgrades.TokenCost) return false;
            Coins -= Upgrades.TokenCost;
            Tokens++;
            Persist();
            return true;
        }

        public static void ConsumeToken()
        {
            Load();
            if (Tokens > 0) Tokens--;
            Persist();
        }

        /// <summary>Wipe the save (useful while balancing / testing).</summary>
        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(KeyCoins);
            PlayerPrefs.DeleteKey(KeyDistance);
            PlayerPrefs.DeleteKey(KeyBest);
            PlayerPrefs.DeleteKey(KeyBestScore);
            PlayerPrefs.DeleteKey(KeyTokens);
            PlayerPrefs.DeleteKey(KeyUpgHp);
            PlayerPrefs.DeleteKey(KeyUpgMag);
            PlayerPrefs.DeleteKey(KeyUpgDmg);
            PlayerPrefs.DeleteKey(KeyUpgMagnet);
            PlayerPrefs.Save();
            for (int i = 0; i < UpgradeLevels.Length; i++) UpgradeLevels[i] = 0;
            Coins = LifetimeDistance = BestDistance = BestScore = Tokens = 0;
            s_loaded = true;
        }
    }
}
