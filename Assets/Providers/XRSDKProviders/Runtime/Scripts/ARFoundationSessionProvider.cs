using UnityEngine;

#if INCLUDE_MARS
using Unity.Labs.Utils;
#endif

#if INCLUDE_AR_FOUNDATION
using Unity.Labs.ModuleLoader;
using UnityEngine.XR.ARFoundation;
using UnityObject = UnityEngine.Object;
#endif

#if UNITY_EDITOR && INCLUDE_MARS
[assembly: OptionalDependency("UnityEngine.XR.ARFoundation.ARSession", "INCLUDE_AR_FOUNDATION")]
#endif

namespace Unity.Labs.MARS.Providers
{
#if INCLUDE_MARS && INCLUDE_AR_FOUNDATION
    public class ARFoundationSessionProvider : MonoBehaviour, IProvidesSessionControl
    {
        static bool s_SessionStarted;
        static ARSession s_TemporarySession;
        public static ARSession currentSession
        {
            get { return s_TemporarySession; }
        }

        public static void RequireARSession()
        {
            if (s_SessionStarted)
                return;

            s_SessionStarted = true;
            if (!UnityObject.FindObjectOfType<ARSession>())
            {
                CreateSessionObject();
                s_TemporarySession.hideFlags = HideFlags.DontSave;
            }
        }

        public static void TearDownARSession()
        {
            s_SessionStarted = false;
            if (s_TemporarySession)
                UnityObjectUtils.Destroy(s_TemporarySession.gameObject);
        }

        public bool SessionExists()
        {
            return Resources.FindObjectsOfTypeAll<ARSession>().Length > 0;
        }

        public bool SessionReady()
        {
            return ARSession.state == ARSessionState.Ready;
        }

        public bool SessionRunning()
        {
            var arSessions = Resources.FindObjectsOfTypeAll<ARSession>();
            if (arSessions.Length > 0)
                return arSessions[0].enabled;

            return false;
        }

        public void CreateSession()
        {
            var arSessions = Resources.FindObjectsOfTypeAll<ARSession>();
            if (arSessions.Length == 0)
                CreateSessionObject();
        }

        static void CreateSessionObject()
        {
            s_TemporarySession = new GameObject("AR Session").AddComponent<ARSession>();
            s_TemporarySession.gameObject.AddComponent<ARInputManager>();
        }

        public void DestroySession()
        {
            var arSessions = Resources.FindObjectsOfTypeAll<ARSession>();
            if (arSessions.Length > 0)
                UnityObjectUtils.Destroy(arSessions[0].gameObject);
        }

        public void ResetSession()
        {
            var fiModule = ModuleLoaderCore.instance.GetModule<FunctionalityInjectionModule>();
            foreach (var island in fiModule.islands)
            {
                foreach (var kvp in island.providers)
                {
                    if (kvp.Value is ITrackableProvider planeProvider)
                        planeProvider.ClearTrackables();
                }
            }

            var arSessions = Resources.FindObjectsOfTypeAll<ARSession>();
            if (arSessions.Length > 0)
                arSessions[0].Reset();

            foreach (var island in fiModule.islands)
            {
                foreach (var kvp in island.providers)
                {
                    if (kvp.Value is ITrackableProvider trackableProvider)
                        trackableProvider.AddExistingTrackables();
                }
            }
        }

        public void PauseSession()
        {
            var arSessions = Resources.FindObjectsOfTypeAll<ARSession>();
            if (arSessions.Length > 0)
                arSessions[0].enabled = false;
        }

        public void ResumeSession()
        {
            var arSessions = Resources.FindObjectsOfTypeAll<ARSession>();
            if (arSessions.Length > 0)
                arSessions[0].enabled = true;
        }

        public void LoadProvider() {}

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var subscriber = obj as IFunctionalitySubscriber<IProvidesSessionControl>;
            if (subscriber != null)
                subscriber.provider = this;
#endif
        }

        public void UnloadProvider() {}
    }
#else
    public class ARFoundationSessionModule : MonoBehaviour
    {
    }
#endif
}
