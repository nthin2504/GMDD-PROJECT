using UnityEngine;

namespace ZombieRunner
{
    /// <summary>
    /// Every type of pooled object in the game.
    /// </summary>
    public enum SpawnType
    {
        Barrier,   // low concrete barrier  -> jump over
        Wreck,     // burnt-out car         -> change lane (too tall to jump)
        Beam,      // hanging scaffold      -> slide under
        Walker,    // slow zombie           -> shoot / jump
        Runner,    // fast zombie           -> shoot / dodge
        Brute,     // armoured zombie (2 hp) -> two shots
        Coin,
        Ammo,
        Medkit
    }

    /// <summary>Vertical occupancy of a hazard, in metres above the road.</summary>
    public struct Hitbox
    {
        public float Bottom;
        public float Top;
        public Hitbox(float bottom, float top) { Bottom = bottom; Top = top; }
    }

    /// <summary>Per-zombie tuning.</summary>
    public struct ZedStats
    {
        public float CloseSpeed;   // extra m/s the zed runs toward the player
        public int Hp;
        public int Score;
        public int Damage;
    }

    /// <summary>
    /// One stage theme. Weights drive the spawn director; colours drive the
    /// lighting / fog / sky grading.
    /// </summary>
    public class SectorDef
    {
        public string Id;
        public string Name;
        public string Code;
        public string Blurb;
        public int UnlockDistance;

        public Color Fog;
        public Color SkyTop;
        public Color SkyBottom;
        public Color Glow;
        public Color Sun;
        public Color HemiSky;
        public Color HemiGround;
        public Color BuildingTint;
        public Color RoadTint;
        public float SunIntensity;

        public float WeightBarrier;
        public float WeightCar;
        public float WeightBeam;
        public float WeightWalker;
        public float WeightRunner;
        public float WeightBrute;

        public float SpeedMultiplier = 1f;
        public float DensityMultiplier = 1f;

        public float WeightFor(SpawnType type)
        {
            switch (type)
            {
                case SpawnType.Barrier: return WeightBarrier;
                case SpawnType.Wreck: return WeightCar;
                case SpawnType.Beam: return WeightBeam;
                case SpawnType.Walker: return WeightWalker;
                case SpawnType.Runner: return WeightRunner;
                case SpawnType.Brute: return WeightBrute;
                default: return 0f;
            }
        }
    }

    /// <summary>
    /// All gameplay tunables in one place (metres, seconds, m/s).
    /// Values are lifted 1:1 from the browser prototype.
    /// </summary>
    public static class GameConfig
    {
        // ---------------------------------------------------------------- track
        public static readonly float[] LaneX = { -2.6f, 0f, 2.6f };
        public const int LaneCount = 3;
        public const float RoadWidth = 10f;
        public const float ChunkLength = 30f;
        public const int ChunkCount = 8;
        public const float SpawnZ = -92f;
        public const float DespawnZ = 16f;

        // ---------------------------------------------------------------- speed
        public const float StartSpeed = 11f;
        public const float MaxSpeed = 24f;

        public static float SpeedForDistance(float distance)
        {
            if (distance < 500f) return 11f + (distance / 500f) * 3f;
            if (distance < 1500f) return 14f + ((distance - 500f) / 1000f) * 3f;
            return Mathf.Min(MaxSpeed, 17f + ((distance - 1500f) / 2000f) * 7f);
        }

        // --------------------------------------------------------------- player
        public const float LaneSnap = 11f;
        public const float JumpVelocity = 10.7f;   // apex ~2.05 m
        public const float Gravity = 28f;
        public const float SlideTime = 0.62f;
        public const float SlideCooldown = 0.30f;
        public const float StandHeight = 1.70f;
        public const float SlideHeight = 0.85f;
        public const int HpMax = 100;
        public const float InvulnerabilityTime = 1.3f;
        public const float ReviveInvulnerability = 2.2f;

        // --------------------------------------------------------------- combat
        public const int BasePierce = 2;
        public const int AmmoMax = 20;
        public const int AmmoStart = 12;
        public const float FireCooldown = 0.09f;
        public const float BoltLife = 0.1f;
        public const int ZombieDamage = 25;
        public const int KillAmmoRefund = 2;
        public const int AmmoPickup = 6;
        public const int MedkitHeal = 30;

        // ----------------------------------------------------------------- feel
        public const float CollisionHalfDepth = 1.5f;
        public const float PickupReach = 1.15f;
        public const float BaseMagnet = 1.15f;
        public const float ComboWindow = 2.6f;

        // ------------------------------------------------------------- spawning
        public const float FirstSpawnGap = 18f;
        public const float FadeTime = 0.38f;

        // -------------------------------------------------------------- hitbox
        /// <summary>Vertical occupancy per hazard (metres above the road).</summary>
        public static Hitbox HitboxOf(SpawnType type)
        {
            switch (type)
            {
                case SpawnType.Barrier: return new Hitbox(0f, 1.15f);     // jump it
                case SpawnType.Wreck: return new Hitbox(0f, 2.35f);     // cannot jump — change lane
                case SpawnType.Beam: return new Hitbox(1.15f, 3.60f);   // slide under it
                case SpawnType.Walker: return new Hitbox(0f, 1.70f);
                case SpawnType.Runner: return new Hitbox(0f, 1.60f);
                case SpawnType.Brute: return new Hitbox(0f, 2.25f);
                default: return new Hitbox(-9f, 9f);                    // pickups ignore height
            }
        }

        public static bool IsZed(SpawnType type)
        {
            return type == SpawnType.Walker || type == SpawnType.Runner || type == SpawnType.Brute;
        }

        public static bool IsPickup(SpawnType type)
        {
            return type == SpawnType.Coin || type == SpawnType.Ammo || type == SpawnType.Medkit;
        }

        public static ZedStats ZedStatsOf(SpawnType type)
        {
            switch (type)
            {
                case SpawnType.Runner: return new ZedStats { CloseSpeed = 8.0f, Hp = 1, Score = 160, Damage = 20 };
                case SpawnType.Brute: return new ZedStats { CloseSpeed = 1.0f, Hp = 2, Score = 300, Damage = 40 };
                default: return new ZedStats { CloseSpeed = 2.2f, Hp = 1, Score = 100, Damage = ZombieDamage };
            }
        }

        /// <summary>Vertical overlap test shared by the sim and the debug probe.</summary>
        public static bool OverlapsVertically(SpawnType type, float feet, float height)
        {
            Hitbox hb = HitboxOf(type);
            return (feet + height) > hb.Bottom && feet < hb.Top;
        }

        // -------------------------------------------------------------- sectors
        public static readonly SectorDef[] Sectors = BuildSectors();

        public static bool SectorUnlocked(SectorDef sector)
        {
            return SaveSystem.BestDistance >= sector.UnlockDistance;
        }

        static SectorDef[] BuildSectors()
        {
            return new[]
            {
                new SectorDef
                {
                    Id = "ruins", Name = "City Ruins", Code = "SECTOR 01", UnlockDistance = 0,
                    Blurb = "Collapsed highway. Balanced hazard mix.",
                    Fog = Hex(0x1b232b), SkyTop = Hex(0x0a1219), SkyBottom = Hex(0x53331f), Glow = Hex(0xd97a3c),
                    Sun = Hex(0xffb277), SunIntensity = 2.1f,
                    HemiSky = Hex(0x5a7590), HemiGround = Hex(0x241a13),
                    BuildingTint = Color.white, RoadTint = Color.white,
                    WeightBarrier = 1.0f, WeightCar = 0.8f, WeightBeam = 0.9f,
                    WeightWalker = 1.0f, WeightRunner = 0.25f, WeightBrute = 0f,
                    SpeedMultiplier = 1f, DensityMultiplier = 1f
                },
                new SectorDef
                {
                    Id = "subway", Name = "Subway", Code = "SECTOR 02", UnlockDistance = 900,
                    Blurb = "Low ceilings and fast runners. Sliding is life.",
                    Fog = Hex(0x101a1c), SkyTop = Hex(0x050c0e), SkyBottom = Hex(0x123236), Glow = Hex(0x3fbfae),
                    Sun = Hex(0x7fd8c8), SunIntensity = 1.5f,
                    HemiSky = Hex(0x2f6a6a), HemiGround = Hex(0x0c1a1a),
                    BuildingTint = Hex(0x6f9a94), RoadTint = Hex(0x7e938f),
                    WeightBarrier = 0.6f, WeightCar = 0.3f, WeightBeam = 1.6f,
                    WeightWalker = 0.9f, WeightRunner = 1.0f, WeightBrute = 0f,
                    SpeedMultiplier = 1.08f, DensityMultiplier = 1.12f
                },
                new SectorDef
                {
                    Id = "military", Name = "Military Base", Code = "SECTOR 03", UnlockDistance = 2200,
                    Blurb = "Armoured brutes and wreck blockades. Heavy resistance.",
                    Fog = Hex(0x241c14), SkyTop = Hex(0x140f08), SkyBottom = Hex(0x4a2f14), Glow = Hex(0xe0a33c),
                    Sun = Hex(0xffd08a), SunIntensity = 2.4f,
                    HemiSky = Hex(0x6a5a3a), HemiGround = Hex(0x201608),
                    BuildingTint = Hex(0xd8c08c), RoadTint = Hex(0xc2ab84),
                    WeightBarrier = 1.2f, WeightCar = 1.4f, WeightBeam = 0.5f,
                    WeightWalker = 0.9f, WeightRunner = 0.5f, WeightBrute = 1.1f,
                    SpeedMultiplier = 1.14f, DensityMultiplier = 1.2f
                }
            };
        }

        /// <summary>0xRRGGBB -> Color (sRGB values, alpha 1).</summary>
        public static Color Hex(int rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f,
                1f);
        }
    }
}
