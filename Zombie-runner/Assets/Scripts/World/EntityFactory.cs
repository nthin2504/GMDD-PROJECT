using UnityEngine;

namespace ZombieRunner
{
    /// <summary>
    /// Builds every entity's visual hierarchy out of primitives — the Unity
    /// equivalent of the THREE.Group factories in the browser prototype.
    /// Objects are created once at boot and recycled by the pool.
    /// </summary>
    public static class EntityFactory
    {
        // ------------------------------------------------------------- palette
        public static readonly Color Concrete = GameConfig.Hex(0x6d6f6c);
        public static readonly Color Rust = GameConfig.Hex(0x7a4326);
        public static readonly Color Dark = GameConfig.Hex(0x1b1f24);
        public static readonly Color Hazard = GameConfig.Hex(0xc25a2a);
        public static readonly Color Lamp = GameConfig.Hex(0xffca8a);
        public static readonly Color LampGlow = GameConfig.Hex(0xff9a45);

        public static Material MatConcrete { get { return MaterialLibrary.Lit(Concrete, 0f, 0.15f); } }
        public static Material MatRust { get { return MaterialLibrary.Lit(Rust, 0.25f, 0.25f); } }
        public static Material MatDark { get { return MaterialLibrary.Lit(Dark, 0.35f, 0.4f); } }
        public static Material MatHazard { get { return MaterialLibrary.Lit(Hazard, 0f, 0.3f); } }
        public static Material MatLamp { get { return MaterialLibrary.Emissive(Lamp, LampGlow, 2.2f); } }

        public static GameObject Create(SpawnType type, Transform parent)
        {
            GameObject root = new GameObject(type + "_Pooled");
            root.transform.SetParent(parent, false);

            Entity entity = root.AddComponent<Entity>();
            entity.Type = type;

            switch (type)
            {
                case SpawnType.Barrier: BuildBarrier(root.transform); break;
                case SpawnType.Wreck: BuildWreck(root.transform); break;
                case SpawnType.Beam: BuildBeam(root.transform); break;
                case SpawnType.Walker: entity.Rig = BuildZed(root.transform, "Walker", GameConfig.Hex(0x93b47a), GameConfig.Hex(0x6a5346), 1f, 0.22f); break;
                case SpawnType.Runner: entity.Rig = BuildZed(root.transform, "Runner", GameConfig.Hex(0xa8d07a), GameConfig.Hex(0x5c4a3a), 0.92f, 0.42f); break;
                case SpawnType.Brute: entity.Rig = BuildZed(root.transform, "Brute", GameConfig.Hex(0xc07a5a), GameConfig.Hex(0x3a2a26), 1.38f, 0.10f); break;
                case SpawnType.Coin: BuildCoin(root.transform); break;
                case SpawnType.Ammo: BuildAmmo(root.transform); break;
                case SpawnType.Medkit: BuildMedkit(root.transform); break;
            }

            entity.CaptureHomeScale();
            root.SetActive(false);
            return root;
        }

        // ------------------------------------------------------------ hazards
        static void BuildBarrier(Transform root)
        {
            MeshKit.Box(root, "Base", new Vector3(3.0f, 0.9f, 0.45f), new Vector3(0f, 0.45f, 0f), MatConcrete);
            MeshKit.Box(root, "Top", new Vector3(2.6f, 0.22f, 0.32f), new Vector3(0f, 1.0f, 0f), MatHazard);
            MeshKit.Box(root, "FootL", new Vector3(0.2f, 0.9f, 1.0f), new Vector3(-1.45f, 0.45f, 0.25f), MatConcrete);
            MeshKit.Box(root, "FootR", new Vector3(0.2f, 0.9f, 1.0f), new Vector3(1.45f, 0.45f, 0.25f), MatConcrete);
        }

        static void BuildWreck(Transform root)
        {
            Material bodyMat = MaterialLibrary.Lit(GameConfig.Hex(0x6a2f26), 0.25f, 0.3f);
            MeshKit.Box(root, "Body", new Vector3(2.4f, 0.85f, 4.4f), new Vector3(0f, 0.75f, 0f), bodyMat);
            MeshKit.Box(root, "Cabin", new Vector3(2.1f, 0.72f, 2.0f), new Vector3(0f, 1.45f, 0.15f), MatDark);
            MeshKit.Box(root, "WheelFL", new Vector3(0.3f, 0.55f, 0.55f), new Vector3(-1.15f, 0.32f, 1.4f), MatDark);
            MeshKit.Box(root, "WheelFR", new Vector3(0.3f, 0.55f, 0.55f), new Vector3(1.15f, 0.32f, 1.4f), MatDark);
            MeshKit.Box(root, "WheelRL", new Vector3(0.3f, 0.55f, 0.55f), new Vector3(-1.15f, 0.32f, -1.4f), MatDark);
            MeshKit.Box(root, "WheelRR", new Vector3(0.3f, 0.55f, 0.55f), new Vector3(1.15f, 0.32f, -1.4f), MatDark);
            MeshKit.Box(root, "HeadlightL", new Vector3(0.45f, 0.16f, 0.1f), new Vector3(-0.75f, 0.8f, -2.22f), MatLamp, false);
            MeshKit.Box(root, "HeadlightR", new Vector3(0.45f, 0.16f, 0.1f), new Vector3(0.75f, 0.8f, -2.22f), MatLamp, false);
        }

        static void BuildBeam(Transform root)
        {
            MeshKit.Box(root, "PostL", new Vector3(0.2f, 3.6f, 0.2f), new Vector3(-1.6f, 1.8f, 0f), MatDark);
            MeshKit.Box(root, "PostR", new Vector3(0.2f, 3.6f, 0.2f), new Vector3(1.6f, 1.8f, 0f), MatDark);
            MeshKit.Box(root, "Beam", new Vector3(3.3f, 0.42f, 0.5f), new Vector3(0f, 1.36f, 0f), MatHazard);
            MeshKit.Box(root, "Stripe", new Vector3(3.34f, 0.1f, 0.54f), new Vector3(0f, 1.6f, 0f), MatLamp, false);
            MeshKit.Box(root, "WarnL", new Vector3(0.18f, 0.18f, 0.18f), new Vector3(-1.2f, 1.62f, 0.3f), MatLamp, false);
            MeshKit.Box(root, "WarnR", new Vector3(0.18f, 0.18f, 0.18f), new Vector3(1.2f, 1.62f, 0.3f), MatLamp, false);
            MeshKit.Box(root, "BraceL", new Vector3(1.0f, 0.14f, 0.14f), new Vector3(-1.1f, 2.9f, 0f), MatDark);
            MeshKit.Box(root, "BraceR", new Vector3(1.0f, 0.14f, 0.14f), new Vector3(1.1f, 2.9f, 0f), MatDark);
        }

        // -------------------------------------------------------------- zeds
        static ZedRig BuildZed(Transform root, string name, Color skin, Color cloth, float scale, float lean)
        {
            GameObject body = new GameObject(name + "_Body");
            body.transform.SetParent(root, false);
            body.transform.localScale = Vector3.one * scale;
            Transform bt = body.transform;

            Material skinMat = MaterialLibrary.Lit(skin, 0f, 0.1f);
            Material clothMat = MaterialLibrary.Lit(cloth, 0f, 0.08f);

            Transform torso = MeshKit.Box(bt, "Torso", new Vector3(0.62f, 0.7f, 0.34f), new Vector3(0f, 1.15f, 0f), clothMat).transform;
            torso.localRotation = Quaternion.Euler(lean * 57.29578f, 0f, 0f); // radians -> degrees, same as THREE
            MeshKit.Box(bt, "Head", new Vector3(0.34f, 0.34f, 0.34f), new Vector3(0.04f, 1.68f, -0.02f), skinMat);

            Transform armL = MeshKit.Pivot(bt, "ArmL", new Vector3(-0.4f, 1.42f, 0f));
            Transform armR = MeshKit.Pivot(bt, "ArmR", new Vector3(0.4f, 1.42f, 0f));
            MeshKit.Box(armL, "ArmLMesh", new Vector3(0.15f, 0.6f, 0.15f), new Vector3(0f, -0.28f, 0f), skinMat);
            MeshKit.Box(armR, "ArmRMesh", new Vector3(0.15f, 0.6f, 0.15f), new Vector3(0f, -0.28f, 0f), skinMat);

            Transform legL = MeshKit.Pivot(bt, "LegL", new Vector3(-0.17f, 0.81f, 0f));
            Transform legR = MeshKit.Pivot(bt, "LegR", new Vector3(0.17f, 0.81f, 0f));
            MeshKit.Box(legL, "LegLMesh", new Vector3(0.18f, 0.72f, 0.2f), new Vector3(0f, -0.36f, 0f), clothMat);
            MeshKit.Box(legR, "LegRMesh", new Vector3(0.18f, 0.72f, 0.2f), new Vector3(0f, -0.36f, 0f), clothMat);

            return new ZedRig { ArmL = armL, ArmR = armR, LegL = legL, LegR = legR, Torso = torso };
        }

        // ----------------------------------------------------------- pickups
        static void BuildCoin(Transform root)
        {
            Material coin = MaterialLibrary.Emissive(GameConfig.Hex(0xffc24a), GameConfig.Hex(0x9a5c00), 0.9f, 0.9f, 0.28f);
            Material rim = MaterialLibrary.Emissive(GameConfig.Hex(0xffe6a8), GameConfig.Hex(0xd28a1a), 1.1f, 0.9f, 0.3f);
            MeshKit.Cylinder(root, "Coin", 0.64f, 0.09f, Vector3.zero, Quaternion.Euler(90f, 0f, 0f), coin);
            MeshKit.Cylinder(root, "Rim", 0.72f, 0.04f, Vector3.zero, Quaternion.Euler(90f, 0f, 0f), rim);
        }

        static void BuildAmmo(Transform root)
        {
            Material ammo = MaterialLibrary.Lit(GameConfig.Hex(0x6f7d3d), 0f, 0.2f);
            Material glow = MaterialLibrary.Emissive(GameConfig.Hex(0xc9e58a), GameConfig.Hex(0x6a8a33), 1.4f);
            MeshKit.Box(root, "Crate", new Vector3(0.8f, 0.46f, 0.6f), new Vector3(0f, 0.9f, 0f), ammo);
            MeshKit.Box(root, "Lid", new Vector3(0.84f, 0.1f, 0.64f), new Vector3(0f, 1.16f, 0f), glow, false);
        }

        static void BuildMedkit(Transform root)
        {
            Material shell = MaterialLibrary.Emissive(GameConfig.Hex(0xd9e7e2), GameConfig.Hex(0x2c5a4a), 0.7f, 0f, 0.6f);
            Material cross = MaterialLibrary.Emissive(GameConfig.Hex(0x54c98a), GameConfig.Hex(0x2f9a63), 1.5f);
            MeshKit.Box(root, "Case", new Vector3(0.7f, 0.5f, 0.5f), new Vector3(0f, 0.95f, 0f), shell);
            MeshKit.Box(root, "CrossA1", new Vector3(0.36f, 0.1f, 0.12f), new Vector3(0f, 0.95f, -0.26f), cross, false);
            MeshKit.Box(root, "CrossA2", new Vector3(0.12f, 0.26f, 0.12f), new Vector3(0f, 0.95f, -0.26f), cross, false);
            MeshKit.Box(root, "CrossB1", new Vector3(0.36f, 0.1f, 0.12f), new Vector3(0f, 0.95f, 0.26f), cross, false);
            MeshKit.Box(root, "CrossB2", new Vector3(0.12f, 0.26f, 0.12f), new Vector3(0f, 0.95f, 0.26f), cross, false);
        }
    }
}
