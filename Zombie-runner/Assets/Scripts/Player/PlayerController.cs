using UnityEngine;

namespace ZombieRunner
{
    /// <summary>
    /// The survivor: 3-lane snap movement, jump, slide, procedural run cycle and
    /// the little gun rig. Everything is driven by GameManager (no Update()),
    /// which keeps the simulation order deterministic.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        // ---- rig ---------------------------------------------------------
        public Transform Rig;
        public Transform Body;
        public Transform ArmL, ArmR, LegL, LegR, Gun;
        public Transform Muzzle;
        public GameObject MuzzleFlash;
        public Light MuzzleLight;
        public Light BodyLight;
        public MeshRenderer TorsoRenderer;
        public MeshRenderer HeadRenderer;

        Material m_cloth, m_skin, m_rust;

        // ---- state -------------------------------------------------------
        public int Lane = 1;
        public float TargetX;
        public float X;
        public float Y;
        public float Vy;
        public bool Grounded = true;
        public float SlideT;
        public float SlideCd;
        public float Invuln;
        public float RunPhase;
        public float GunRecoil;
        public bool FireHeld;
        public float JumpBuffer;
        public float SlideBuffer;

        float m_flashTime;
        float m_speed = GameConfig.StartSpeed;

        public bool Sliding { get { return SlideT > 0f && Grounded; } }

        public float BodyHeight
        {
            get { return Sliding ? GameConfig.SlideHeight : GameConfig.StandHeight; }
        }

        /// <summary>World position used for hits / pickups.</summary>
        public Vector3 FeetPosition
        {
            get { return new Vector3(X, Y, 0f); }
        }

        void Awake()
        {
            BuildRig();
        }

        // =================================================================
        //  Rig construction (mirrors buildPlayer() from the browser build)
        // =================================================================
        void BuildRig()
        {
            m_skin = MaterialLibrary.Lit(GameConfig.Hex(0xc98f63), 0f, 0.08f);
            m_cloth = MaterialLibrary.Emissive(GameConfig.Hex(0xa9713c), GameConfig.Hex(0x1d1005), 1.4f, 0f, 0.08f);
            m_rust = MaterialLibrary.Lit(GameConfig.Hex(0x7a4326), 0.2f, 0.25f);

            Material pants = MaterialLibrary.Lit(GameConfig.Hex(0x39445a), 0f, 0.06f);
            Material pack = MaterialLibrary.Lit(GameConfig.Hex(0x6b5730), 0f, 0.08f);
            Material dark = MaterialLibrary.Lit(GameConfig.Hex(0x1b1f24), 0.35f, 0.4f);
            Material gunMat = MaterialLibrary.Lit(GameConfig.Hex(0x24282c), 0.7f, 0.35f);

            Rig = transform;
            Body = MeshKit.Pivot(Rig, "Body", Vector3.zero);

            MeshKit.Box(Body, "Torso", new Vector3(0.66f, 0.78f, 0.36f), new Vector3(0f, 1.28f, 0f), m_cloth);
            MeshKit.Box(Body, "Hip", new Vector3(0.6f, 0.26f, 0.34f), new Vector3(0f, 0.9f, 0f), pants);
            MeshKit.Box(Body, "Head", new Vector3(0.36f, 0.36f, 0.34f), new Vector3(0f, 1.83f, 0f), m_skin);
            MeshKit.Box(Body, "Hair", new Vector3(0.38f, 0.1f, 0.36f), new Vector3(0f, 2.0f, 0f), dark);
            MeshKit.Box(Body, "Backpack", new Vector3(0.44f, 0.52f, 0.22f), new Vector3(0f, 1.34f, 0.26f), pack);
            MeshKit.Box(Body, "Scarf", new Vector3(0.5f, 0.12f, 0.38f), new Vector3(0f, 1.66f, 0f), m_rust);

            TorsoRenderer = Body.Find("Torso").GetComponent<MeshRenderer>();
            HeadRenderer = Body.Find("Head").GetComponent<MeshRenderer>();

            // arms pivot at the shoulders
            ArmL = MeshKit.Pivot(Body, "ArmL", new Vector3(-0.42f, 1.55f, 0f));
            ArmR = MeshKit.Pivot(Body, "ArmR", new Vector3(0.42f, 1.55f, 0f));
            MeshKit.Box(ArmL, "ArmLMesh", new Vector3(0.16f, 0.62f, 0.18f), new Vector3(0f, -0.31f, 0f), m_cloth);
            MeshKit.Box(ArmR, "ArmRMesh", new Vector3(0.16f, 0.62f, 0.18f), new Vector3(0f, -0.31f, 0f), m_cloth);

            // pistol in the right hand
            GameObject gunRoot = new GameObject("Gun");
            gunRoot.transform.SetParent(ArmR, false);
            gunRoot.transform.localPosition = new Vector3(0.05f, -0.6f, -0.12f);
            Gun = gunRoot.transform;

            MeshKit.Box(Gun, "GunBody", new Vector3(0.1f, 0.16f, 0.62f), new Vector3(0f, 0f, -0.12f), gunMat);
            MeshKit.Box(Gun, "GunMag", new Vector3(0.09f, 0.24f, 0.12f), new Vector3(0f, -0.16f, -0.02f), gunMat);
            MeshKit.Box(Gun, "GunBarrel", new Vector3(0.07f, 0.07f, 0.34f), new Vector3(0f, 0f, -0.5f), gunMat);

            Muzzle = MeshKit.Pivot(Gun, "Muzzle", new Vector3(0f, 0f, -0.72f));

            MuzzleFlash = MeshKit.Sphere(Muzzle, "MuzzleFlash", 0.32f, Vector3.zero,
                MaterialLibrary.Unlit(new Color(1f, 0.85f, 0.63f, 0.95f)), false, false);
            MuzzleFlash.SetActive(false);

            // legs pivot at the hips
            LegL = MeshKit.Pivot(Body, "LegL", new Vector3(-0.18f, 0.86f, 0f));
            LegR = MeshKit.Pivot(Body, "LegR", new Vector3(0.18f, 0.86f, 0f));
            MeshKit.Box(LegL, "LegLMesh", new Vector3(0.2f, 0.76f, 0.22f), new Vector3(0f, -0.38f, 0f), pants);
            MeshKit.Box(LegR, "LegRMesh", new Vector3(0.2f, 0.76f, 0.22f), new Vector3(0f, -0.38f, 0f), pants);
            MeshKit.Box(LegL, "BootL", new Vector3(0.22f, 0.14f, 0.32f), new Vector3(0f, -0.76f, -0.05f), dark);
            MeshKit.Box(LegR, "BootR", new Vector3(0.22f, 0.14f, 0.32f), new Vector3(0f, -0.76f, -0.05f), dark);

            // warm light that travels with the survivor
            GameObject lightGo = new GameObject("PlayerLight");
            lightGo.transform.SetParent(null, false);
            BodyLight = lightGo.AddComponent<Light>();
            BodyLight.type = LightType.Point;
            BodyLight.color = GameConfig.Hex(0xffd0a0);
            BodyLight.intensity = 2.4f;
            BodyLight.range = 9f;
            BodyLight.shadows = LightShadows.None;
            BodyLight.renderMode = LightRenderMode.ForceVertex;
            BodyLight.transform.position = new Vector3(-0.6f, 3.0f, 2.2f);

            GameObject muzzleGo = new GameObject("MuzzleLight");
            muzzleGo.transform.SetParent(Muzzle, false);
            MuzzleLight = muzzleGo.AddComponent<Light>();
            MuzzleLight.type = LightType.Point;
            MuzzleLight.color = GameConfig.Hex(0xffb060);
            MuzzleLight.intensity = 0f;
            MuzzleLight.range = 12f;
            MuzzleLight.shadows = LightShadows.None;
            MuzzleLight.renderMode = LightRenderMode.ForceVertex;
        }

        // =================================================================
        //  Input verbs
        // =================================================================
        public void ResetRun()
        {
            Lane = 1;
            X = 0f;
            TargetX = 0f;
            Y = 0f;
            Vy = 0f;
            Grounded = true;
            SlideT = 0f;
            SlideCd = 0f;
            Invuln = 0f;
            RunPhase = 0f;
            GunRecoil = 0f;
            FireHeld = false;
            JumpBuffer = 0f;
            SlideBuffer = 0f;
            m_flashTime = 0f;
            if (MuzzleFlash != null) MuzzleFlash.SetActive(false);
            if (MuzzleLight != null) MuzzleLight.intensity = 0f;
            if (Body != null)
            {
                Body.localPosition = Vector3.zero;
                Body.localRotation = Quaternion.identity;
            }
            if (Rig != null)
            {
                Rig.localPosition = Vector3.zero;
                Rig.localRotation = Quaternion.identity;
            }
            RestoreMaterials();
        }

        public bool Jump()
        {
            if (!Grounded)
            {
                JumpBuffer = 0.14f;     // buffer the input, fire it on landing
                return false;
            }
            Vy = GameConfig.JumpVelocity;
            Grounded = false;
            SlideT = 0f;                // jumping cancels a slide
            if (SfxSystem.Instance != null) SfxSystem.Instance.Jump();
            return true;
        }

        public bool Slide()
        {
            if (SlideT > 0f || SlideCd > 0f)
            {
                SlideBuffer = 0.16f;
                return false;
            }
            if (!Grounded) Vy = Mathf.Min(Vy, -16f);   // fast-fall into the slide
            SlideT = GameConfig.SlideTime;
            SlideCd = GameConfig.SlideTime + GameConfig.SlideCooldown;
            if (SfxSystem.Instance != null) SfxSystem.Instance.Slide();
            return true;
        }

        public bool MoveLane(int direction)
        {
            int next = Mathf.Clamp(Lane + direction, 0, GameConfig.LaneCount - 1);
            if (next == Lane) return false;
            Lane = next;
            TargetX = GameConfig.LaneX[next];
            if (SfxSystem.Instance != null) SfxSystem.Instance.UiTick();
            return true;
        }

        // =================================================================
        //  Simulation
        // =================================================================
        /// <summary>Advance physics + animation. Called with fixed sub-steps.</summary>
        public void Tick(float dt, float speed)
        {
            m_speed = speed;

            // lateral snap
            X += (TargetX - X) * Mathf.Min(1f, dt * GameConfig.LaneSnap);

            // vertical physics
            if (!Grounded)
            {
                Vy -= GameConfig.Gravity * dt;
                Y += Vy * dt;
                if (Y <= 0f)
                {
                    Y = 0f;
                    Vy = 0f;
                    Grounded = true;
                    if (SfxSystem.Instance != null) SfxSystem.Instance.Land();
                }
            }

            if (SlideT > 0f) SlideT = Mathf.Max(0f, SlideT - dt);
            if (SlideCd > 0f) SlideCd = Mathf.Max(0f, SlideCd - dt);
            if (Invuln > 0f) Invuln -= dt;

            // buffered verbs
            if (JumpBuffer > 0f)
            {
                JumpBuffer -= dt;
                if (Grounded) { JumpBuffer = 0f; Jump(); }
            }
            if (SlideBuffer > 0f)
            {
                SlideBuffer -= dt;
                if (Grounded && SlideT <= 0f && SlideCd <= 0f) { SlideBuffer = 0f; Slide(); }
            }

            Rig.localPosition = new Vector3(X, Y, 0f);
            Animate(dt);
        }

        void Animate(float dt)
        {
            RunPhase += dt * (m_speed * 0.62f + 3f);
            float ph = RunPhase;
            float legs = Mathf.Sin(ph);
            float arms = Mathf.Sin(ph + Mathf.PI);
            float k = Mathf.Min(1f, dt * 14f);

            float tLegL, tLegR, tArmL, tArmR, tBodyY, tBodyX, tBodyZ;
            if (!Grounded)
            {
                // airborne tuck
                tLegL = -0.95f; tLegR = 0.55f;
                tArmL = -1.1f; tArmR = -0.9f;
                tBodyY = 0f; tBodyX = -0.12f; tBodyZ = 0f;
            }
            else if (Sliding)
            {
                // feet-first slide, reclined about the hips
                tLegL = 0.10f; tLegR = 0.42f;
                tArmL = -0.95f; tArmR = -0.75f;
                tBodyY = -0.10f; tBodyX = 1.02f; tBodyZ = 0.18f;
            }
            else
            {
                tLegL = legs * 0.92f; tLegR = -legs * 0.92f;
                tArmL = -arms * 0.75f; tArmR = -0.45f + arms * 0.16f;
                tBodyY = Mathf.Abs(Mathf.Sin(ph)) * 0.075f; tBodyX = 0.10f; tBodyZ = 0f;
            }

            LegL.localRotation = LerpRot(LegL.localRotation, tLegL, k);
            LegR.localRotation = LerpRot(LegR.localRotation, tLegR, k);
            ArmL.localRotation = LerpRot(ArmL.localRotation, tArmL, k);
            ArmR.localRotation = LerpRot(ArmR.localRotation, tArmR, k);

            Body.localPosition = new Vector3(0f,
                Mathf.Lerp(Body.localPosition.y, tBodyY, k),
                Mathf.Lerp(Body.localPosition.z, tBodyZ, k));
            Body.localRotation = Quaternion.Euler(
                Mathf.LerpAngle(Body.localRotation.eulerAngles.x, tBodyX * Mathf.Rad2Deg, k),
                0f,
                -(TargetX - X) * 0.10f * Mathf.Rad2Deg);
            Rig.localRotation = Quaternion.Euler(0f, 0f, -(TargetX - X) * 0.06f * Mathf.Rad2Deg);

            // recoil
            if (GunRecoil > 0f)
            {
                GunRecoil = Mathf.Max(0f, GunRecoil - dt * 7f);
                ArmR.localRotation = Quaternion.Euler(
                    ArmR.localRotation.eulerAngles.x + GunRecoil * 0.32f * Mathf.Rad2Deg,
                    ArmR.localRotation.eulerAngles.y,
                    ArmR.localRotation.eulerAngles.z);
                Gun.localPosition = new Vector3(0.05f, -0.6f, -0.12f + GunRecoil * 0.06f);
            }

            // muzzle flash decay
            if (m_flashTime > 0f)
            {
                m_flashTime -= dt;
                if (m_flashTime <= 0f) MuzzleFlash.SetActive(false);
            }
            if (MuzzleLight.intensity > 0f)
            {
                MuzzleLight.intensity = Mathf.Max(0f, MuzzleLight.intensity - dt * 60f);
            }

            // damage blink
            if (Invuln > 0f)
            {
                bool blink = Mathf.Sin(Time.time * 40f) > 0f;
                TorsoRenderer.sharedMaterial = blink ? m_rust : m_cloth;
                HeadRenderer.sharedMaterial = blink ? m_rust : m_skin;
            }
            else
            {
                RestoreMaterials();
            }

            // follow light
            if (BodyLight != null)
            {
                BodyLight.transform.position = new Vector3(X - 0.6f, 3.0f + Y * 0.5f, 2.2f);
            }
        }

        void RestoreMaterials()
        {
            if (TorsoRenderer != null && TorsoRenderer.sharedMaterial != m_cloth) TorsoRenderer.sharedMaterial = m_cloth;
            if (HeadRenderer != null && HeadRenderer.sharedMaterial != m_skin) HeadRenderer.sharedMaterial = m_skin;
        }

        static Quaternion LerpRot(Quaternion current, float targetDegrees, float k)
        {
            return Quaternion.Euler(
                Mathf.LerpAngle(current.eulerAngles.x, targetDegrees * Mathf.Rad2Deg, k),
                current.eulerAngles.y,
                current.eulerAngles.z);
        }

        // =================================================================
        //  Idle / death presentation
        // =================================================================
        /// <summary>Menu idle: gentle breathing run cycle in place.</summary>
        public void IdleTick(float dt)
        {
            RunPhase += dt * 2.2f;
            float k = Mathf.Min(1f, dt * 6f);
            LegL.localRotation = LerpRot(LegL.localRotation, Mathf.Sin(RunPhase) * 0.16f, k);
            LegR.localRotation = LerpRot(LegR.localRotation, -Mathf.Sin(RunPhase) * 0.16f, k);
            ArmL.localRotation = LerpRot(ArmL.localRotation, -0.12f + Mathf.Sin(RunPhase * 0.7f) * 0.08f, k);
            ArmR.localRotation = LerpRot(ArmR.localRotation, -0.45f, k);
            Body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(RunPhase)) * 0.02f, 0f);
            Rig.localPosition = Vector3.zero;
        }

        /// <summary>Death stumble: tip forward, slide backwards, roll to one side.</summary>
        public void TickDying(float dt)
        {
            float targetX = -1.15f * Mathf.Rad2Deg;
            float x = Mathf.LerpAngle(Body.localRotation.eulerAngles.x, targetX, Mathf.Min(1f, dt * 5f));
            Body.localRotation = Quaternion.Euler(x, 0f, Body.localRotation.eulerAngles.z);

            Vector3 p = Rig.localPosition;
            p.z += dt * 2.2f;
            Rig.localPosition = p;

            Rig.localRotation = Quaternion.Euler(0f, 0f, Rig.localRotation.eulerAngles.z + dt * 0.6f * Mathf.Rad2Deg);
        }

        public void ResetDeathPose()
        {
            Body.localRotation = Quaternion.identity;
            Body.localPosition = Vector3.zero;
            Rig.localRotation = Quaternion.identity;
            Rig.localPosition = new Vector3(X, Y, 0f);
        }

        // =================================================================
        //  Combat hooks
        // =================================================================
        public void OnFired()
        {
            GunRecoil = 1f;
            m_flashTime = 0.07f;
            MuzzleFlash.SetActive(true);
            float s = 0.35f * (1.7f + Random.value * 0.7f);
            MuzzleFlash.transform.localScale = Vector3.one * s;
            MuzzleFlash.transform.localRotation = Quaternion.Euler(0f, 0f, Random.value * 360f);
            MuzzleLight.intensity = 14f;
        }

        public Vector3 MuzzleWorldPosition
        {
            get
            {
                if (Muzzle != null) return Muzzle.position;
                return new Vector3(X + 0.42f, 1.12f + Y, -0.85f);
            }
        }

        /// <summary>Damage flash / stumble feedback.</summary>
        public void OnDamaged()
        {
            Invuln = GameConfig.InvulnerabilityTime;
        }
    }
}
