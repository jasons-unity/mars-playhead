#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public static class EditorOnlyEvents
    {
        public static event Action onTemporalSimulationStart;
        public static event Action onEnvironmentSetup;

        public static void OnTemporalSimulationStart()
        {
            if (onTemporalSimulationStart != null)
                onTemporalSimulationStart();
        }

        public static void OnEnvironmentSetup()
        {
            if (onEnvironmentSetup != null)
                onEnvironmentSetup();
        }
    }
}
#endif
