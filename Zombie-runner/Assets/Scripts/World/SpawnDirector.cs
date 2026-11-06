using System.Collections.Generic;
using UnityEngine;

namespace ZombieRunner
{
    /// <summary>
    /// Weighted wave generator. Guarantees at least one clear lane per wave and
    /// blends hazards, zombies and pickups according to the active sector.
    /// </summary>
    public class SpawnDirector : MonoBehaviour
    {
        [System.NonSerialized] public EntityPool Pool = new EntityPool();

        GameManager m_game;
        readonly int[] m_lanes = new int[3];

        public void Init(GameManager game, Transform poolRoot)
        {
            m_game = game;
            Pool.Build(poolRoot);
        }

        // ------------------------------------------------------------- helpers
        static void Shuffle(int[] lanes)
        {
            for (int i = lanes.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = lanes[i];
                lanes[i] = lanes[j];
                lanes[j] = tmp;
            }
        }

        /// <summary>Weighted pick over the candidate list. Returns fallback when all weights are 0.</summary>
        SpawnType WeightedPick(List<SpawnType> candidates, SectorDef sector, SpawnType fallback)
        {
            float total = 0f;
            for (int i = 0; i < candidates.Count; i++) total += sector.WeightFor(candidates[i]);
            if (total <= 0f) return fallback;

            float roll = Random.value * total;
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= sector.WeightFor(candidates[i]);
                if (roll <= 0f) return candidates[i];
            }
            return candidates[0];
        }

        static readonly List<SpawnType> BlockCandidates = new List<SpawnType>
        {
            SpawnType.Barrier, SpawnType.Wreck, SpawnType.Beam
        };

        static readonly List<SpawnType> ZedCandidates = new List<SpawnType>
        {
            SpawnType.Walker, SpawnType.Runner, SpawnType.Brute
        };

        // --------------------------------------------------------------- waves
        public void PreloadWave()
        {
            for (int i = 0; i < 6; i++)
            {
                Entity coin = Pool.Take(SpawnType.Coin, GameConfig.LaneX[1], -32f - i * 2.4f, 1);
                if (coin == null) break;
            }
            Entity walker = Pool.Take(SpawnType.Walker, GameConfig.LaneX[1], -62f, 1);
            if (walker != null) walker.Z = -62f;
        }

        public void SpawnWave()
        {
            SectorDef sector = m_game.Sector;
            float hard = Mathf.Min(1f, m_game.Distance / 1800f);

            m_lanes[0] = 0; m_lanes[1] = 1; m_lanes[2] = 2;
            Shuffle(m_lanes);

            int blockCount = Random.value < 0.3f + hard * 0.4f ? 2 : 1;
            blockCount = Mathf.Clamp(blockCount, 1, 2);

            for (int i = 0; i < blockCount; i++)
            {
                int lane = m_lanes[i];
                SpawnType type = WeightedPick(BlockCandidates, sector, SpawnType.Barrier);
                // one blocked lane in five holds a walker instead
                if (Random.value < 0.25f) type = SpawnType.Walker;

                float z = GameConfig.SpawnZ - Random.value * 6f - i * 4f;
                Entity e = Pool.Take(type, GameConfig.LaneX[lane], z, lane);
                if (e == null) continue;
                e.Z = z;

                float yaw;
                if (type == SpawnType.Wreck || type == SpawnType.Barrier)
                {
                    yaw = (Random.value < 0.5f ? 0f : 180f) + (Random.value - 0.5f) * 11f;
                }
                else
                {
                    yaw = (Random.value - 0.5f) * 23f;
                }
                e.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            }

            // zombies in the free lanes
            int freeStart = blockCount;
            int freeCount = GameConfig.LaneCount - blockCount;
            int zedCount = Random.value < 0.5f ? 2 : 1;
            for (int i = 0; i < zedCount; i++)
            {
                if (Random.value > 0.8f) continue;
                SpawnType kind = WeightedPick(ZedCandidates, sector, SpawnType.Walker);
                int lane = m_lanes[freeStart + Random.Range(0, Mathf.Max(1, freeCount))];
                float z = GameConfig.SpawnZ - 8f - Random.value * 12f - i * 7f;
                Entity e = Pool.Take(kind, GameConfig.LaneX[lane], z, lane);
                if (e != null) e.Z = z;
            }

            // coin trail
            if (Random.value < 0.78f)
            {
                int lane = m_lanes[freeStart + Random.Range(0, Mathf.Max(1, freeCount))];
                int count = 4 + Random.Range(0, 3);
                for (int i = 0; i < count; i++)
                {
                    float z = GameConfig.SpawnZ + 6f - i * 2.4f;
                    Entity c = Pool.Take(SpawnType.Coin, GameConfig.LaneX[lane], z, lane);
                    if (c == null) break;
                    c.Z = z;
                }
            }

            // ammo drop
            if (Random.value < 0.36f)
            {
                int lane = m_lanes[freeStart + Random.Range(0, Mathf.Max(1, freeCount))];
                float z = GameConfig.SpawnZ - 4f;
                Entity e = Pool.Take(SpawnType.Ammo, GameConfig.LaneX[lane], z, lane);
                if (e != null) e.Z = z;
            }

            // medkit only when hurt
            if (m_game.Hp < Upgrades.MaxHp * 0.6f && Random.value < 0.34f)
            {
                int lane = m_lanes[freeStart + Random.Range(0, Mathf.Max(1, freeCount))];
                float z = GameConfig.SpawnZ - 2f;
                Entity e = Pool.Take(SpawnType.Medkit, GameConfig.LaneX[lane], z, lane);
                if (e != null) e.Z = z;
            }
        }

        // ---------------------------------------------------------------- tick
        /// <summary>Moves every live entity and returns entities that were recycled.</summary>
        public void TickEntities(float dz, float dt, float playerX, float time)
        {
            List<Entity> active = Pool.Active;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Entity e = active[i];
                if (e == null)
                {
                    active.RemoveAt(i);
                    continue;
                }

                bool alive = e.Tick(dz, dt, playerX, time);
                if (!alive || e.Z > GameConfig.DespawnZ)
                {
                    Pool.Release(e);
                }
            }
        }

        public void ReleaseAll()
        {
            Pool.ReleaseAll();
        }

        /// <summary>Revive helper: clears hazards close to the player.</summary>
        public void ClearNearPlayer(float zLimit, FxSystem fx)
        {
            List<Entity> active = Pool.Active;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Entity e = active[i];
                if (e == null) continue;
                if (GameConfig.IsPickup(e.Type)) continue;
                if (e.Z <= zLimit) continue;
                if (fx != null) fx.Sparks(new Vector3(e.X, 1f, e.Z), 10, new Color(0.62f, 0.88f, 1f), 5f, 4f, 0f);
                Pool.Release(e);
            }
        }
    }
}
