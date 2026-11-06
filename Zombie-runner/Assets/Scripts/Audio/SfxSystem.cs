using UnityEngine;

namespace ZombieRunner
{
    /// <summary>
    /// 100% procedural audio — every sound is synthesised into an AudioClip at
    /// boot, so the port ships with zero audio assets (same approach as the
    /// WebAudio synth in the browser build).
    /// </summary>
    public class SfxSystem : MonoBehaviour
    {
        public static SfxSystem Instance { get; private set; }

        public bool Muted;

        AudioSource m_source;
        AudioSource m_ambience;

        AudioClip m_shot, m_flesh, m_jump, m_slide, m_land, m_coin, m_pickup,
                  m_hurt, m_impact, m_ui, m_dry, m_armor, m_revive, m_combo, m_ambienceClip;

        const int SampleRate = 44100;

        void Awake()
        {
            Instance = this;
            Build();
        }

        void Build()
        {
            m_source = gameObject.AddComponent<AudioSource>();
            m_source.playOnAwake = false;
            m_source.spatialBlend = 0f;
            m_source.volume = 0.85f;

            m_shot = BuildShot();
            m_flesh = BuildFlesh();
            m_jump = BuildJump();
            m_slide = BuildSlide();
            m_land = BuildLand();
            m_coin = BuildCoin();
            m_pickup = BuildPickup();
            m_hurt = BuildHurt();
            m_impact = BuildImpact();
            m_ui = BuildUi();
            m_dry = BuildDry();
            m_armor = BuildArmor();
            m_revive = BuildRevive();
            m_combo = BuildCombo();
            m_ambienceClip = BuildAmbience();

            m_ambience = gameObject.AddComponent<AudioSource>();
            m_ambience.playOnAwake = false;
            m_ambience.loop = true;
            m_ambience.volume = 0.22f;
            m_ambience.spatialBlend = 0f;
            m_ambience.clip = m_ambienceClip;
            m_ambience.Play();
        }

        // ------------------------------------------------------------------
        //  Play API
        // ------------------------------------------------------------------
        void Play(AudioClip clip, float volume)
        {
            if (Muted || clip == null || m_source == null) return;
            m_source.PlayOneShot(clip, volume);
        }

        public void SetMuted(bool muted)
        {
            Muted = muted;
            if (m_ambience != null) m_ambience.mute = muted;
        }

        public void Shot() { Play(m_shot, 0.9f); }
        public void Flesh() { Play(m_flesh, 0.65f); }
        public void Jump() { Play(m_jump, 0.5f); }
        public void Slide() { Play(m_slide, 0.6f); }
        public void Land() { Play(m_land, 0.5f); }
        public void Coin() { Play(m_coin, 0.55f); }
        public void Pickup() { Play(m_pickup, 0.55f); }
        public void Hurt() { Play(m_hurt, 0.7f); }
        public void Impact() { Play(m_impact, 0.8f); }
        public void UiTick() { Play(m_ui, 0.45f); }
        public void Dry() { Play(m_dry, 0.5f); }
        public void Armor() { Play(m_armor, 0.6f); }
        public void Revive() { Play(m_revive, 0.8f); }
        public void Combo(int level) { Play(m_combo, 0.45f + Mathf.Min(0.4f, level * 0.02f)); }

        // ------------------------------------------------------------------
        //  Synthesis helpers
        // ------------------------------------------------------------------
        static AudioClip Clip(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Noise(System.Random rng)
        {
            return (float)(rng.NextDouble() * 2.0 - 1.0);
        }

        /// <summary>Exponential decay envelope with a short attack.</summary>
        static float Env(float t, float attack, float decay)
        {
            if (t < attack) return attack <= 0.0001f ? 1f : t / attack;
            return Mathf.Exp(-(t - attack) / Mathf.Max(0.0001f, decay));
        }

        // ------------------------------------------------------------------
        //  Individual sounds
        // ------------------------------------------------------------------
        static AudioClip BuildShot()
        {
            const float duration = 0.28f;
            int n = (int)(SampleRate * duration);
            float[] data = new float[n];
            System.Random rng = new System.Random(1234);
            float lp = 0f;
            double phase = 0.0, subPhase = 0.0;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float noise = Noise(rng);
                lp += (noise - lp) * 0.28f;
                float crack = (noise - lp) * Mathf.Exp(-t * 24f) * 0.9f;

                float f1 = 190f * Mathf.Exp(-t * 9f) + 48f;
                phase += 2.0 * Mathf.PI * f1 / SampleRate;
                float thump = Mathf.Sin((float)phase) * Mathf.Exp(-t * 15f) * 0.55f;

                float f2 = 85f * Mathf.Exp(-t * 6f) + 38f;
                subPhase += 2.0 * Mathf.PI * f2 / SampleRate;
                float sub = Mathf.Sin((float)subPhase) * Mathf.Exp(-t * 9f) * 0.45f;

                data[i] = Mathf.Clamp(crack + thump + sub, -1f, 1f);
            }
            return Clip("ZR_Shot", data);
        }

        static AudioClip BuildFlesh()
        {
            const float duration = 0.24f;
            int n = (int)(SampleRate * duration);
            float[] data = new float[n];
            System.Random rng = new System.Random(99);
            float lp = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float noise = Noise(rng);
                float cutoff = Mathf.Lerp(0.25f, 0.04f, Mathf.Clamp01(t / duration));
                lp += (noise - lp) * cutoff;
                data[i] = lp * Env(t, 0.004f, 0.09f) * 1.4f;
            }
            return Clip("ZR_Flesh", data);
        }

        static AudioClip BuildJump()
        {
            const float duration = 0.26f;
            int n = (int)(SampleRate * duration);
            float[] data = new float[n];
            System.Random rng = new System.Random(555);
            float bp = 0f;
            double phase = 0;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float noise = Noise(rng);
                bp += (noise - bp) * Mathf.Lerp(0.08f, 0.5f, Mathf.Clamp01(t / 0.16f));
                float whoosh = bp * Env(t, 0.01f, 0.1f) * 0.5f;

                float f = Mathf.Lerp(300f, 620f, Mathf.Clamp01(t / 0.14f));
                phase += 2.0 * Mathf.PI * f / SampleRate;
                float blip = Mathf.Sin((float)phase) * Env(t, 0.004f, 0.08f) * 0.25f;

                data[i] = Mathf.Clamp(whoosh + blip, -1f, 1f);
            }
            return Clip("ZR_Jump", data);
        }

        static AudioClip BuildSlide()
        {
            const float duration = 0.52f;
            int n = (int)(SampleRate * duration);
            float[] data = new float[n];
            System.Random rng = new System.Random(77);
            float lp = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float noise = Noise(rng);
                float cutoff = Mathf.Lerp(0.5f, 0.03f, Mathf.Clamp01(t / duration));
                lp += (noise - lp) * cutoff;
                data[i] = lp * Env(t, 0.01f, 0.22f) * 1.2f;
            }
            return Clip("ZR_Slide", data);
        }

        static AudioClip BuildLand()
        {
            const float duration = 0.18f;
            int n = (int)(SampleRate * duration);
            float[] data = new float[n];
            System.Random rng = new System.Random(31);
            float lp = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                lp += (Noise(rng) - lp) * 0.06f;
                data[i] = lp * Env(t, 0.004f, 0.05f) * 1.6f;
            }
            return Clip("ZR_Land", data);
        }

        static AudioClip BuildCoin()
        {
            const float duration = 0.34f;
            int n = (int)(SampleRate * duration);
            float[] data = new float[n];
            double p1 = 0, p2 = 0;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                p1 += 2.0 * Mathf.PI * 1318.5 / SampleRate;
                p2 += 2.0 * Mathf.PI * 1975.5 / SampleRate;
                float a = Mathf.Sin((float)p1) * Env(t, 0.004f, 0.07f);
                float b = t > 0.055f ? Mathf.Sin((float)p2) * Env(t - 0.055f, 0.004f, 0.09f) : 0f;
                data[i] = Mathf.Clamp((a + b) * 0.35f, -1f, 1f);
            }
            return Clip("ZR_Coin", data);
        }

        static AudioClip BuildPickup()
        {
            const float duration = 0.2f;
            int n = (int)(SampleRate * duration);
            float[] data = new float[n];
            double phase = 0;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float f = Mathf.Lerp(420f, 880f, Mathf.Clamp01(t / 0.09f));
                phase += 2.0 * Mathf.PI * f / SampleRate;
                float square = Mathf.Sign(Mathf.Sin((float)phase)) * 0.5f;
                data[i] = square * Env(t, 0.004f, 0.06f) * 0.5f;
            }
            return Clip("ZR_Pickup", data);
        }

        static AudioClip BuildHurt()
        {
            const float duration = 0.36f;
            int n = (int)(SampleRate * duration);
            float[] data = new float[n];
            double phase = 0;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float f = Mathf.Lerp(300f, 90f, Mathf.Clamp01(t / 0.25f));
                phase += 2.0 * Mathf.PI * f / SampleRate;
                float saw = Mathf.Repeat((float)phase / (2f * Mathf.PI), 1f) * 2f - 1f;
                data[i] = saw * Env(t, 0.005f, 0.12f) * 0.5f;
            }
            return Clip("ZR_Hurt", data);
        }

        static AudioClip BuildImpact()
        {
            const float duration = 0.5f;
            int n = (int)(SampleRate * duration);
            float[] data = new float[n];
            System.Random rng = new System.Random(4242);
            float lp = 0f;
            double phase = 0;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                lp += (Noise(rng) - lp) * 0.05f;
                float thud = lp * Env(t, 0.01f, 0.15f) * 1.4f;

                float f = Mathf.Lerp(110f, 40f, Mathf.Clamp01(t / 0.35f));
                phase += 2.0 * Mathf.PI * f / SampleRate;
                float boom = Mathf.Sin((float)phase) * Env(t, 0.01f, 0.16f) * 0.55f;

                data[i] = Mathf.Clamp(thud + boom, -1f, 1f);
            }
            return Clip("ZR_Impact", data);
        }

        static AudioClip BuildUi()
        {
            const float duration = 0.09f;
            int n = (int)(SampleRate * duration);
            float[] data = new float[n];
            double phase = 0;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                phase += 2.0 * Mathf.PI * 660.0 / SampleRate;
                data[i] = Mathf.Sign(Mathf.Sin((float)phase)) * Env(t, 0.003f, 0.025f) * 0.2f;
            }
            return Clip("ZR_Ui", data);
        }

        static AudioClip BuildDry()
        {
            const float duration = 0.08f;
            int n = (int)(SampleRate * duration);
            float[] data = new float[n];
            System.Random rng = new System.Random(8);
            float lp = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float noise = Noise(rng);
                lp += (noise - lp) * 0.9f;
                float hp = noise - lp;
                data[i] = hp * Env(t, 0.002f, 0.02f) * 0.5f;
            }
            return Clip("ZR_Dry", data);
        }

        static AudioClip BuildArmor()
        {
            const float duration = 0.22f;
            int n = (int)(SampleRate * duration);
            float[] data = new float[n];
            double phase = 0;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float f = Mathf.Lerp(720f, 300f, Mathf.Clamp01(t / 0.12f));
                phase += 2.0 * Mathf.PI * f / SampleRate;
                float square = Mathf.Sign(Mathf.Sin((float)phase));
                data[i] = square * Env(t, 0.002f, 0.07f) * 0.28f;
            }
            return Clip("ZR_Armor", data);
        }

        static AudioClip BuildRevive()
        {
            const float duration = 0.9f;
            int n = (int)(SampleRate * duration);
            float[] data = new float[n];
            float[] notes = { 392f, 523.25f, 659.25f, 783.99f };
            double[] phases = new double[notes.Length];

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float sum = 0f;
                for (int k = 0; k < notes.Length; k++)
                {
                    float start = k * 0.09f;
                    if (t < start) continue;
                    phases[k] += 2.0 * Mathf.PI * notes[k] / SampleRate;
                    sum += Mathf.Sin((float)phases[k]) * Env(t - start, 0.01f, 0.28f);
                }
                data[i] = Mathf.Clamp(sum * 0.18f, -1f, 1f);
            }
            return Clip("ZR_Revive", data);
        }

        static AudioClip BuildCombo()
        {
            const float duration = 0.4f;
            int n = (int)(SampleRate * duration);
            float[] data = new float[n];
            float[] notes = { 880f, 1108.7f, 1318.5f };
            double[] phases = new double[notes.Length];

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float sum = 0f;
                for (int k = 0; k < notes.Length; k++)
                {
                    float start = k * 0.04f;
                    if (t < start) continue;
                    phases[k] += 2.0 * Mathf.PI * notes[k] / SampleRate;
                    sum += Mathf.Sin((float)phases[k]) * Env(t - start, 0.004f, 0.09f);
                }
                data[i] = Mathf.Clamp(sum * 0.2f, -1f, 1f);
            }
            return Clip("ZR_Combo", data);
        }

        static AudioClip BuildAmbience()
        {
            const float duration = 4f;
            int n = (int)(SampleRate * duration);
            float[] data = new float[n];
            System.Random rng = new System.Random(2024);
            float lp = 0f;
            double lfoPhase = 0;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                lp += (Noise(rng) - lp) * 0.012f;
                lfoPhase += 2.0 * Mathf.PI * 0.09 / SampleRate;
                float lfo = 0.6f + 0.4f * Mathf.Sin((float)lfoPhase);
                // cross-fade the loop seam
                float fade = Mathf.Min(1f, Mathf.Min(t, duration - t) / 0.25f);
                data[i] = lp * lfo * 1.6f * fade;
            }
            return Clip("ZR_Ambience", data);
        }
    }
}
