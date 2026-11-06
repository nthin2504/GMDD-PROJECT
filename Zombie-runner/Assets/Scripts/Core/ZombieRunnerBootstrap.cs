using UnityEngine;
using UnityEngine.Rendering;

namespace ZombieRunner
{
    /// <summary>
    /// Builds the whole scene at runtime: camera, lights, sky, road, player, FX,
    /// audio, spawner and UI. That means the project has no prefab or scene
    /// dependencies — drop the folder into any Unity project, press Play, and the
    /// game assembles itself.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ZombieRunnerBootstrap : MonoBehaviour
    {
        public static ZombieRunnerBootstrap Instance { get; private set; }

        [Tooltip("Target frame rate. 60 matches the browser build.")]
        public int TargetFrameRate = 60;

        public GameManager Game { get; private set; }

        string m_step = "start";

        /// <summary>
        /// Any scene can be played: if no bootstrap exists after the scene loads,
        /// one is created automatically.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (FindExisting() != null) return;
            GameObject go = new GameObject("[Zombie Runner]");
            go.AddComponent<ZombieRunnerBootstrap>();
        }

        static ZombieRunnerBootstrap FindExisting()
        {
            // FindAnyObjectByType exists from 2022.2; FindFirstObjectByType is deprecated in Unity 6.6,
            // and FindObjectOfType is the only option on 2021.x.
#if UNITY_2022_2_OR_NEWER
            return Object.FindAnyObjectByType<ZombieRunnerBootstrap>();
#else
            return Object.FindObjectOfType<ZombieRunnerBootstrap>();
#endif
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Application.targetFrameRate = TargetFrameRate;
            QualitySettings.shadowDistance = 90f;
            QualitySettings.antiAliasing = 2;

            try
            {
                BuildScene();
            }
            catch (System.Exception e)
            {
                // A half-built scene is much harder to debug than a named failure.
                Debug.LogError("[Zombie Runner] Scene build failed at step '" + m_step + "'. " +
                               "The game could not start.\n" + e);
            }
        }

        void BuildScene()
        {
            m_step = "camera";
            // ------------------------------------------------------- camera
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = GameConfig.Hex(0x1b232b);
            cam.fieldOfView = 64f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 620f;
            cam.allowHDR = true;
            cam.transform.SetParent(null, false);
            cam.transform.position = new Vector3(0f, 3.5f, 7.6f);
            cam.transform.rotation = Quaternion.Euler(12f, 0f, 0f);

            if (FindListener() == null) cam.gameObject.AddComponent<AudioListener>();

            CameraRig camRig = cam.GetComponent<CameraRig>();
            if (camRig == null) camRig = cam.gameObject.AddComponent<CameraRig>();

            m_step = "lights";
            // ------------------------------------------------------- lights
            // Reuse a directional light already in the scene (the default "Directional Light"
            // of a fresh Unity scene) so the world isn't lit twice.
            Light sun = FindDirectionalLight();
            GameObject sunGo;
            if (sun != null)
            {
                sunGo = sun.gameObject;
                sunGo.name = "Sun";
            }
            else
            {
                sunGo = new GameObject("Sun");
                sunGo.transform.SetParent(transform, false);
                sun = sunGo.AddComponent<Light>();
            }
            sun.type = LightType.Directional;
            sun.color = GameConfig.Hex(0xffb277);
            sun.intensity = 2.1f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            sun.shadowBias = 0.045f;
            sun.shadowNormalBias = 0.35f;
            sunGo.transform.rotation = Quaternion.LookRotation(new Vector3(26f, -26f, 42f).normalized);

            GameObject rimGo = new GameObject("Rim Light");
            rimGo.transform.SetParent(transform, false);
            Light rim = rimGo.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = GameConfig.Hex(0x5f9ad0);
            rim.intensity = 0.5f;
            rim.shadows = LightShadows.None;
            rimGo.transform.rotation = Quaternion.LookRotation(new Vector3(-20f, -14f, -22f).normalized);

            RenderSettings.sun = sun;

            camRig.Init(cam, sunGo.transform);

            m_step = "sky";
            // ---------------------------------------------------------- sky
            SkyDome sky = SkyDome.Create(transform, 420f);

            m_step = "world";
            // -------------------------------------------------------- world
            GameObject worldGo = new GameObject("World");
            worldGo.transform.SetParent(transform, false);
            WorldScroller world = worldGo.AddComponent<WorldScroller>();
            world.Build();

            m_step = "player";
            // ------------------------------------------------------- player
            GameObject playerGo = new GameObject("Player");
            playerGo.transform.SetParent(transform, false);
            PlayerController player = playerGo.AddComponent<PlayerController>();

            m_step = "fx";
            // ----------------------------------------------------------- fx
            GameObject fxGo = new GameObject("FX");
            fxGo.transform.SetParent(transform, false);
            FxSystem fx = fxGo.AddComponent<FxSystem>();
            fx.Init(fxGo.transform);

            m_step = "audio";
            // -------------------------------------------------------- audio
            GameObject audioGo = new GameObject("Audio");
            audioGo.transform.SetParent(transform, false);
            SfxSystem sfx = audioGo.AddComponent<SfxSystem>();

            m_step = "game manager";
            // --------------------------------------------------- game manager
            GameManager game = gameObject.AddComponent<GameManager>();

            m_step = "spawning";
            // ------------------------------------------------------ spawning
            GameObject spawnGo = new GameObject("Spawner");
            spawnGo.transform.SetParent(transform, false);
            SpawnDirector spawner = spawnGo.AddComponent<SpawnDirector>();
            spawner.Init(game, spawnGo.transform);

            WeaponSystem weapon = gameObject.AddComponent<WeaponSystem>();
            weapon.Init(game);

            m_step = "ui";
            // ------------------------------------------------------------ ui
            GameUI ui = gameObject.AddComponent<GameUI>();
            ui.Build();

            m_step = "wiring";
            // ------------------------------------------------------- wiring
            game.Cam = cam;
            game.Sun = sun;
            game.Rim = rim;
            game.Sky = sky;
            game.Player = player;
            game.World = world;
            game.Spawner = spawner;
            game.Weapon = weapon;
            game.Fx = fx;
            game.Sfx = sfx;
            game.UI = ui;
            game.CamRig = camRig;

            Game = game;
            m_step = "initialize";
            game.Initialize();
            m_step = "done";
        }

        Light FindDirectionalLight()
        {
#if UNITY_2022_2_OR_NEWER
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
            Light[] lights = Object.FindObjectsOfType<Light>();
#endif
            // Prefer the brightest directional light that isn't one of ours
            // (the game's own lights are children of this object).
            Light best = null;
            for (int i = 0; i < lights.Length; i++)
            {
                Light candidate = lights[i];
                if (candidate == null || candidate.type != LightType.Directional) continue;
                if (candidate.transform.IsChildOf(transform)) continue;
                if (best == null || candidate.intensity > best.intensity) best = candidate;
            }
            return best;
        }

        static AudioListener FindListener()
        {
#if UNITY_2022_2_OR_NEWER
            return Object.FindAnyObjectByType<AudioListener>();
#else
            return Object.FindObjectOfType<AudioListener>();
#endif
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
