# Zombie Runner : City Ruins — Unity port

The browser prototype (`zombie-runner.html`, Three.js) rebuilt as a **self-contained Unity project**.
Same lanes, same verbs, same feel — 3-lane snap running, jump / slide / tap-fire combat, endless
procedural city, base-camp upgrades, revive tokens and persistent progression.

Everything is generated at runtime: **no prefabs, no textures, no audio files, no external packages.**
The whole game is C# plus the engine's built-in primitives.

---

## 0. "Missing Editor Version" in Unity Hub

That dialog means the project's `ProjectVersion.txt` names an editor you don't have installed. Pick **one** of these:

1. **Use the editor you already have (fastest).** In that window, click the radio button next to
   your installed version — in your case **6000.6.0f1** under *INSTALLS* — then press the button at
   the bottom right (it changes from *Install Version 2022.3.22f1* to **Open**). Unity 6 opens the
   project and upgrades the files in place. This copy of the project is already pinned to
   `6000.6.0f1`, so if you downloaded it after this fix, no dialog appears at all.
2. **Edit the pin by hand.** Open `ProjectSettings/ProjectVersion.txt` and set
   `m_EditorVersion: <your version>` (e.g. `6000.0.23f1`). Save, then open the folder.
3. **Install the old editor** — *not recommended here*: 2022.3.22f1 is flagged **Security Alert** in
   the Hub (Unity has published patched builds, 2022.3.62f3 or newer). If you must stay on 2022.3,
   install the latest 2022.3 patch instead and set `ProjectVersion.txt` to it.

Nothing else needs to change when the project is opened with a newer editor — the code has no
version-specific dependencies beyond two `#if UNITY_20xx` guards for the built-in font name and
`FindFirstObjectByType`.

---

## 0.1 First-open messages in Unity 6 (6000.x) — all handled

These are the messages a 6000.6.0f1 editor produced on the first open, and what was changed so they
do not come back. If you kept an older copy of the project, apply the two manual edits at the bottom.

| Message | Cause | Status in this copy |
|---|---|---|
| *Unity Package Manager Error — `com.unity.modules.vr@1.0.0 cannot be found`* | `Packages/manifest.json` listed the legacy **VR built-in module**, which Unity 6 no longer ships. | **Fixed** — that entry is gone. If any other module ever fails to resolve, delete `Packages/manifest.json` entirely and let Unity regenerate the correct list for your editor. |
| *`MaterialLibrary.cs(91): error CS0619: 'Object.GetInstanceID()' is obsolete: Use GetEntityId instead`* | Unity 6.6 turned `GetInstanceID()` into an **error**. This compile error is what sent the editor into **Safe Mode**. | **Fixed** — the cache key now uses `RuntimeHelpers.GetHashCode(texture)` (works in 2021 → 6.x; `GetEntityId` does not exist in older editors). |
| *warning CS0618: `FindFirstObjectByType` is obsolete, use `FindAnyObjectByType`* | Deprecated in Unity 6.6. | **Fixed** — uses `FindAnyObjectByType` on 2022.2+, `FindObjectOfType` on 2021.x. |
| *warning UAC1001: field `Rig` / `Pool` is skipped by serialization (missing `[Serializable]`)* | New serializer analyzer, harmless but noisy. | **Fixed** — those runtime-only fields are marked `[System.NonSerialized]`. |
| *Error while opening specified scene `Assets/Scenes/SampleScene.unity`* | A fresh project's auto-generated Build Settings point at the template scene that doesn't exist here. | **Fixed** — the editor script now prunes missing scene entries when it registers `ZombieRunner.unity`. |
| *This project uses Input Manager, which is marked for deprecation* | Informational only (Unity 6 nudges you to the Input System package). | **Harmless** — both input backends are supported; leave it as-is or install the Input System package. |
| *Enter Safe Mode?* | Appears only because a script failed to compile (the CS0619 above). | **Fixed** — with no compile errors the project opens normally. |

### If you want to patch the copy you already have (instead of re-extracting)

1. **`Assets/Scripts/Core/MaterialLibrary.cs`** — find the material cache key line and replace
   ```csharp
   texture == null ? "0" : texture.GetInstanceID().ToString());
   ```
   with
   ```csharp
   texture == null ? "0" : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(texture).ToString());
   ```
   (Same file also got a small `IdentityOf()` helper — copying the whole file from this project is
   fine and equivalent.)
2. **`Packages/manifest.json`** — delete the line `"com.unity.modules.vr": "1.0.0",` (or delete the
   whole file; Unity rebuilds it with defaults).
3. Back in Unity: click **Exit Safe Mode** (or restart the editor) → Unity recompiles → the game runs.

Re-extracting the zip over `D:\zombie-runner` (keeping `Assets`, `Packages`, `ProjectSettings`) is the
fastest route; the `Library` folder can stay, Unity just re-imports.

---

## 0.2 Runtime messages on first Play in Unity 6

Second round of messages, from the first successful compile. Both were caused by one thing in
`FxSystem`: a `ParticleSystem` is **born playing** (`playOnAwake = true`), and Unity 6 refuses module
changes on a live system.

| Message | Cause | Status |
|---|---|---|
| *`Setting the duration while system is still playing is not supported…`* | `ParticleSystem.MainModule.duration` was assigned on a system that was already playing. | **Fixed** — the FX object is created inactive, fully configured, then activated and `Play()`ed. |
| *`ArgumentOutOfRangeException: Index was out of range… Parameter name: index`* | Same root cause: accessing/setting particle modules (size-over-lifetime, colour-over-lifetime) while the system is playing throws inside the engine. | **Fixed** by the same change; particle setup is now wrapped in a try/catch so FX can never block the boot. |
| *Scene opens as "Untitled" with just Main Camera / Directional Light* | The editor script only auto-opens the bootstrap scene when your current scene is empty and unsaved; a fresh default scene has those two objects. | **Improved** — an untouched default scene (camera + light only) is now recognised, so Unity opens `Assets/Scenes/ZombieRunner.unity` for you. You can always use **Zombie Runner → Open Bootstrap Scene**. |
| *Two suns / double lighting* | Playing inside the default scene stacks the template's Directional Light on top of the game's sun. | **Improved** — the bootstrap now reuses the brightest existing directional light as its sun instead of adding a second one. |

**What you should do:** stop Play mode, re-extract the zip over `D:\zombie-runner`, let it recompile,
then **Play** again. Expected Console after the fix: silence, apart from the informational
*"This project uses Input Manager…"* notice, which is harmless.

If a problem does occur, the bootstrap now names the failing step — e.g.
`[Zombie Runner] Scene build failed at step 'fx'.` — which points straight at the subsystem to report.

---

## 1. Requirements

| | |
|---|---|
| Unity | **Unity 6** (this copy is pinned to `6000.6.0f1`). The code also compiles on **2021.3 LTS → 2023.x**: if you use another editor, just change the version line in `ProjectSettings/ProjectVersion.txt` (see §0 for the Hub dialog). |
| Render pipeline | **Built-in RP** (recommended, zero setup) or **URP**. Materials fall back automatically (`Standard` → `Universal Render Pipeline/Lit`). HDRP is not targeted. |
| Input | Works with **Legacy Input Manager**, the **new Input System package**, or **Both**. `Assets/Editor/InputHandlerGuard.cs` warns if the setting would leave you with no input at all. |
| Assets | None required. No TMP essentials, no sprites, no fonts to import. uGUI comes from `Packages/manifest.json` (version 2.0.0, the Unity 6 one). |

---

## 2. Importing it (two ways)

### Option A — open the folder as a project (cleanest)
1. **Unity Hub → Add → Add project from disk** → select the `zombie-runner` folder.
2. Open it with Unity 2021.3+ (accept the version-upgrade prompt if shown).
3. Unity generates the missing editor defaults (`Packages/manifest.json`, quality settings, input
   manager, tags) on first open — that is expected for a code-only project.
4. Press **Play**. The game boots itself.

### Option B — drop it into an existing project
1. Copy the **`Assets`** folder into your project (merge with the existing `Assets`).
2. Press **Play** in any scene — even an empty one. `ZombieRunnerBootstrap` has a
   `[RuntimeInitializeOnLoadMethod]` that spawns the game automatically when no instance exists.

> Both paths are supported on purpose: you can ship it as a whole project *or* as a drop-in package.

### What the first open does
`Assets/Editor/ZombieRunnerSetup.cs` runs once per editor session and, if missing, creates
`Assets/Scenes/ZombieRunner.unity` containing a single `Zombie Runner` object, adds it to
**Build Settings → Scenes In Build**, and opens it (only if your current scene is empty and unsaved).
Menu items **Zombie Runner → Recreate Bootstrap Scene / Open Bootstrap Scene** are available any time.

If you only ever want one bootstrap object in your own scene: create an empty GameObject, add the
`ZombieRunnerBootstrap` component, press Play. That's the entire setup.

---

## 3. Controls

| Action | Keyboard | Touch / mouse |
|---|---|---|
| Change lane | `A` / `D`, `←` / `→` | swipe left / right |
| Jump | `W`, `↑`, or the **JUMP** button | swipe up / JUMP |
| Slide | `S`, `↓`, or the **SLIDE** button | swipe down / SLIDE |
| Fire | `Space` (hold = auto-fire), or **FIRE** | tap the road / hold FIRE |
| Pause | `Esc` / `P` | ⏸ button |
| Mute | `M` | SFX button |
| Retry from pause / game over | `R` | Retry button |

The UI is built from code and does **not** use an `EventSystem` or `GraphicRaycaster` — buttons test
the pointer against their own rect, so UI input behaves identically in every input backend.

---

## 4. Folder layout

```
zombie-runner/
├── README.md
├── .gitignore
├── Packages/
│   └── manifest.json                   # uGUI 2.0.0 + built-in modules (Unity 6 set)
├── ProjectSettings/
│   └── ProjectVersion.txt              # editor version pin (6000.6.0f1)
└── Assets/
    ├── Editor/
    │   ├── ZombieRunnerSetup.cs        # first-run scene creation + menu items
    │   └── InputHandlerGuard.cs        # flags "Input System only" with no package
    └── Scripts/
        ├── Core/
        │   ├── GameConfig.cs           # every tunable, sectors, hitboxes, zed stats
        │   ├── GameManager.cs          # state machine + fixed-step simulation
        │   ├── SaveSystem.cs           # PlayerPrefs progression (coins, best, upgrades)
        │   ├── Upgrades.cs             # base-camp catalogue + derived stats
        │   ├── MaterialLibrary.cs      # pipeline-safe materials + procedural textures
        │   ├── MeshKit.cs              # primitive props from built-in meshes
        │   ├── CameraRig.cs            # chase cam, FOV kick, screen shake, menu dolly
        │   └── ZombieRunnerBootstrap.cs# builds the scene, auto-boots in any scene
        ├── Player/
        │   └── PlayerController.cs     # lanes, jump, slide, run cycle, gun rig
        ├── World/
        │   ├── WorldScroller.cs        # 8×30 m chunk ring, buildings, lamps, rubble
        │   ├── EntityPool.cs           # fixed pools per spawn type
        │   ├── EntityFactory.cs        # barrier / wreck / beam / zeds / pickups
        │   ├── Entity.cs               # pooled object behaviour (kinematic)
        │   ├── SpawnDirector.cs        # weighted waves, always leaves one lane open
        │   └── SkyDome.cs              # vertex-coloured gradient sky + horizon glow
        ├── Combat/
        │   ├── WeaponSystem.cs         # aim assist, pierce, ammo economy
        │   └── FxSystem.cs             # spark bursts + tracer bolts
        ├── UI/
        │   ├── UIFactory.cs            # canvas / label / card / button helpers
        │   ├── ZRButton.cs             # pointer-tested button (no EventSystem)
        │   ├── InputBridge.cs          # legacy **or** new Input System
        │   ├── HudView.cs              # HP, ammo, score, combo, verb pad
        │   ├── ScreenViews.cs          # menu, sector select, base camp, report, pause
        │   └── GameUI.cs               # canvas owner + pointer/swipe routing
        └── Audio/
            └── SfxSystem.cs            # every sound synthesised into an AudioClip at boot
```

`Assets/Scenes/ZombieRunner.unity` is created by the editor script on first open (not shipped, so
there are no GUID mismatches when you import the folder anywhere).

---

## 5. What was ported (parity map)

| Browser build | Unity port |
|---|---|
| `THREE.PerspectiveCamera` chase rig, FOV kick, shake | `CameraRig` (same targets: 64°→+0.75°/m/s, `y=3.55`, `z=7.7`, look at `-9.5`) |
| `CFG` object | `GameConfig` (identical numbers: lanes ±2.6 m, 30 m chunks, 11→24 m/s, jump 10.7, gravity 28) |
| 8 recycled road chunks, random buildings/props/lamps | `WorldScroller` + `MeshKit` (same randomisation ranges) |
| `POOL_SIZES` + `take()/release()` | `EntityPool` (barrier 10, wreck 8, beam 8, walker 16, runner 8, brute 6, coin 64, ammo 10, medkit 4) |
| `spawnWave()` weighted hazard mix, "never block all three" | `SpawnDirector` (identical weights, 25 % of blocked lanes hold a walker) |
| `HITBOX` + `overlapsY()` (jump barriers, slide beams, lane-change wrecks) | `GameConfig.HitboxOf` / `OverlapsVertically` |
| `fire()` with lane assist, pierce, brute armour, +2 ammo per kill | `WeaponSystem` |
| HP, invulnerability blink, revive tokens, combo multiplier | `GameManager` |
| `localStorage` save keys | `SaveSystem` via `PlayerPrefs` (`ZR_COINSAVE`, `ZR_BEST`, `ZR_UPGRADES`, …) |
| WebAudio synth (shot, flesh, coin, hurt, impact…) | `SfxSystem` — same envelopes swept into `AudioClip`s |
| CSS HUD / menus | uGUI built at runtime (`HudView`, `ScreenViews`) |
| Canvas-painted asphalt / facade textures | `TextureFactory` (procedural `Texture2D`s, same look) |
| Gradient-sky shader | `SkyDome` (vertex-coloured dome, no custom shader assets) |
| — | **Not ported:** CSS scanlines/watermark, adaptive quality downgrade, localStorage migration |

All gameplay numbers live in `Assets/Scripts/Core/GameConfig.cs`, so a design tweak is one edit.
Sector themes (City Ruins / Subway / Military Base, unlock at 0 / 900 / 2 200 m) are the
`GameConfig.Sectors` array — colours, fog, light, spawn weights and speed multiplier per sector.

---

## 6. Tuning cheat-sheet

| Want to change | Where |
|---|---|
| Speed curve, lanes, jump height, slide duration, ammo | `GameConfig` constants (`SpeedForDistance`, `JumpVelocity`, …) |
| Hazard hitboxes (what clears what) | `GameConfig.HitboxOf` |
| Zombie hp / damage / closing speed / score | `GameConfig.ZedStatsOf` |
| Wave density, spawn distance | `SpawnDirector` + `GameManager.StepSimulation` (`m_nextSpawn`) |
| Upgrade prices / effects | `Upgrades.All` and the derived properties |
| Sector palettes, weights, unlocks | `GameConfig.BuildSectors()` |
| Audio volumes / timbres | `SfxSystem.Build*` methods |

**Debug helpers:** `GameManager` exposes `GrantCoins(int)`; drag the GameManager component into the
inspector to watch `Hp / Ammo / Distance / Combo` live while playing.

---

## 7. How it runs (architecture in 60 seconds)

* `ZombieRunnerBootstrap` assembles camera, lights, sky, world, player, FX, audio, spawner and UI in
  `Awake`, then hands them to `GameManager`.
* `GameManager` owns a single state machine (`Menu → Stage → Info → Playing → Dying → Over`,
  plus `Camp` and `Paused`) and the only `Update()` in the gameplay code.
* The run simulation runs in **fixed 1/50 s sub-steps** inside that update, so collision and speed
  behave identically at 30, 60 or 144 fps — the same trick the browser build used.
* Nothing uses Unity physics: entities are kinematic transforms moved toward `+Z`, and collisions are
  AABB checks (`|z| < 1.5`, lane distance, vertical overlap). No colliders, no rigidbodies, no
  `FixedUpdate` tuning.
* Spawning is pooled: ~134 objects are created once at boot and recycled; no allocations per frame
  beyond a couple of short lists.

---

## 8. Build targets

* **Windows / macOS / Linux**: File → Build Settings → everything already configured; the bootstrap
  scene is added to the build automatically.
* **Android / iOS**: works out of the box (swipe + on-screen verb pad). Portrait or landscape both
  fine; the canvas scales from a 1920×1080 reference.
* **WebGL**: builds, but note the browser build you started from will be lighter — the Unity runtime
  is ~10–20 MB gzipped. Set *Compression Format* to Brotli and *Decompression Fallback* on if your
  host doesn't send `Content-Encoding` headers.

---

## 9. Troubleshooting

| Symptom | Fix |
|---|---|
| Everything is magenta / pink | You are in a pipeline whose lit shader isn't found (HDRP). Switch to Built-in or URP — `MaterialLibrary` already tries `Standard`, URP Lit, HDRP Lit and Diffuse in that order. |
| `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class…` | Project Settings → Player → **Active Input Handling** → `Both` (or `Input System Package` only, which is also supported). |
| No text on screen | The built-in font wasn't found. `UIFactory.UIFont` tries `LegacyRuntime.ttf` (2022.2+) then `Arial.ttf`, then an OS font. On a stripped Linux container, install `fonts-liberation`. |
| Game doesn't start when I press Play | Check that no other component throws during `Awake` — the bootstrap logs through the console. You can always add an empty GameObject with `ZombieRunnerBootstrap` explicitly. |
| Scene missing from a build | `Zombie Runner → Recreate Bootstrap Scene` (adds it to Build Settings automatically). |
| Unity Hub: *Missing Editor Version* | See §0 — pick your installed editor, or edit `ProjectSettings/ProjectVersion.txt`. |
| Package Manager: *com.unity.ugui@2.0.0 is not available* | You opened this on Unity 2021/2022. Either open with Unity 6, or delete `Packages/manifest.json` and add **Unity UI (uGUI)** from *Window → Package Manager* — it installs the 1.0.0 build instead. |
| Package Manager: *com.unity.modules.*** cannot be found* | Delete `Packages/manifest.json` and reopen — Unity regenerates the module list valid for that editor (§0.1). |
| Compile error mentioning `GetInstanceID` / `GetEntityId` | Old copy of the project; see the one-line patch in §0.1. |
| Game runs but keys do nothing | Project Settings → Player → **Active Input Handling** = `Both`, or run `Zombie Runner → Fix Input Handling`. |
| *Setting the duration while system is still playing* / *ArgumentOutOfRangeException* from particles | Old copy — see §0.2 (particle systems are now configured while inactive). |
| `[Zombie Runner] Scene build failed at step '…'` | That log names the subsystem that threw; the exception text follows on the next line. |

---

## 10. Notes on the port

* All C# in this project has been **compile-checked against Unity 2021.3 reference assemblies** in
  three configurations (legacy input, new Input System, both), targeting the `netstandard2.1` API
  level and C# 9. It has not been launched inside a Unity Editor in this environment, so if a
  platform-specific detail (font, shadow bias, URP material) looks off, it's a one-line tweak in the
  files listed above rather than a structural problem.
* Deliberately gameplay-identical: the difficulty curve, hitbox heights, ammo economy and upgrade
  prices are the same numbers as the HTML build.
* If you want the original as a fallback/reference, keep `zombie-runner.html` next to this folder.

Have fun running.
