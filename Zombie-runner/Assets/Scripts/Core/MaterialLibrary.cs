using System.Collections.Generic;
using UnityEngine;

namespace ZombieRunner
{
    /// <summary>
    /// Creates materials without assuming a render pipeline.
    /// Tries Built-in "Standard" first, then URP / HDRP equivalents, so the
    /// project renders correctly in Built-in RP *and* URP projects.
    /// </summary>
    public static class MaterialLibrary
    {
        static Shader s_lit;
        static Shader s_unlit;
        static readonly Dictionary<string, Material> s_cache = new Dictionary<string, Material>();

        public static bool UsingScriptablePipeline
        {
            get { return UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null; }
        }

        public static Shader LitShader
        {
            get
            {
                if (s_lit == null)
                {
                    s_lit = FindShader(
                        "Standard",
                        "Universal Render Pipeline/Lit",
                        "HDRP/Lit",
                        "Diffuse",
                        "Legacy Shaders/Diffuse");
                }
                return s_lit;
            }
        }

        /// <summary>Unlit shader that supports alpha (used for bolts / decals / FX).</summary>
        public static Shader UnlitShader
        {
            get
            {
                if (s_unlit == null)
                {
                    s_unlit = FindShader(
                        "Sprites/Default",
                        "Unlit/Transparent",
                        "Universal Render Pipeline/Unlit",
                        "Unlit/Color");
                }
                return s_unlit;
            }
        }

        static Shader FindShader(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Shader shader = Shader.Find(names[i]);
                if (shader != null) return shader;
            }
            // Last resort: whatever the engine considers the default.
            return Shader.Find("Hidden/InternalErrorShader");
        }

        // ------------------------------------------------------------------
        // Materials
        // ------------------------------------------------------------------

        public static Material Lit(Color color, float metallic = 0f, float smoothness = 0.12f)
        {
            return Lit(color, metallic, smoothness, Color.black, 0f, null);
        }

        public static Material Lit(Color color, Texture texture)
        {
            Material m = Lit(color, 0f, 0.08f, Color.black, 0f, texture);
            return m;
        }

        public static Material Emissive(Color baseColor, Color emission, float intensity = 1f, float metallic = 0f, float smoothness = 0.4f)
        {
            return Lit(baseColor, metallic, smoothness, emission, intensity, null);
        }

        static Material Lit(Color color, float metallic, float smoothness, Color emission, float intensity, Texture texture)
        {
            string key = string.Format("L{0}{1}{2}{3}{4}{5}",
                ColorKey(color), metallic, smoothness, ColorKey(emission), intensity,
                IdentityOf(texture));

            Material cached;
            if (s_cache.TryGetValue(key, out cached) && cached != null) return cached;

            Material mat = new Material(LitShader);
            mat.name = "ZR_Lit_" + key;
            ApplyColor(mat, color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Glossiness", Mathf.Lerp(0.05f, 0.95f, smoothness));
            mat.SetFloat("_Smoothness", Mathf.Lerp(0.05f, 0.95f, smoothness));
            if (texture != null)
            {
                mat.mainTexture = texture;
                mat.SetTexture("_BaseMap", texture);
            }
            if (emission.maxColorComponent > 0.001f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission * intensity);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            s_cache[key] = mat;
            return mat;
        }

        /// <summary>Alpha-blended unlit material (FX, decals, tracers).</summary>
        public static Material Unlit(Color color)
        {
            string key = "U" + ColorKey(color);
            Material cached;
            if (s_cache.TryGetValue(key, out cached) && cached != null) return cached;

            Material mat = new Material(UnlitShader);
            mat.name = "ZR_Unlit_" + key;
            ApplyColor(mat, color);
            mat.renderQueue = 3000;
            s_cache[key] = mat;
            return mat;
        }

        /// <summary>Same as Unlit() but with its own instance (per-effect fading).</summary>
        public static Material UnlitInstance(Color color)
        {
            Material mat = new Material(UnlitShader);
            mat.name = "ZR_UnlitFx";
            ApplyColor(mat, color);
            mat.renderQueue = 3000;
            return mat;
        }

        /// <summary>
        /// Stable identity for a texture used in cache keys.
        /// Deliberately avoids Object.GetInstanceID(): Unity 6.6 marks it obsolete-as-error
        /// ("Use GetEntityId instead"), and GetEntityId does not exist in older editors.
        /// </summary>
        static string IdentityOf(Object obj)
        {
            if (obj == null) return "0";
            return obj.name + "#" + System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }

        static void ApplyColor(Material mat, Color color)
        {
            // Built-in RP uses _Color, URP/SRP use _BaseColor.
            mat.SetColor("_Color", color);
            mat.SetColor("_BaseColor", color);
        }

        static string ColorKey(Color c)
        {
            return Mathf.RoundToInt(c.r * 255f) + "_" + Mathf.RoundToInt(c.g * 255f) + "_" +
                   Mathf.RoundToInt(c.b * 255f) + "_" + Mathf.RoundToInt(c.a * 255f);
        }

        public static void ClearCache()
        {
            s_cache.Clear();
        }
    }

    /// <summary>
    /// Small procedural textures generated at startup — the port ships no art
    /// assets, exactly like the browser build (which painted everything onto a
    /// 2D canvas at boot).
    /// </summary>
    public static class TextureFactory
    {
        static Texture2D s_road;
        static Texture2D s_sidewalk;
        static Texture2D[] s_facades;

        public static Texture2D Road
        {
            get
            {
                if (s_road == null) s_road = BuildRoad();
                return s_road;
            }
        }

        public static Texture2D Sidewalk
        {
            get
            {
                if (s_sidewalk == null) s_sidewalk = BuildSidewalk();
                return s_sidewalk;
            }
        }

        public static Texture2D Facade(int variant)
        {
            if (s_facades == null) s_facades = new Texture2D[4];
            variant = Mathf.Clamp(variant, 0, 3);
            if (s_facades[variant] == null) s_facades[variant] = BuildFacade(variant);
            return s_facades[variant];
        }

        static Texture2D NewTex(int w, int h, bool mips)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, mips);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Trilinear;
            tex.anisoLevel = 4;
            return tex;
        }

        static Color32 C(int r, int g, int b, int a = 255)
        {
            return new Color32((byte)r, (byte)g, (byte)b, (byte)a);
        }

        // ---------------------------------------------------------------- road
        static Texture2D BuildRoad()
        {
            const int w = 256, h = 512;
            Texture2D tex = NewTex(w, h, true);
            Color32[] px = new Color32[w * h];
            System.Random rng = new System.Random(1337);

            for (int i = 0; i < px.Length; i++) px[i] = C(36, 42, 48);

            // asphalt grain
            for (int i = 0; i < 26000; i++)
            {
                int x = rng.Next(w), y = rng.Next(h);
                int idx = y * w + x;
                byte v = (byte)(rng.Next(2) == 0 ? 255 : 0);
                int a = rng.Next(14);
                Color32 c = px[idx];
                px[idx] = new Color32(
                    (byte)Mathf.Clamp(c.r + (v == 255 ? a : -a), 0, 255),
                    (byte)Mathf.Clamp(c.g + (v == 255 ? a : -a), 0, 255),
                    (byte)Mathf.Clamp(c.b + (v == 255 ? a : -a), 0, 255), 255);
            }

            // 25.6 px per metre (road is 10 m wide => 256 px)
            int laneOffset = Mathf.RoundToInt(1.3f * 25.6f);

            // dashed lane separators
            DrawDashedLine(px, w, h, w / 2 - laneOffset, rng);
            DrawDashedLine(px, w, h, w / 2 + laneOffset, rng);

            // solid edge lines
            DrawSolidLine(px, w, h, 6);
            DrawSolidLine(px, w, h, w - 9);

            // cracks
            for (int c = 0; c < 9; c++)
            {
                float x = rng.Next(w), y = rng.Next(h);
                for (int k = 0; k < 22; k++)
                {
                    int ix = Mathf.Clamp(Mathf.RoundToInt(x), 0, w - 1);
                    int iy = Mathf.Clamp(Mathf.RoundToInt(y), 0, h - 1);
                    int idx = iy * w + ix;
                    px[idx] = C(16, 18, 20);
                    x += (float)(rng.NextDouble() - 0.5) * 12f;
                    y += (float)(rng.NextDouble() - 0.5) * 14f;
                }
            }

            tex.SetPixels32(px);
            tex.Apply(true, false);
            return tex;
        }

        static void DrawDashedLine(Color32[] px, int w, int h, int cx, System.Random rng)
        {
            for (int y = 0; y < h; y += 108)
            {
                for (int yy = y; yy < Mathf.Min(h, y + 58); yy++)
                {
                    for (int x = cx - 2; x <= cx + 2; x++)
                    {
                        if (x < 0 || x >= w) continue;
                        int idx = yy * w + x;
                        bool wear = rng.Next(10) == 0;
                        px[idx] = wear ? C(120, 118, 104) : C(206, 204, 186);
                    }
                }
            }
        }

        static void DrawSolidLine(Color32[] px, int w, int h, int x0)
        {
            for (int y = 0; y < h; y++)
            {
                for (int x = x0; x < x0 + 4; x++)
                {
                    if (x < 0 || x >= w) continue;
                    px[y * w + x] = C(150, 148, 132);
                }
            }
        }

        // ------------------------------------------------------------ sidewalk
        static Texture2D BuildSidewalk()
        {
            const int s = 128;
            Texture2D tex = NewTex(s, s, true);
            Color32[] px = new Color32[s * s];
            System.Random rng = new System.Random(7);

            for (int i = 0; i < px.Length; i++) px[i] = C(58, 61, 64);

            for (int i = 0; i < 4000; i++)
            {
                int idx = rng.Next(px.Length);
                int a = rng.Next(16);
                Color32 c = px[idx];
                bool plus = rng.Next(2) == 0;
                px[idx] = new Color32(
                    (byte)Mathf.Clamp(c.r + (plus ? a : -a), 0, 255),
                    (byte)Mathf.Clamp(c.g + (plus ? a : -a), 0, 255),
                    (byte)Mathf.Clamp(c.b + (plus ? a : -a), 0, 255), 255);
            }

            // slab joints
            for (int x = 0; x < s; x++)
            {
                for (int y = 0; y < s; y++)
                {
                    if (x % 32 == 0 || y % 32 == 0 || x % 32 == 1 || y % 32 == 1)
                    {
                        px[y * s + x] = C(30, 32, 34);
                    }
                }
            }

            tex.SetPixels32(px);
            tex.Apply(true, false);
            return tex;
        }

        // -------------------------------------------------------------- facade
        static Texture2D BuildFacade(int variant)
        {
            const int w = 128, h = 256;
            Texture2D tex = NewTex(w, h, true);
            Color32[] px = new Color32[w * h];
            System.Random rng = new System.Random(100 + variant * 31);

            Color32 baseColor;
            Color32 litColor;
            switch (variant)
            {
                case 0: baseColor = C(59, 65, 72); litColor = C(255, 178, 100); break;
                case 1: baseColor = C(74, 58, 52); litColor = C(255, 201, 138); break;
                case 2: baseColor = C(51, 60, 66); litColor = C(143, 208, 224); break;
                default: baseColor = C(65, 57, 60); litColor = C(255, 208, 138); break;
            }

            for (int i = 0; i < px.Length; i++) px[i] = baseColor;

            // grime noise
            for (int i = 0; i < 9000; i++)
            {
                int idx = rng.Next(px.Length);
                Color32 c = px[idx];
                int a = rng.Next(18);
                px[idx] = new Color32(
                    (byte)Mathf.Max(0, c.r - a),
                    (byte)Mathf.Max(0, c.g - a),
                    (byte)Mathf.Max(0, c.b - a), 255);
            }

            // vertical grime streaks
            for (int i = 0; i < 16; i++)
            {
                int x = rng.Next(w);
                int width = 2 + rng.Next(6);
                int alpha = 12 + rng.Next(26);
                for (int y = 0; y < h; y++)
                {
                    for (int xx = x; xx < Mathf.Min(w, x + width); xx++)
                    {
                        Color32 c = px[y * w + xx];
                        px[y * w + xx] = new Color32(
                            (byte)Mathf.Max(0, c.r - alpha),
                            (byte)Mathf.Max(0, c.g - alpha),
                            (byte)Mathf.Max(0, c.b - alpha), 255);
                    }
                }
            }

            // window grid: 4 cols x 10 rows
            const int cols = 4, rows = 10, pad = 10;
            float cw = (w - pad * 2f) / cols;
            float ch = (h - pad * 2f) / rows;
            for (int r = 0; r < rows; r++)
            {
                for (int cidx = 0; cidx < cols; cidx++)
                {
                    int x0 = Mathf.RoundToInt(pad + cidx * cw + 4);
                    int y0 = Mathf.RoundToInt(pad + r * ch + 5);
                    int ww = Mathf.RoundToInt(cw - 12);
                    int hh = Mathf.RoundToInt(ch - 14);
                    bool lit = rng.NextDouble() > 0.82;
                    bool dark = rng.NextDouble() > 0.45;
                    Color32 window = lit ? litColor : (dark ? C(11, 16, 22) : C(20, 29, 36));
                    for (int y = y0; y < y0 + hh; y++)
                    {
                        for (int x = x0; x < x0 + ww; x++)
                        {
                            if (x < 0 || x >= w || y < 0 || y >= h) continue;
                            px[y * w + x] = window;
                        }
                    }
                    if (lit)
                    {
                        // soft bloom around lit windows
                        for (int y = y0 - 2; y < y0 + hh + 2; y++)
                        {
                            for (int x = x0 - 2; x < x0 + ww + 2; x++)
                            {
                                if (x < 0 || x >= w || y < 0 || y >= h) continue;
                                if (x >= x0 && x < x0 + ww && y >= y0 && y < y0 + hh) continue;
                                px[y * w + x] = Color32.Lerp(px[y * w + x], litColor, 0.22f);
                            }
                        }
                    }
                }
            }

            tex.SetPixels32(px);
            tex.Apply(true, false);
            return tex;
        }
    }
}
