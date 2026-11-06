namespace ZombieRunner
{
    /// <summary>A permanent base-camp upgrade bought with banked coins.</summary>
    public class UpgradeDef
    {
        public string Name;
        public string Description;
        public int[] Costs;
    }

    /// <summary>Base-camp shop catalogue + derived run statistics.</summary>
    public static class Upgrades
    {
        public const int TokenCost = 250;
        public const int TokenMax = 3;

        public static readonly UpgradeDef[] All =
        {
            new UpgradeDef { Name = "Body Armor",    Description = "+25 max HP",          Costs = new[] {  80, 180, 360 } },
            new UpgradeDef { Name = "Extended Mag",  Description = "+5 magazine",         Costs = new[] {  70, 160, 320 } },
            new UpgradeDef { Name = "Hollow Points", Description = "+1 pierce per round", Costs = new[] { 120, 260, 520 } },
            new UpgradeDef { Name = "Coin Magnet",   Description = "+0.6 m pickup radius",Costs = new[] {  60, 140, 280 } }
        };

        // ---- derived values (base + purchased levels) ----------------------
        public static int MaxHp
        {
            get { return GameConfig.HpMax + SaveSystem.LevelOf(SaveSystem.UpgHp) * 25; }
        }

        public static int MaxAmmo
        {
            get { return GameConfig.AmmoMax + SaveSystem.LevelOf(SaveSystem.UpgMag) * 5; }
        }

        public static int StartAmmo
        {
            get
            {
                return UnityEngine.Mathf.Min(MaxAmmo,
                    GameConfig.AmmoStart + SaveSystem.LevelOf(SaveSystem.UpgMag) * 2);
            }
        }

        public static int Pierce
        {
            get { return GameConfig.BasePierce + SaveSystem.LevelOf(SaveSystem.UpgDmg); }
        }

        public static float CoinMagnet
        {
            get { return GameConfig.BaseMagnet + SaveSystem.LevelOf(SaveSystem.UpgMagnet) * 0.6f; }
        }
    }
}
