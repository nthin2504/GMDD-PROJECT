using UnityEngine;

namespace ZombieRunner
{
    /// <summary>
    /// Endless road: a ring of 8 thirty-metre chunks that leapfrog ahead of the
    /// camera as the world scrolls toward +Z. Buildings, rubble and street lamps
    /// are re-randomised every time a chunk recycles, so the city never repeats.
    /// </summary>
    public class WorldScroller : MonoBehaviour
    {
        class Chunk
        {
            public Transform Root;
            public Transform[] Buildings;
            public Transform[] Props;
            public Transform[] Lamps;
            public Material[] BuildingMats;
        }

        Chunk[] m_chunks;
        Transform m_ground;
        Material m_roadMat;
        Material m_walkMat;
        Material m_groundMat;
        Material m_concreteMat;
        Material m_rustMat;
        Material m_darkMat;
        Material m_lampMat;

        public void Build()
        {
            m_roadMat = new Material(MaterialLibrary.Lit(Color.white, TextureFactory.Road));
            m_roadMat.name = "ZR_Road";
            m_roadMat.mainTextureScale = new Vector2(1f, 1f);

            m_walkMat = new Material(MaterialLibrary.Lit(Color.white, TextureFactory.Sidewalk));
            m_walkMat.name = "ZR_Sidewalk";
            m_walkMat.mainTextureScale = new Vector2(1f, 8f);

            m_groundMat = MaterialLibrary.Lit(GameConfig.Hex(0x171b1f), 0f, 0.05f);
            m_concreteMat = MaterialLibrary.Lit(EntityFactory.Concrete, 0f, 0.15f);
            m_rustMat = MaterialLibrary.Lit(EntityFactory.Rust, 0.25f, 0.25f);
            m_darkMat = MaterialLibrary.Lit(EntityFactory.Dark, 0.35f, 0.4f);
            m_lampMat = MaterialLibrary.Emissive(EntityFactory.Lamp, EntityFactory.LampGlow, 2.2f);

            m_chunks = new Chunk[GameConfig.ChunkCount];
            for (int i = 0; i < GameConfig.ChunkCount; i++)
            {
                m_chunks[i] = BuildChunk(i);
            }

            // distant filler ground so the horizon never shows a void
            GameObject ground = MeshKit.Box(transform, "GroundPlane", new Vector3(400f, 0.2f, 600f),
                new Vector3(0f, -0.35f, -150f), m_groundMat, false, true);
            m_ground = ground.transform;

            ResetWorld();
        }

        Chunk BuildChunk(int index)
        {
            Chunk chunk = new Chunk();
            GameObject root = new GameObject("Chunk_" + index);
            root.transform.SetParent(transform, false);
            chunk.Root = root.transform;

            // road slab
            MeshKit.Box(chunk.Root, "Road", new Vector3(GameConfig.RoadWidth, 0.3f, GameConfig.ChunkLength),
                new Vector3(0f, -0.15f, 0f), m_roadMat, false, true);

            float kerbX = GameConfig.RoadWidth * 0.5f + 0.85f;
            MeshKit.Box(chunk.Root, "KerbL", new Vector3(1.7f, 0.26f, GameConfig.ChunkLength),
                new Vector3(-kerbX, 0.08f, 0f), m_walkMat, false, true);
            MeshKit.Box(chunk.Root, "KerbR", new Vector3(1.7f, 0.26f, GameConfig.ChunkLength),
                new Vector3(kerbX, 0.08f, 0f), m_walkMat, false, true);

            // buildings: 5 slots per side
            chunk.Buildings = new Transform[10];
            chunk.BuildingMats = new Material[10];
            int b = 0;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 5; i++)
                {
                    Material mat = new Material(MaterialLibrary.Lit(Color.white, TextureFactory.Facade(Random.Range(0, 4))));
                    mat.name = "ZR_Facade";
                    GameObject building = MeshKit.Box(chunk.Root, "Building", Vector3.one, Vector3.zero, mat, false, false);
                    chunk.Buildings[b] = building.transform;
                    chunk.BuildingMats[b] = mat;
                    b++;
                }
            }

            // rubble / props
            chunk.Props = new Transform[10];
            for (int i = 0; i < 10; i++)
            {
                Material mat = (i % 3 == 0) ? m_rustMat : m_concreteMat;
                GameObject prop = MeshKit.Box(chunk.Root, "Rubble", Vector3.one, Vector3.zero, mat, true, true);
                chunk.Props[i] = prop.transform;
            }

            // street lamps
            chunk.Lamps = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                Transform lamp = MeshKit.Pivot(chunk.Root, "Lamp", Vector3.zero);
                MeshKit.Box(lamp, "Pole", new Vector3(0.12f, 5.2f, 0.12f), new Vector3(0f, 2.6f, 0f), m_darkMat);
                MeshKit.Box(lamp, "Arm", new Vector3(0.9f, 0.1f, 0.1f), new Vector3(0.42f, 5.1f, 0f), m_darkMat);
                MeshKit.Box(lamp, "Head", new Vector3(0.5f, 0.14f, 0.28f), new Vector3(0.8f, 5.04f, 0f), m_lampMat, false);
                chunk.Lamps[i] = lamp;
            }

            return chunk;
        }

        void RandomizeChunk(Chunk chunk)
        {
            for (int i = 0; i < chunk.Buildings.Length; i++)
            {
                int side = i < 5 ? -1 : 1;
                int slot = i % 5;
                float w = 3f + Random.value * 5f;
                float h = 5f + Random.value * 22f;
                float d = 4f + Random.value * 8f;

                Transform t = chunk.Buildings[i];
                t.localScale = new Vector3(w, h, d);
                t.localPosition = new Vector3(
                    side * (7.5f + Random.value * 6f + w * 0.5f),
                    h * 0.5f - 1.5f,
                    (slot - 2) * 6f + (Random.value - 0.5f) * 2.5f);
                t.localRotation = Quaternion.Euler(
                    (Random.value - 0.5f) * 3f,
                    (Random.value - 0.5f) * 9f,
                    (Random.value - 0.5f) * 3f);

                Material mat = chunk.BuildingMats[i];
                if (mat != null)
                {
                    mat.mainTextureScale = new Vector2(
                        Mathf.Max(1f, Mathf.Round(w / 3.2f)),
                        Mathf.Max(1f, Mathf.Round(h / 3.4f)));
                }
            }

            for (int i = 0; i < chunk.Props.Length; i++)
            {
                Transform t = chunk.Props[i];
                t.localScale = new Vector3(
                    0.3f + Random.value * 0.7f,
                    0.2f + Random.value * 0.5f,
                    0.3f + Random.value * 0.7f);
                t.localPosition = new Vector3(
                    (Random.value > 0.5f ? 1f : -1f) * (4.2f + Random.value * 1.8f),
                    0.2f + Random.value * 0.1f,
                    (Random.value - 0.5f) * 28f);
                t.localRotation = Quaternion.Euler(0f, Random.value * 360f, 0f);
            }

            for (int i = 0; i < chunk.Lamps.Length; i++)
            {
                int side = i % 2 == 0 ? -1 : 1;
                Transform t = chunk.Lamps[i];
                t.localPosition = new Vector3(side * 5.4f, 0f, (i - 1) * 10f + 3f);
                t.localScale = new Vector3(side, 1f, 1f);
                bool on = Random.value > 0.25f;
                t.GetChild(2).gameObject.SetActive(on);
            }
        }

        public void ResetWorld()
        {
            for (int i = 0; i < m_chunks.Length; i++)
            {
                m_chunks[i].Root.localPosition = new Vector3(0f, 0f, 15f - i * GameConfig.ChunkLength);
                RandomizeChunk(m_chunks[i]);
            }
            if (m_ground != null) m_ground.localPosition = new Vector3(0f, -0.35f, -150f);
        }

        /// <summary>Scroll everything toward the player. dz is metres this step.</summary>
        public void Scroll(float dz)
        {
            float wrap = GameConfig.ChunkCount * GameConfig.ChunkLength;
            for (int i = 0; i < m_chunks.Length; i++)
            {
                Chunk c = m_chunks[i];
                Vector3 p = c.Root.localPosition;
                p.z += dz;
                if (p.z - GameConfig.ChunkLength * 0.5f > 16f)
                {
                    p.z -= wrap;
                    RandomizeChunk(c);
                }
                c.Root.localPosition = p;
            }

            if (m_ground != null)
            {
                Vector3 g = m_ground.localPosition;
                g.z += dz;
                if (g.z > 0f) g.z -= 600f;
                m_ground.localPosition = g;
            }
        }

        /// <summary>Re-grade road / building colours for the active sector.</summary>
        public void ApplySector(SectorDef sector)
        {
            if (m_roadMat != null) m_roadMat.SetColor("_Color", sector.RoadTint);
            if (m_roadMat != null) m_roadMat.SetColor("_BaseColor", sector.RoadTint);
            if (m_walkMat != null) m_walkMat.SetColor("_Color", sector.RoadTint);
            if (m_walkMat != null) m_walkMat.SetColor("_BaseColor", sector.RoadTint);

            for (int i = 0; i < m_chunks.Length; i++)
            {
                for (int b = 0; b < m_chunks[i].BuildingMats.Length; b++)
                {
                    Material mat = m_chunks[i].BuildingMats[b];
                    if (mat == null) continue;
                    mat.SetColor("_Color", sector.BuildingTint);
                    mat.SetColor("_BaseColor", sector.BuildingTint);
                }
            }
        }
    }
}
