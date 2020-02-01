using System;
using Unity.Labs.MARS.Data;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS.Query
{
    [ModuleOrder(ModuleOrders.PipelinesLoadOrder)]
    [ModuleUnloadOrder(ModuleOrders.PipelinesUnloadOrder)]
    [ScriptableSettingsPath(MARSCore.SettingsFolder)]
    public class QueryPipelinesModule : ScriptableSettings<QueryPipelinesModule>, IModuleDependency<MARSDatabase>,
        IModuleMarsUpdate, IUsesCameraOffset
    {
        internal enum AcquireCycleState : byte
        {
            UpdatesOnly,
            RunningSets,
            RunningStandalone
        }

        MARSDatabase m_Database;
        SlowTaskModule m_SlowTaskModule;

        AcquireCycleState m_State = AcquireCycleState.UpdatesOnly;

        internal AcquireCycleState State => m_State;

        internal StandaloneQueryPipeline StandalonePipeline { get; set; }
        internal SetQueryPipeline SetPipeline { get; set; }

        public event Action OnSceneEvaluationComplete;

#if !FI_AUTOFILL
        IProvidesCameraOffset IFunctionalitySubscriber<IProvidesCameraOffset>.provider { get; set; }
#endif

        public void ConnectDependency(MARSDatabase dependency)
        {
            m_Database = dependency;
        }

        public void LoadModule()
        {
            StandalonePipeline = new StandaloneQueryPipeline(m_Database);
            SetPipeline = new SetQueryPipeline(m_Database);

            // the query results must have their poses offset by the camera pose
            StandalonePipeline.ResultFillStage.Transformation.applyOffsetToPose = this.ApplyOffsetToPose;
            SetPipeline.MemberResultFillStage.Transformation.applyOffsetToPose = this.ApplyOffsetToPose;

#if UNITY_EDITOR
            EditorOnlyEvents.onTemporalSimulationStart += StandalonePipeline.ClearData;
#endif
        }

        public void UnloadModule()
        {
            Pose IdentityPoseMethod(Pose p) => p;
            StandalonePipeline.ResultFillStage.Transformation.applyOffsetToPose = IdentityPoseMethod;
            SetPipeline.MemberResultFillStage.Transformation.applyOffsetToPose = IdentityPoseMethod;
            if (StandalonePipeline != null)
            {
#if UNITY_EDITOR
                EditorOnlyEvents.onTemporalSimulationStart -= StandalonePipeline.ClearData;
#endif
                StandalonePipeline.ClearData();
            }

            SetPipeline?.Clear();

            StandalonePipeline = null;
            SetPipeline = null;
        }

        internal void StartCycle()
        {
            // we run the set pipeline first, and after it's done the standalone one gets run
            m_State = AcquireCycleState.RunningSets;
            SetPipeline.StartCycle();
        }

        public void Clear()
        {
            StandalonePipeline.ClearData();
            SetPipeline.Clear();
        }

        public void OnMarsUpdate()
        {
            // Check which updating queries need to be run, if any, and update them.
            MARSQueryBackend.instance.RunSetMatchUpdates(SetPipeline.Data);
            MARSQueryBackend.instance.RunMatchUpdates(StandalonePipeline.Data);

            switch (m_State)
            {
                case AcquireCycleState.UpdatesOnly:
                    return;
                case AcquireCycleState.RunningSets:
                {
                    SetPipeline.OnUpdate();
                    if (!SetPipeline.CurrentlyActive)
                    {
                        m_State = AcquireCycleState.RunningStandalone;
                        m_Database.StopUpdateBuffering();
                        StandalonePipeline.StartCycle();
                    }

                    break;
                }
                case AcquireCycleState.RunningStandalone:
                    StandalonePipeline.OnUpdate();
                    if (!StandalonePipeline.CurrentlyActive)
                    {
                        m_State = AcquireCycleState.UpdatesOnly;
                        m_Database.StopUpdateBuffering();
                        OnSceneEvaluationComplete?.Invoke();
                    }

                    break;
            }
        }
    }
}
