using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public class SimEnvironmentSwitcher : MonoBehaviour, IUsesSessionControl
    {
#if !FI_AUTOFILL
        IProvidesSessionControl IFunctionalitySubscriber<IProvidesSessionControl>.provider { get; set; }
#endif

        void Awake()
        {
#if PLATFORM_LUMIN
            gameObject.SetActive(false);
#elif UNITY_EDITOR
            if (!MARSSceneModule.instance.simulateInPlaymode)
                gameObject.SetActive(false);
#else
            gameObject.SetActive(false);
#endif
        }


        public void PreviousEnvironment()
        {
#if UNITY_EDITOR
            if (EditorOnlyDelegates.SwitchToNextEnvironment != null)
            {
                EditorOnlyDelegates.SwitchToNextEnvironment(false);
                this.ResetSession();
            }
#endif
        }

        public void NextEnvironment()
        {
#if UNITY_EDITOR
            if (EditorOnlyDelegates.SwitchToNextEnvironment != null)
            {
                EditorOnlyDelegates.SwitchToNextEnvironment(true);
                this.ResetSession();
            }
#endif
        }
    }
}
