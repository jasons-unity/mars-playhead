using System.Reflection;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Labs.MARS
{
    public class MARSEditorBackend : IModuleBuildCallbacks
    {
        const string k_PresetMismatch = "Preset Mismatch: Cannot build player because scenes {0} and {1} have mismatching build presets";

        static readonly MethodInfo k_PlayerSettingsGetSerializedObject =
            typeof(PlayerSettings).GetMethod("GetSerializedObject", BindingFlags.NonPublic | BindingFlags.Static);

        public void LoadModule()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            PrefabStage.prefabSaving += OnPrefabSaving;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange != PlayModeStateChange.ExitingEditMode)
                return;

            // TODO: What to do about dirtying the scene when you enter play mode?
            // Update current scene info in case capabilities have changed
            var session = MARSUtils.GetMARSSession(SceneManager.GetActiveScene());
            if (session == null)
                return;

            session.CheckCapabilities();
        }

        public void UnloadModule()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            PrefabStage.prefabSaving -= OnPrefabSaving;
        }

        static void OnSceneSaving(Scene scene, string path)
        {
            // Creating a new scene can trigger this callback with an invalid scene
            if (!scene.IsValid())
                return;

            // Hide the OnDestroyNotifier so it is not saved
            foreach (var root in scene.GetRootGameObjects())
            {
                var notifier = root.GetComponentInChildren<OnDestroyNotifier>();
                if (notifier != null)
                    notifier.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }

            var session = MARSUtils.GetMARSSession(scene);
            if (session)
                session.CheckCapabilities();
        }

        static void OnSceneSaved(Scene scene)
        {
            if (!scene.IsValid())
                return;

            // Hide the OnDestroyNotifier so it is not saved
            foreach (var root in scene.GetRootGameObjects())
            {
                var notifier = root.GetComponentInChildren<OnDestroyNotifier>();
                if (notifier != null)
                    notifier.gameObject.hideFlags = HideFlags.HideInHierarchy;
            }
        }

        static void OnPrefabSaving(GameObject prefab)
        {
            var environmentSettings = prefab.GetComponent<MARSEnvironmentSettings>();
            if (environmentSettings != null)
                environmentSettings.UpdatePrefabInfo();
        }

        public void OnPreprocessBuild()
        {
            string firstSceneName = null;
            BuildPreset firstPreset = null;
            foreach (var buildScene in EditorBuildSettings.scenes)
            {
                if (!buildScene.enabled)
                    continue;

                var activeScene = SceneManager.GetActiveScene();
                var buildScenePath = buildScene.path;
                var buildSceneIsActiveScene = activeScene.path == buildScenePath;
                Scene sceneWithSession;
                if (!buildSceneIsActiveScene)
                {
                    sceneWithSession = EditorSceneManager.OpenScene(buildScenePath, OpenSceneMode.Additive);
                    // TODO: Fix capabilities from currently loaded scene "leaking" into this check--disabled for now
                    //session.CheckCapabilities();
                    //EditorSceneManager.SaveScene(scene);
                }
                else
                {
                    sceneWithSession = activeScene;
                }

                var sceneName = sceneWithSession.name;
                var session = MARSUtils.GetMARSSession(sceneWithSession);
                if (!buildSceneIsActiveScene)
                    EditorSceneManager.CloseScene(sceneWithSession, true);

                if (session)
                {
                    if (session.buildSettings != null)
                    {
                        if (firstPreset == null)
                        {
                            firstPreset = session.buildSettings;
                            firstSceneName = sceneName;
                        }
                        else if (session.buildSettings != firstPreset)
                        {
                            throw new BuildFailedException(string.Format(k_PresetMismatch, firstSceneName, sceneName));
                        }
                    }
                }
            }

            if (firstPreset != null)
            {
                var serializedObject = (SerializedObject)k_PlayerSettingsGetSerializedObject.Invoke(null, null);
                SetARCoreSupported(serializedObject, firstPreset.ARCoreEnabled);
                SetDeviceRotation(firstPreset);
                serializedObject.ApplyModifiedProperties();
            }
        }

        static void SetDeviceRotation(BuildPreset preset)
        {
            // The device rotation lock is needed for the ULS tracker, which isn't used on iOS
#if !UNITY_IOS
            PlayerSettings.defaultInterfaceOrientation = preset.defaultInterfaceOrientation;
            PlayerSettings.allowedAutorotateToPortrait = preset.allowedAutorotateToPortrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = preset.allowedAutorotateToLandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeRight = preset.allowedAutorotateToLandscapeRight;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = preset.allowedAutorotateToPortraitUpsideDown;
#endif
        }

        static void SetARCoreSupported(SerializedObject playerSettings, bool supported)
        {
            var arCoreSupported = playerSettings.FindProperty("AndroidEnableTango");
            arCoreSupported.boolValue = supported;
        }

        public void OnProcessScene(Scene scene) {}

        public void OnPostprocessBuild() {}
    }
}
