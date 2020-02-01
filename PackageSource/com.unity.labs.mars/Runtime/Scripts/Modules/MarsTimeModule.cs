using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Manages <see cref="MarsTime"/> properties and callbacks
    /// </summary>
    [ScriptableSettingsPath(MARSCore.SettingsFolder)]
    [ModuleBehaviorCallbackOrder(ModuleOrders.MarsTimeBehaviorOrder)]
    public class MarsTimeModule : ScriptableSettings<MarsTimeModule>, IModuleBehaviorCallbacks
    {
        [SerializeField]
        [Tooltip("Sets the interval in seconds at which MarsUpdate events are performed")]
        float m_TimeStep = 0.016f;

        readonly List<IModuleMarsUpdate> m_MarsUpdateModules = new List<IModuleMarsUpdate>();

        float m_StartTime;
        float m_FixedTimeStep;

        public void LoadModule()
        {
            MarsTime.TimeStep = m_TimeStep;
            m_MarsUpdateModules.Clear();
            foreach (var module in ModuleLoaderCore.instance.modules)
            {
                if (module is IModuleMarsUpdate marsUpdateModule)
                    m_MarsUpdateModules.Add(marsUpdateModule);
            }
        }

        public void UnloadModule()
        {
            MarsTime.Time = 0f;
            MarsTime.FrameCount = 0;
        }

        public void OnBehaviorAwake() { }

        public void OnBehaviorEnable()
        {
            m_StartTime = Time.time;
            MarsTime.Time = 0f;
            MarsTime.FrameCount = 0;
            MarsTime.TimeStep = m_TimeStep;
            m_FixedTimeStep = m_TimeStep; // Cache the time step here in case the serialized value changes during this session
        }

        public void OnBehaviorStart() { }

        public void OnBehaviorUpdate()
        {
            // Mars Time must tick on a fixed time step and also progress at the same pace as Time.time.
            // So in each behavior update we tick Mars Time by the fixed time step until it has caught up as much
            // as it can to the time from the start without surpassing it.
            var timeFromStart = Time.time - m_StartTime;
            var nextTime = MarsTime.Time + m_FixedTimeStep;
            while (nextTime <= timeFromStart)
            {
                MarsTime.Time = nextTime;
                MarsTime.FrameCount++;
                InvokeMarsUpdate();
                nextTime += m_FixedTimeStep;
            }
        }

        public void OnBehaviorDisable() { }

        public void OnBehaviorDestroy() { }

        void InvokeMarsUpdate()
        {
            foreach (var module in m_MarsUpdateModules)
            {
                module.OnMarsUpdate();
            }

            MarsTime.InvokeMarsUpdate();
        }
    }
}
