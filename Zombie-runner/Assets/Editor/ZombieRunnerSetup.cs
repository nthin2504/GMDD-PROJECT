using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZombieRunner.EditorTools
{
    /// <summary>
    /// First-run convenience: creates Assets/Scenes/ZombieRunner.unity with a
    /// bootstrap object, adds it to the build settings and opens it. Works
    /// without this file too — the game auto-boots in whatever scene you play.
    /// </summary>
    [InitializeOnLoad]
    public static class ZombieRunnerSetup
    {
        const string SceneFolder = "Assets/Scenes";
        const string ScenePath = "Assets/Scenes/ZombieRunner.unity";
        const string SessionKey = "ZombieRunner.SetupDone";

        static ZombieRunnerSetup()
        {
            EditorApplication.delayCall += RunOnce;
        }

        static void RunOnce()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                // wait for the first compile to finish, then try again
                EditorApplication.delayCall += RunOnce;
                return;
            }

            SessionState.SetBool(SessionKey, true);
            EnsureScene();
        }

        [MenuItem("Zombie Runner/Recreate Bootstrap Scene")]
        public static void RecreateScene()
        {
            CreateScene(true);
        }

        [MenuItem("Zombie Runner/Open Bootstrap Scene")]
        public static void OpenScene()
        {
            if (!File.Exists(ScenePath))
            {
                CreateScene(true);
                return;
            }
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        static void EnsureScene()
        {
            if (!Directory.Exists(SceneFolder))
            {
                Directory.CreateDirectory(SceneFolder);
                AssetDatabase.Refresh();
            }

            bool created = false;
            if (!File.Exists(ScenePath))
            {
                created = CreateScene(false);
            }

            AddToBuildSettings();

            // Only hijack the editor when the current scene is a clean, untitled one
            // (either empty, or the untouched default template with just a camera + light).
            Scene active = SceneManager.GetActiveScene();
            bool canOpen = active.path == "" && !active.isDirty && IsUntouchedScene(active);
            if (canOpen && (created || File.Exists(ScenePath)))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }

        static bool IsUntouchedScene(Scene scene)
        {
            if (scene.rootCount == 0) return true;
            if (scene.rootCount > 2) return false;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                string n = roots[i].name;
                if (n != "Main Camera" && n != "Directional Light" && n != "Global Volume") return false;
            }
            return true;
        }

        static bool CreateScene(bool openAfterwards)
        {
            if (!Directory.Exists(SceneFolder))
            {
                Directory.CreateDirectory(SceneFolder);
                AssetDatabase.Refresh();
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            GameObject root = new GameObject("Zombie Runner");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<ZombieRunnerBootstrap>();

            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.Refresh();

            if (saved)
            {
                Debug.Log("[Zombie Runner] Created bootstrap scene at " + ScenePath);
            }
            else
            {
                Debug.LogWarning("[Zombie Runner] Could not save " + ScenePath);
            }

            AddToBuildSettings();

            if (openAfterwards) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            return saved;
        }

        /// <summary>
        /// Adds the bootstrap scene and drops entries that point at scenes which no longer
        /// exist (a fresh project ships Build Settings with a template "SampleScene" path,
        /// which otherwise logs "Error while opening specified scene" on every launch).
        /// </summary>
        static void AddToBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            List<EditorBuildSettingsScene> kept = new List<EditorBuildSettingsScene>();
            bool present = false;

            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i] == null) continue;
                if (scenes[i].path == ScenePath) present = true;
                if (!File.Exists(scenes[i].path)) continue;   // prune stale / template entries
                kept.Add(scenes[i]);
            }

            if (!present) kept.Insert(0, new EditorBuildSettingsScene(ScenePath, true));

            EditorBuildSettings.scenes = kept.ToArray();
        }
    }
}
