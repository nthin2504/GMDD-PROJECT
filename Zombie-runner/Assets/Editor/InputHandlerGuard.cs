using UnityEditor;
using UnityEngine;

namespace ZombieRunner.EditorTools
{
    /// <summary>
    /// Protects against the one project setting that can silently kill input:
    /// "Active Input Handling" set to <c>Input System Package (New)</c> while the
    /// Input System package is not installed. In that state neither input backend
    /// compiles in, so the game runs but ignores every key.
    /// </summary>
    [InitializeOnLoad]
    public static class InputHandlerGuard
    {
        const string MenuPath = "Zombie Runner/Fix Input Handling";

        // ProjectSettings > Player > Active Input Handling
        const int HandlerLegacy = 0;
        const int HandlerNew = 1;
        const int HandlerBoth = 2;

        static InputHandlerGuard()
        {
            EditorApplication.delayCall += Check;
        }

        static void Check()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            int handler = ReadHandler();
            if (handler != HandlerNew) return;              // legacy or both: fine
            if (InputSystemPackagePresent()) return;        // new input only: fine

            Debug.LogWarning(
                "[Zombie Runner] Active Input Handling is 'Input System Package (New)' but the package " +
                "is not installed, so the game would receive no input. Run " +
                "'Zombie Runner > Fix Input Handling', or set Project Settings > Player > " +
                "Active Input Handling to 'Both' / 'Input Manager (Old)' and restart the editor.");
        }

        [MenuItem(MenuPath)]
        public static void FixInputHandling()
        {
            if (!WriteHandler(HandlerLegacy))
            {
                Debug.LogWarning("[Zombie Runner] Could not change the setting automatically. " +
                                 "Set Project Settings > Player > Active Input Handling manually.");
                return;
            }
            Debug.Log("[Zombie Runner] Active Input Handling was reset to 'Input Manager (Old)'. " +
                      "Unity may ask you to restart the editor for it to take effect.");
        }

        static bool InputSystemPackagePresent()
        {
            return System.Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem") != null;
        }

        static SerializedObject LoadPlayerSettings()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return null;
            return new SerializedObject(assets[0]);
        }

        static int ReadHandler()
        {
            SerializedObject settings = LoadPlayerSettings();
            if (settings == null) return HandlerLegacy;
            SerializedProperty property = settings.FindProperty("activeInputHandler");
            return property != null ? property.intValue : HandlerLegacy;
        }

        static bool WriteHandler(int value)
        {
            SerializedObject settings = LoadPlayerSettings();
            if (settings == null) return false;
            SerializedProperty property = settings.FindProperty("activeInputHandler");
            if (property == null) return false;
            property.intValue = value;
            settings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            return true;
        }
    }
}
