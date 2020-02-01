using Unity.Labs.Utils;
using UnityEditor;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace Unity.Labs.MARS
{
    class MARSRootSettingsProvider : SettingsProvider
    {
        public const string MenuPath = "MARS";

        protected MARSRootSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }


        [SettingsProvider]
        public static SettingsProvider CreateMARSRootSettingsProvider()
        {
            var provider = new MARSRootSettingsProvider(MenuPath);
            return provider;
        }
    }

    class MARSUserPreferencesSettingsProvider : ScriptableSettingsProvider<MARSUserPreferences>
    {
        const string k_MenuPath = MARSRootSettingsProvider.MenuPath + "/User Preferences";

        MARSUserPreferencesDrawer m_PreferencesDrawer;

        public MARSUserPreferencesSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_PreferencesDrawer = new MARSUserPreferencesDrawer(serializedObject);
        }

        public override void OnGUI(string searchContext)
        {
            m_PreferencesDrawer.InspectorGUI(target, serializedObject);
        }

        [SettingsProvider]
        public static SettingsProvider CreateMARSUserPreferencesSettingsProvider()
        {
            var provider = new MARSUserPreferencesSettingsProvider(k_MenuPath);
            provider.keywords = GetSearchKeywordsFromSerializedObject(serializedObject);
            return provider;
        }
    }

    class MARSDebugSettingsProvider : ScriptableSettingsProvider<MARSDebugSettings>
    {
        const string k_MenuPath = MARSRootSettingsProvider.MenuPath + "/Debug Settings";

        MARSDebugSettingsDrawer m_PreferencesDrawer;

        public MARSDebugSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_PreferencesDrawer = new MARSDebugSettingsDrawer(serializedObject);
        }

        public override void OnGUI(string searchContext)
        {
            m_PreferencesDrawer.InspectorGUI(serializedObject);
        }

        [SettingsProvider]
        public static SettingsProvider CreateMARSDebugSettingsProvider()
        {
            var provider = new MARSDebugSettingsProvider(k_MenuPath);
            provider.keywords = GetSearchKeywordsFromSerializedObject(serializedObject);
            return provider;
        }
    }

    class MARSSceneModulesProvider : ScriptableSettingsProvider<MARSSceneModule>
    {
        const string k_MenuPath = MARSRootSettingsProvider.MenuPath + "/Scene Module";

        MARSSceneModuleDrawer m_SceneModuleDrawer;

        public MARSSceneModulesProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_SceneModuleDrawer = new MARSSceneModuleDrawer(serializedObject);
        }

        public override void OnGUI(string searchContext)
        {
            m_SceneModuleDrawer.OnInspectorGUI(serializedObject);
        }

        [SettingsProvider]
        public static SettingsProvider CreateMARSSceneModulesProvider()
        {
            var provider = new MARSSceneModulesProvider(k_MenuPath);
            provider.keywords = GetSearchKeywordsFromSerializedObject(serializedObject);
            return provider;
        }
    }

    class SimulationSettingsProvider : ScriptableSettingsProvider<SimulationSettings>
    {
        const string k_MenuPath = MARSRootSettingsProvider.MenuPath + "/Simulation Settings";

        SimulationSettingsDrawer m_PreferencesDrawer;

        public SimulationSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_PreferencesDrawer = new SimulationSettingsDrawer(serializedObject);
        }

        public override void OnGUI(string searchContext)
        {
            m_PreferencesDrawer.InspectorGUI(serializedObject);
        }

        [SettingsProvider]
        public static SettingsProvider CreateSimulationSettingsProvider()
        {
            var provider = new SimulationSettingsProvider(k_MenuPath);
            provider.keywords = GetSearchKeywordsFromSerializedObject(serializedObject);
            return provider;
        }
    }

    class SessionRecordingSettingsProvider : ScriptableSettingsProvider<SessionRecordingSettings>
    {
        const string k_MenuPath = MARSRootSettingsProvider.MenuPath + "/Session Recording Settings";

        SessionRecordingSettingsDrawer m_PreferencesDrawer;

        public SessionRecordingSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_PreferencesDrawer = new SessionRecordingSettingsDrawer(serializedObject);
        }

        public override void OnGUI(string searchContext)
        {
            m_PreferencesDrawer.InspectorGUI(serializedObject);
        }

        [SettingsProvider]
        public static SettingsProvider CreateSessionRecordingSettingsProvider()
        {
            var provider = new SessionRecordingSettingsProvider(k_MenuPath);
            provider.keywords = GetSearchKeywordsFromSerializedObject(serializedObject);
            return provider;
        }
    }
}
