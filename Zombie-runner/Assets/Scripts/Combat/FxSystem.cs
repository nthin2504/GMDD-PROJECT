using UnityEngine;

namespace ZombieRunner
{
    /// <summary>
    /// Punch feedback: muzzle bolts, impact sparks and pickup glitter.
    /// One ParticleSystem handles every burst; tracers are pooled LineRenderers.
    /// </summary>
    public class FxSystem : MonoBehaviour
    {
        ParticleSystem m_sparks;
        LineRenderer[] m_bolts;
        float[] m_boltLife;

        const int BoltCount = 10;

        public void Init(Transform parent)
        {
            InitSparks(parent);
            InitBolts(parent);
        }

        /// <summary>
        /// Particle systems must be configured while they are NOT playing: Unity rejects
        /// module changes (duration, size-over-lifetime, ...) on a live system and throws
        /// internal ArgumentOutOfRangeExceptions when you try. So the object is created
        /// inactive, fully configured, and only then activated and played.
        /// </summary>
        void InitSparks(Transform parent)
        {
            try
            {
                GameObject sparkGo = new GameObject("Sparks");
                sparkGo.transform.SetParent(parent, false);
                sparkGo.SetActive(false);

                m_sparks = sparkGo.AddComponent<ParticleSystem>();
                m_sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                ParticleSystem.MainModule main = m_sparks.main;
                main.playOnAwake = false;
                main.loop = true;
                main.duration = 1f;
                main.startLifetime = 0.55f;
                main.startSpeed = 0f;
                main.startSize = 0.2f;
                main.startColor = Color.white;
                main.gravityModifier = 1.4f;
                main.maxParticles = 600;
                main.simulationSpace = ParticleSystemSimulationSpace.World;

                ParticleSystem.EmissionModule emission = m_sparks.emission;
                emission.enabled = false;

                ParticleSystem.ColorOverLifetimeModule fade = m_sparks.colorOverLifetime;
                fade.enabled = true;
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.35f), new GradientAlphaKey(0f, 1f) });
                fade.color = new ParticleSystem.MinMaxGradient(gradient);

                ParticleSystem.SizeOverLifetimeModule size = m_sparks.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.15f));

                ParticleSystemRenderer psr = sparkGo.GetComponent<ParticleSystemRenderer>();
                if (psr != null)
                {
                    psr.renderMode = ParticleSystemRenderMode.Billboard;
                    psr.material = MaterialLibrary.Unlit(Color.white);
                    psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    psr.receiveShadows = false;
                    psr.alignment = ParticleSystemRenderSpace.World;
                }

                sparkGo.SetActive(true);
                m_sparks.Play();
            }
            catch (System.Exception e)
            {
                // FX are cosmetic: never let them stop the game from booting
                Debug.LogWarning("[Zombie Runner] Sparks unavailable, continuing without them: " + e.Message);
                m_sparks = null;
            }
        }

        void InitBolts(Transform parent)
        {
            m_bolts = new LineRenderer[BoltCount];
            m_boltLife = new float[BoltCount];
            for (int i = 0; i < BoltCount; i++)
            {
                GameObject go = new GameObject("Bolt");
                go.transform.SetParent(parent, false);
                LineRenderer lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.positionCount = 2;
                lr.startWidth = 0.10f;
                lr.endWidth = 0.06f;
                lr.numCapVertices = 2;
                lr.material = MaterialLibrary.UnlitInstance(new Color(1f, 0.88f, 0.65f, 0.95f));
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.enabled = false;
                m_bolts[i] = lr;
                m_boltLife[i] = 0f;
            }
        }

        public void Sparks(Vector3 position, int count, Color color, float spread, float upward, float worldMove)
        {
            if (m_sparks == null || count <= 0) return;

            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                emit.position = position + new Vector3(
                    (Random.value - 0.5f) * 0.25f,
                    (Random.value - 0.5f) * 0.25f,
                    (Random.value - 0.5f) * 0.25f);
                emit.velocity = new Vector3(
                    (Random.value - 0.5f) * spread,
                    Random.value * upward + 1f,
                    (Random.value - 0.5f) * spread + worldMove * 0.55f);
                emit.startColor = color;
                emit.startSize = 0.12f + Random.value * 0.16f;
                emit.startLifetime = 0.3f + Random.value * 0.35f;
                m_sparks.Emit(emit, 1);
            }
        }

        public void Bolt(Vector3 from, Vector3 to)
        {
            if (m_bolts == null) return;
            int index = -1;
            for (int i = 0; i < m_bolts.Length; i++)
            {
                if (m_boltLife[i] <= 0f) { index = i; break; }
            }
            if (index < 0) index = 0;

            LineRenderer lr = m_bolts[index];
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.enabled = true;
            lr.startColor = new Color(1f, 0.95f, 0.78f, 0.95f);
            lr.endColor = new Color(1f, 0.6f, 0.25f, 0.95f);
            m_boltLife[index] = GameConfig.BoltLife;
        }

        public void Tick(float dt)
        {
            if (m_bolts == null) return;
            for (int i = 0; i < m_bolts.Length; i++)
            {
                if (m_boltLife[i] <= 0f) continue;
                m_boltLife[i] -= dt;
                float a = Mathf.Clamp01(m_boltLife[i] / GameConfig.BoltLife);
                if (m_bolts[i] != null)
                {
                    if (m_boltLife[i] <= 0f)
                    {
                        m_bolts[i].enabled = false;
                    }
                    else
                    {
                        m_bolts[i].startColor = new Color(1f, 0.95f, 0.78f, 0.95f * a);
                        m_bolts[i].endColor = new Color(1f, 0.6f, 0.25f, 0.95f * a);
                    }
                }
            }
        }

        public void Clear()
        {
            if (m_sparks != null) m_sparks.Clear();
            if (m_bolts == null) return;
            for (int i = 0; i < m_bolts.Length; i++)
            {
                m_boltLife[i] = 0f;
                if (m_bolts[i] != null) m_bolts[i].enabled = false;
            }
        }
    }
}
