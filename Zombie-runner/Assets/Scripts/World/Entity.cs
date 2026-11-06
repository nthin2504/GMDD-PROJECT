using UnityEngine;

namespace ZombieRunner
{
    /// <summary>Handles for the animated limb pivots of a zombie (runtime only).</summary>
    [System.Serializable]
    public class ZedRig
    {
        public Transform ArmL;
        public Transform ArmR;
        public Transform LegL;
        public Transform LegR;
        public Transform Torso;
    }

    /// <summary>
    /// One pooled world object (hazard, zombie or pickup).
    /// Movement is fully kinematic: everything slides toward +Z as the world
    /// scrolls, exactly like the browser build. No colliders, no rigidbodies.
    /// </summary>
    public class Entity : MonoBehaviour
    {
        public SpawnType Type;
        public bool Active;
        public bool Dead;
        public int Lane = 1;
        public int Hp;
        public float Life;
        public float Spin;
        public float BaseY;
        public float Z;
        public float X;
        public Vector3 HomeScale = Vector3.one;
        [System.NonSerialized] public ZedRig Rig;

        float m_punch;
        Transform m_transform;

        Transform CachedTransform
        {
            get
            {
                if (m_transform == null) m_transform = transform;
                return m_transform;
            }
        }

        public void ResetForSpawn(float x, float z, int lane)
        {
            X = x;
            Z = z;
            Lane = lane;
            Dead = false;
            Life = 0f;
            m_punch = 0f;
            Spin = Random.value * 6.28f;
            BaseY = Type == SpawnType.Coin ? 1f : 0f;

            Transform t = CachedTransform;
            t.localScale = HomeScale;
            t.localPosition = new Vector3(x, BaseY, z);
            t.localRotation = Quaternion.identity;

            if (GameConfig.IsZed(Type))
            {
                Hp = GameConfig.ZedStatsOf(Type).Hp;
                if (Rig != null)
                {
                    Rig.ArmL.localRotation = Quaternion.Euler(-1.4f, 0f, 0.2f);
                    Rig.ArmR.localRotation = Quaternion.Euler(-1.4f, 0f, -0.2f);
                }
            }
        }

        /// <summary>Call once right after the visual hierarchy has been built.</summary>
        public void CaptureHomeScale()
        {
            HomeScale = transform.localScale;
        }

        public void Punch()
        {
            m_punch = 0.12f;
        }

        /// <summary>
        /// Advances the entity. Returns false once it should go back to the pool.
        /// </summary>
        public bool Tick(float dz, float dt, float playerX, float time)
        {
            Transform t = CachedTransform;
            if (m_punch > 0f) m_punch = Mathf.Max(0f, m_punch - dt);

            if (GameConfig.IsZed(Type))
            {
                if (Dead)
                {
                    // topple forward and sink into the road
                    Life += dt;
                    float yaw = t.localRotation.eulerAngles.y;
                    t.localRotation = Quaternion.Euler(Mathf.Min(1.45f, Life * 7f), yaw, 0f);
                    Z += dz;
                    t.localPosition = new Vector3(X, -Mathf.Min(1.2f, Life * 1.3f), Z);
                    return Life <= 0.75f;
                }

                Z += dz + dt * GameConfig.ZedStatsOf(Type).CloseSpeed;
                float zp = time * 6f + Spin;
                if (Rig != null)
                {
                    Rig.LegL.localRotation = Quaternion.Euler(Mathf.Sin(zp) * 0.5f, 0f, 0f);
                    Rig.LegR.localRotation = Quaternion.Euler(-Mathf.Sin(zp) * 0.5f, 0f, 0f);
                    Rig.ArmL.localRotation = Quaternion.Euler(-1.4f, 0f, 0.2f + Mathf.Sin(zp) * 0.08f);
                    Rig.ArmR.localRotation = Quaternion.Euler(-1.4f, 0f, -0.2f - Mathf.Sin(zp) * 0.08f);
                }
                float bob = Mathf.Abs(Mathf.Sin(zp)) * 0.05f;
                float punch = m_punch > 0f ? 1f + m_punch * 0.6f : 1f;
                t.localScale = HomeScale * punch;
                t.localPosition = new Vector3(X, bob, Z);
                t.LookAt(new Vector3(playerX, 0f, 0f));
                return true;
            }

            if (Type == SpawnType.Coin)
            {
                Z += dz;
                Spin += dt * 3.4f;
                float punch = m_punch > 0f ? 1f + m_punch : 1f;
                if (Dead)
                {
                    Life += dt;
                    BaseY += dt * 6f;
                    float scale = Mathf.Max(0.01f, 1f - Life * 3f);
                    t.localScale = HomeScale * scale;
                    t.localPosition = new Vector3(X, BaseY, Z);
                    t.localRotation = Quaternion.Euler(0f, Spin * Mathf.Rad2Deg + Life * 1146f, 0f);
                    return Life <= 0.34f;
                }
                t.localScale = HomeScale * punch;
                t.localPosition = new Vector3(X, BaseY, Z);
                t.localRotation = Quaternion.Euler(0f, Spin * Mathf.Rad2Deg, 0f);
                return true;
            }

            Z += dz;
            t.localPosition = new Vector3(X, t.localPosition.y, Z);
            if (m_punch > 0f)
            {
                t.localScale = HomeScale * (1f + m_punch * 0.6f);
            }
            return true;
        }
    }
}
