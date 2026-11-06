using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZombieRunner
{
    public enum GameState { Boot, Menu, Stage, Camp, Info, Playing, Dying, Over, Paused }

    /// <summary>
    /// Owns the run state machine and the fixed-step simulation, exactly like the
    /// single update loop in the browser build. Every subsystem is ticked from
    /// here, in order, so behaviour is deterministic and frame-rate independent.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ---- scene references (assigned by ZombieRunnerBootstrap) --------
        public Camera Cam;
        public Light Sun;
        public Light Rim;
        public SkyDome Sky;
        public PlayerController Player;
        public WorldScroller World;
        public SpawnDirector Spawner;
        public WeaponSystem Weapon;
        public FxSystem Fx;
        public SfxSystem Sfx;
        public GameUI UI;
        public CameraRig CamRig;

        // ---- run state ---------------------------------------------------
        public GameState State = GameState.Boot;
        public int Hp = GameConfig.HpMax;
        public int Ammo = GameConfig.AmmoStart;
        public int Coins;
        public int Kills;
        public float Distance;
        public float Score;
        public float Speed = GameConfig.StartSpeed;
        public int Combo;
        public int BestCombo;
        public float ComboT;
        public float Hitstop;
        public float RunTime;
        public bool UsedRevive;
        public int CommittedCoins;
        public int CommittedDistance;
        public int SelectedSector;
        public bool DeathByRoadblock;
        public bool Muted;

        float m_spawnGap;
        float m_nextSpawn = GameConfig.FirstSpawnGap;
        float m_dyingTimer;
        float m_showOverAt;
        float m_infoTimer;

        public SectorDef Sector
        {
            get { return GameConfig.Sectors[Mathf.Clamp(SelectedSector, 0, GameConfig.Sectors.Length - 1)]; }
        }

        /// <summary>1.0 -> 5.5x score multiplier, +0.5x every 3 kills.</summary>
        public float ComboMultiplier
        {
            get { return 1f + Mathf.Min(9, Mathf.Floor(Combo / 3f)) * 0.5f; }
        }

        void Awake()
        {
            Instance = this;
        }

        // =================================================================
        //  Boot
        // =================================================================
        public void Initialize()
        {
            SaveSystem.Load();
            WireUI();
            ApplySector(SelectedSector);
            GoMenu();
        }

        void WireUI()
        {
            HudView hud = UI.Hud;
            ScreenViews screens = UI.Screens;

            hud.PauseButton.OnClick = TogglePause;
            hud.MuteButton.OnClick = ToggleMute;
            hud.JumpButton.OnClick = OnJumpPressed;
            hud.SlideButton.OnClick = OnSlidePressed;
            hud.FireButton.OnClick = OnFirePressed;

            screens.StartButton.OnClick = GoStage;
            screens.MenuCampButton.OnClick = GoCamp;
            screens.DeployButton.OnClick = GoInfo;
            screens.StageBackButton.OnClick = GoMenu;
            screens.CampBackButton.OnClick = GoMenu;
            screens.CampDeployButton.OnClick = GoStage;
            screens.TokenButton.OnClick = OnBuyToken;
            screens.RetryButton.OnClick = StartRun;
            screens.OverCampButton.OnClick = GoMenu;
            screens.ReviveButton.OnClick = Revive;
            screens.ResumeButton.OnClick = TogglePause;
            screens.QuitButton.OnClick = GoMenu;
            screens.SkipInfoButton.OnClick = StartRun;

            screens.OnSectorClicked = OnSectorClicked;
            screens.OnUpgradeClicked = OnBuyUpgrade;
            screens.OnTokenClicked = OnBuyToken;
        }

        // =================================================================
        //  Frame loop
        // =================================================================
        void Update()
        {
            float dt = Mathf.Min(UnityEngine.Time.deltaTime, 0.05f);
            RunTime += dt;

            HandleGlobalKeys();

            switch (State)
            {
                case GameState.Playing:
                    Weapon.Tick(dt);
                    if (Hitstop > 0f)
                    {
                        Hitstop -= dt;      // brief impact freeze for punch
                    }
                    else
                    {
                        // fixed sub-steps keep speed and collisions frame-rate independent
                        float remain = dt;
                        int guard = 0;
                        while (remain > 0.0001f && guard++ < 40)
                        {
                            float step = Mathf.Min(remain, 1f / 50f);
                            StepSimulation(step);
                            remain -= step;
                        }
                    }
                    CamRig.TickPlaying(Player, dt, Speed);
                    Fx.Tick(dt);
                    HudRefresh();
                    break;

                case GameState.Dying:
                    m_dyingTimer += dt;
                    Player.TickDying(dt);
                    CamRig.TickDying(Player, dt);
                    Fx.Tick(dt);
                    HudRefresh();
                    if (m_dyingTimer >= m_showOverAt) ShowGameOver();
                    break;

                case GameState.Info:
                    TickIdleWorld(dt);
                    m_infoTimer += dt;
                    int remaining = 3 - Mathf.FloorToInt(m_infoTimer / 0.9f);
                    UI.Screens.SetInfoCountdown(Mathf.Max(0, remaining));
                    if (m_infoTimer >= 2.7f) StartRun();
                    break;

                case GameState.Menu:
                case GameState.Stage:
                case GameState.Camp:
                    TickIdleWorld(dt);
                    break;

                case GameState.Over:
                    // keep the death camera pose behind the report, just breathe
                    World.Scroll(2f * dt);
                    Player.IdleTick(dt);
                    Fx.Tick(dt);
                    HudRefresh();
                    break;

                case GameState.Paused:
                    Fx.Tick(0f);
                    break;
            }

            if (Sky != null && Cam != null) Sky.FollowCamera(Cam.transform);
        }

        void TickIdleWorld(float dt)
        {
            World.Scroll(6f * dt);
            Player.IdleTick(dt);
            CamRig.TickIdle(dt);
            Fx.Tick(dt);
        }

        void HandleGlobalKeys()
        {
            if (InputBridge.GetKeyDown(KeyCode.Escape) || InputBridge.GetKeyDown(KeyCode.P))
            {
                if (State == GameState.Playing || State == GameState.Paused) TogglePause();
            }

            if (InputBridge.GetKeyDown(KeyCode.M)) ToggleMute();

            switch (State)
            {
                case GameState.Playing:
                    if (InputBridge.GetKeyDown(KeyCode.A) || InputBridge.GetKeyDown(KeyCode.LeftArrow)) Player.MoveLane(-1);
                    if (InputBridge.GetKeyDown(KeyCode.D) || InputBridge.GetKeyDown(KeyCode.RightArrow)) Player.MoveLane(1);
                    if (InputBridge.GetKeyDown(KeyCode.W) || InputBridge.GetKeyDown(KeyCode.UpArrow)) Player.Jump();
                    if (InputBridge.GetKeyDown(KeyCode.S) || InputBridge.GetKeyDown(KeyCode.DownArrow)) Player.Slide();
                    Player.FireHeld = InputBridge.GetKey(KeyCode.Space);
                    if (InputBridge.GetKeyDown(KeyCode.Space)) Weapon.TryFire();
                    break;

                case GameState.Menu:
                    if (InputBridge.GetKeyDown(KeyCode.Space) || InputBridge.GetKeyDown(KeyCode.W) ||
                        InputBridge.GetKeyDown(KeyCode.Return))
                    {
                        GoStage();
                    }
                    break;

                case GameState.Over:
                case GameState.Paused:
                    if (InputBridge.GetKeyDown(KeyCode.R)) StartRun();
                    break;
            }
        }

        // =================================================================
        //  Simulation
        // =================================================================
        void StepSimulation(float dt)
        {
            Speed = GameConfig.SpeedForDistance(Distance) * Sector.SpeedMultiplier;
            Distance += Speed * dt;
            Score += Speed * dt;                 // 1 point per metre

            if (ComboT > 0f)
            {
                ComboT -= dt;
                if (ComboT <= 0f) Combo = 0;
            }

            Player.Tick(dt, Speed);
            if (Player.FireHeld || (UI != null && UI.Hud.FireHeld)) Weapon.TryFire();

            float dz = Speed * dt;
            World.Scroll(dz);

            m_spawnGap += dz;
            if (m_spawnGap >= m_nextSpawn)
            {
                m_spawnGap = 0f;
                m_nextSpawn = 20f + Random.value * 12f - Mathf.Min(6f, Distance / 400f);
                Spawner.SpawnWave();
            }

            Spawner.TickEntities(dz, dt, Player.X, RunTime);
            ResolveCollisions();
        }

        void ResolveCollisions()
        {
            List<Entity> active = Spawner.Pool.Active;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Entity e = active[i];
                if (e == null || e.Dead) continue;

                if (Mathf.Abs(e.Z) >= GameConfig.CollisionHalfDepth) continue;

                float reach = e.Type == SpawnType.Coin ? Upgrades.CoinMagnet : GameConfig.PickupReach;
                if (Mathf.Abs(e.X - Player.X) >= reach) continue;

                // pickups ignore height; hazards need a vertical overlap too
                if (!GameConfig.IsPickup(e.Type))
                {
                    if (!GameConfig.OverlapsVertically(e.Type, Player.Y, Player.BodyHeight)) continue;
                }

                switch (e.Type)
                {
                    case SpawnType.Coin:
                        e.Dead = true;
                        e.Life = 0f;
                        Coins++;
                        Sfx.Coin();
                        Fx.Sparks(new Vector3(e.X, 1.0f, e.Z), 6, new Color(1f, 0.85f, 0.47f), 3f, 3f, Speed);
                        break;

                    case SpawnType.Ammo:
                        Ammo = Mathf.Min(Upgrades.MaxAmmo, Ammo + GameConfig.AmmoPickup);
                        Sfx.Pickup();
                        Fx.Sparks(new Vector3(e.X, 1.1f, e.Z), 8, new Color(0.72f, 0.88f, 0.42f), 3f, 3f, Speed);
                        Spawner.Pool.Release(e);
                        break;

                    case SpawnType.Medkit:
                        Hp = Mathf.Min(Upgrades.MaxHp, Hp + GameConfig.MedkitHeal);
                        Sfx.Pickup();
                        UI.FlashHeal();
                        Fx.Sparks(new Vector3(e.X, 1.1f, e.Z), 8, new Color(0.5f, 0.88f, 0.69f), 3f, 3f, Speed);
                        Spawner.Pool.Release(e);
                        break;

                    default:
                        if (GameConfig.IsZed(e.Type))
                        {
                            HitZombie(e);
                        }
                        else if (Player.Invuln <= 0f)
                        {
                            // lethal roadblock / wreck / overhead beam
                            float y = e.Type == SpawnType.Beam ? 1.5f : 1.0f;
                            Fx.Sparks(new Vector3(e.X, y, e.Z), 24, new Color(1f, 0.54f, 0.29f), 7f, 5f, Speed);
                            Die(true);
                        }
                        break;
                }

                if (State != GameState.Playing) return;
            }
        }

        void HitZombie(Entity e)
        {
            if (Player.Invuln > 0f) return;

            Hp = Mathf.Max(0, Hp - GameConfig.ZedStatsOf(e.Type).Damage);
            Combo = 0;
            ComboT = 0f;
            Player.OnDamaged();
            CamRig.AddShake(0.5f);
            Sfx.Hurt();
            UI.FlashDamage();
            Fx.Sparks(new Vector3(e.X, 1.2f, e.Z), 12, new Color(0.54f, 0.77f, 0.42f), 4f, 3f, Speed);
            Spawner.Pool.Release(e);

            if (Hp <= 0) Die(false);
        }

        // =================================================================
        //  Run lifecycle
        // =================================================================
        public void ResetRun()
        {
            Spawner.ReleaseAll();
            Hp = Upgrades.MaxHp;
            Ammo = Upgrades.StartAmmo;
            Score = 0f;
            Coins = 0;
            Kills = 0;
            Distance = 0f;
            Speed = GameConfig.StartSpeed;
            Combo = 0;
            ComboT = 0f;
            BestCombo = 0;
            Hitstop = 0f;
            UsedRevive = false;
            CommittedCoins = 0;
            CommittedDistance = 0;
            DeathByRoadblock = false;
            m_spawnGap = 0f;
            m_nextSpawn = GameConfig.FirstSpawnGap;

            Player.ResetRun();
            World.ResetWorld();
            CamRig.SnapToPlayer(Player);
            Fx.Clear();
            Spawner.PreloadWave();
            HudRefresh();
        }

        public void StartRun()
        {
            if (!GameConfig.SectorUnlocked(Sector)) SelectedSector = 0;
            ApplySector(SelectedSector);
            ResetRun();
            UI.ShowScreen(ScreenId.None);
            UI.ShowHud(true);
            State = GameState.Playing;
            Sfx.UiTick();
        }

        public void Die(bool roadblock)
        {
            if (State != GameState.Playing) return;
            State = GameState.Dying;
            DeathByRoadblock = roadblock;
            m_dyingTimer = 0f;
            m_showOverAt = roadblock ? 0.9f : 0.7f;
            CamRig.AddShake(roadblock ? 1f : 0.6f);
            Sfx.Impact();
            UI.FlashDamage();
        }

        void ShowGameOver()
        {
            if (State != GameState.Dying) return;
            State = GameState.Over;

            // bank only what has not been banked yet this run (revives keep the run alive)
            int deltaCoins = Coins - CommittedCoins;
            int deltaDistance = Mathf.RoundToInt(Distance - CommittedDistance);
            SaveSystem.Commit(deltaCoins, deltaDistance, Mathf.RoundToInt(Score));
            CommittedCoins = Coins;
            CommittedDistance = Mathf.RoundToInt(Distance);

            bool canRevive = SaveSystem.Tokens > 0 && !UsedRevive;
            UI.Screens.RefreshOver(this, canRevive);
            UI.ShowScreen(ScreenId.Over);
            UI.ShowHud(false);
        }

        /// <summary>Second wind: spend a token, clear the road, keep the run going.</summary>
        public void Revive()
        {
            if (State != GameState.Over || SaveSystem.Tokens <= 0 || UsedRevive) return;

            SaveSystem.ConsumeToken();
            UsedRevive = true;
            Hp = Mathf.RoundToInt(Upgrades.MaxHp * 0.6f);
            Ammo = Mathf.Max(Ammo, Mathf.RoundToInt(Upgrades.MaxAmmo * 0.5f));
            Combo = 0;
            ComboT = 0f;

            Spawner.ClearNearPlayer(-34f, Fx);

            UI.ShowScreen(ScreenId.None);
            UI.ShowHud(true);
            State = GameState.Playing;
            Player.Invuln = GameConfig.ReviveInvulnerability;
            Player.ResetDeathPose();
            Player.FireHeld = false;
            Sfx.Revive();
            UI.FlashHeal();
            CamRig.SnapToPlayer(Player);
            HudRefresh();
        }

        public void TogglePause()
        {
            if (State == GameState.Playing)
            {
                State = GameState.Paused;
                UI.ShowScreen(ScreenId.Pause);
                Player.FireHeld = false;
            }
            else if (State == GameState.Paused)
            {
                State = GameState.Playing;
                UI.ShowScreen(ScreenId.None);
            }
        }

        // =================================================================
        //  Menus
        // =================================================================
        public void GoMenu()
        {
            State = GameState.Menu;
            Spawner.ReleaseAll();
            Player.ResetRun();
            World.ResetWorld();
            CamRig.TickIdle(0.016f);
            UI.ShowScreen(ScreenId.Menu);
            UI.ShowHud(false);
            UI.Screens.RefreshMenu();
        }

        public void GoStage()
        {
            State = GameState.Stage;
            UI.ShowScreen(ScreenId.Stage);
            UI.ShowHud(false);
            UI.Screens.RefreshStage(SelectedSector);
            Sfx.UiTick();
        }

        public void GoCamp()
        {
            State = GameState.Camp;
            UI.ShowScreen(ScreenId.Camp);
            UI.ShowHud(false);
            UI.Screens.RefreshCamp();
            Sfx.UiTick();
        }

        public void GoInfo()
        {
            State = GameState.Info;
            m_infoTimer = 0f;
            UI.Screens.SetInfoCountdown(3);
            UI.ShowScreen(ScreenId.Info);
            UI.ShowHud(false);
            Sfx.UiTick();
        }

        void OnSectorClicked(int index)
        {
            if (!GameConfig.SectorUnlocked(GameConfig.Sectors[index])) return;
            SelectedSector = index;
            UI.Screens.RefreshStage(SelectedSector);
            Sfx.UiTick();
        }

        void OnBuyUpgrade(int index)
        {
            if (SaveSystem.Buy(index))
            {
                Sfx.Pickup();
                UI.Screens.RefreshCamp();
                UI.Screens.RefreshMenu();
            }
            else
            {
                Sfx.Dry();
            }
        }

        void OnBuyToken()
        {
            if (SaveSystem.BuyToken())
            {
                Sfx.Pickup();
                UI.Screens.RefreshCamp();
                UI.Screens.RefreshMenu();
            }
            else
            {
                Sfx.Dry();
            }
        }

        void OnJumpPressed()
        {
            if (State == GameState.Playing) Player.Jump();
        }

        void OnSlidePressed()
        {
            if (State == GameState.Playing) Player.Slide();
        }

        void OnFirePressed()
        {
            if (State == GameState.Playing) Weapon.TryFire();
        }

        public void ToggleMute()
        {
            Muted = !Muted;
            if (Sfx != null) Sfx.SetMuted(Muted);
            if (UI != null && UI.Hud != null && UI.Hud.MuteButton != null)
            {
                UI.Hud.MuteButton.SetText(Muted ? "MUTED" : "SFX");
            }
        }

        // =================================================================
        //  Presentation
        // =================================================================
        public void ApplySector(int index)
        {
            SelectedSector = Mathf.Clamp(index, 0, GameConfig.Sectors.Length - 1);
            SectorDef sector = GameConfig.Sectors[SelectedSector];

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = sector.Fog;
            RenderSettings.fogDensity = 0.0115f;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sector.HemiSky;
            RenderSettings.ambientEquatorColor = Color.Lerp(sector.HemiSky, sector.HemiGround, 0.5f);
            RenderSettings.ambientGroundColor = sector.HemiGround;

            if (Sun != null)
            {
                Sun.color = sector.Sun;
                Sun.intensity = sector.SunIntensity;
            }
            if (Rim != null)
            {
                Rim.color = GameConfig.Hex(0x5f9ad0);
                Rim.intensity = 0.5f;
            }
            if (Sky != null) Sky.ApplyColors(sector.SkyTop, sector.SkyBottom, sector.Glow);
            if (Cam != null) Cam.backgroundColor = sector.Fog;
            if (World != null) World.ApplySector(sector);
        }

        public void AddKillScore(SpawnType type)
        {
            Combo++;
            ComboT = GameConfig.ComboWindow;
            BestCombo = Mathf.Max(BestCombo, Combo);
            Score += Mathf.Round(GameConfig.ZedStatsOf(type).Score * ComboMultiplier);
        }

        public void HudRefresh()
        {
            if (UI != null && UI.Hud != null) UI.Hud.Refresh(this, false);
        }

        /// <summary>Debug helper (also handy in the Unity inspector).</summary>
        public void GrantCoins(int amount)
        {
            SaveSystem.Commit(amount, 0, 0);
            if (UI != null && UI.Screens != null)
            {
                UI.Screens.RefreshMenu();
                UI.Screens.RefreshCamp();
            }
        }
    }
}
