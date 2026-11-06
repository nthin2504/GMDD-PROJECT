using System.Collections.Generic;
using UnityEngine;

namespace ZombieRunner
{
    /// <summary>
    /// Fixed-size object pools, one list per entity type. Nothing is ever
    /// instantiated during a run — objects are toggled and recycled, so there is
    /// no garbage-collection churn while playing.
    /// </summary>
    public class EntityPool
    {
        static readonly Dictionary<SpawnType, int> PoolSizes = new Dictionary<SpawnType, int>
        {
            { SpawnType.Barrier, 10 },
            { SpawnType.Wreck, 8 },
            { SpawnType.Beam, 8 },
            { SpawnType.Walker, 16 },
            { SpawnType.Runner, 8 },
            { SpawnType.Brute, 6 },
            { SpawnType.Coin, 64 },
            { SpawnType.Ammo, 10 },
            { SpawnType.Medkit, 4 }
        };

        readonly Dictionary<SpawnType, List<Entity>> m_pools = new Dictionary<SpawnType, List<Entity>>();
        readonly List<Entity> m_active = new List<Entity>(160);
        Transform m_root;

        public List<Entity> Active { get { return m_active; } }

        public void Build(Transform root)
        {
            m_root = root;
            foreach (KeyValuePair<SpawnType, int> pair in PoolSizes)
            {
                List<Entity> list = new List<Entity>(pair.Value);
                for (int i = 0; i < pair.Value; i++)
                {
                    GameObject go = EntityFactory.Create(pair.Key, root);
                    Entity entity = go.GetComponent<Entity>();
                    entity.Active = false;
                    list.Add(entity);
                }
                m_pools[pair.Key] = list;
            }
        }

        public Entity Take(SpawnType type, float x, float z, int lane)
        {
            List<Entity> list;
            if (!m_pools.TryGetValue(type, out list)) return null;

            for (int i = 0; i < list.Count; i++)
            {
                Entity e = list[i];
                if (e == null || e.Active) continue;
                e.Active = true;
                e.ResetForSpawn(x, z, lane);
                e.gameObject.SetActive(true);
                m_active.Add(e);
                return e;
            }
            return null;   // pool exhausted — simply skip this spawn
        }

        public void Release(Entity entity)
        {
            if (entity == null) return;
            entity.Active = false;
            entity.gameObject.SetActive(false);
            m_active.Remove(entity);
        }

        public void ReleaseAll()
        {
            for (int i = m_active.Count - 1; i >= 0; i--)
            {
                Entity e = m_active[i];
                if (e == null)
                {
                    m_active.RemoveAt(i);
                    continue;
                }
                e.Active = false;
                e.gameObject.SetActive(false);
            }
            m_active.Clear();
        }

        public Transform Root { get { return m_root; } }
    }
}
