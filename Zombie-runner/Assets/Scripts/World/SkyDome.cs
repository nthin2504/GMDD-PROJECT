using UnityEngine;

namespace ZombieRunner
{
    /// <summary>
    /// Gradient sky dome with a horizon glow band — the Unity replacement for the
    /// tiny gradient shader used by the browser build. Colours live in the mesh's
    /// vertex stream, so no custom shader assets are required and it works in
    /// Built-in RP and URP alike.
    /// </summary>
    public class SkyDome : MonoBehaviour
    {
        Mesh m_mesh;
        MeshRenderer m_renderer;
        Color m_top, m_bottom, m_glow;

        public static SkyDome Create(Transform parent, float radius)
        {
            GameObject go = new GameObject("SkyDome");
            go.transform.SetParent(parent, false);
            SkyDome dome = go.AddComponent<SkyDome>();
            dome.Build(radius);
            return dome;
        }

        void Build(float radius)
        {
            const int segments = 32;
            const int rings = 24;

            Vector3[] verts = new Vector3[(rings + 1) * (segments + 1)];
            int[] tris = new int[rings * segments * 6];
            Color[] colors = new Color[verts.Length];

            int v = 0;
            for (int r = 0; r <= rings; r++)
            {
                float phi = Mathf.Lerp(-0.35f * Mathf.PI, 0.5f * Mathf.PI, r / (float)rings);
                float y = Mathf.Sin(phi) * radius;
                float ringRadius = Mathf.Cos(phi) * radius;
                for (int s = 0; s <= segments; s++)
                {
                    float theta = (s / (float)segments) * Mathf.PI * 2f;
                    verts[v] = new Vector3(Mathf.Cos(theta) * ringRadius, y, Mathf.Sin(theta) * ringRadius);
                    colors[v] = Color.white;    // filled in by ApplyColors
                    v++;
                }
            }

            int t = 0;
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int a = r * (segments + 1) + s;
                    int b = a + segments + 1;
                    tris[t++] = a; tris[t++] = b; tris[t++] = a + 1;
                    tris[t++] = a + 1; tris[t++] = b; tris[t++] = b + 1;
                }
            }

            m_mesh = new Mesh();
            m_mesh.name = "SkyDome";
            m_mesh.vertices = verts;
            m_mesh.triangles = tris;
            m_mesh.colors = colors;
            m_mesh.RecalculateNormals();
            m_mesh.bounds = new Bounds(Vector3.zero, Vector3.one * radius * 2.2f);

            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = m_mesh;

            m_renderer = gameObject.AddComponent<MeshRenderer>();
            // Sprites/Default is unlit, double sided and uses vertex colours, which
            // is exactly what the dome needs. It ships with every pipeline.
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = MaterialLibrary.UnlitShader;
            Material mat = new Material(shader);
            mat.name = "ZR_SkyDome";
            mat.SetColor("_Color", Color.white);
            mat.renderQueue = 2500;
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
            m_renderer.sharedMaterial = mat;
            m_renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_renderer.receiveShadows = false;
            m_renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            m_renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            gameObject.transform.localPosition = Vector3.zero;
        }

        public void ApplyColors(Color top, Color bottom, Color glow)
        {
            m_top = top;
            m_bottom = bottom;
            m_glow = glow;
            if (m_mesh == null) return;

            Vector3[] verts = m_mesh.vertices;
            Color[] colors = new Color[verts.Length];
            float radius = Mathf.Max(1f, m_mesh.bounds.extents.y);

            for (int i = 0; i < verts.Length; i++)
            {
                float h = verts[i].y / radius;
                float t = Mathf.Pow(Mathf.Clamp01(h * 1.15f), 0.75f);
                Color c = Color.Lerp(bottom, top, t);
                float band = Mathf.Exp(-Mathf.Pow((h - 0.015f) * 9f, 2f));
                c += glow * (band * 0.55f);
                c.a = 1f;
                colors[i] = c;
            }
            m_mesh.colors = colors;
        }

        /// <summary>Keeps the dome centred on the camera without scaling it.</summary>
        public void FollowCamera(Transform cameraTransform)
        {
            if (cameraTransform == null) return;
            transform.position = new Vector3(cameraTransform.position.x * 0.35f, 0f, 0f);
        }
    }
}
