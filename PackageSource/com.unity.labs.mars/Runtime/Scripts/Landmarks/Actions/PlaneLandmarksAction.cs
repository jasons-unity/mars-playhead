﻿using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using Unity.Labs.MARS.Query;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [MonoBehaviourComponentMenu(typeof(PlaneLandmarksAction), "Action/Plane Landmarks")]
    public class PlaneLandmarksAction : LandmarkOutputPolygon, IUsesMARSTrackableData<MRPlane>, IUsesCameraOffset, ISpawnable
    {
        const string k_CenterDefinitionName = "Provided Center";
        static readonly List<LandmarkDefinition> k_Definitions = new List<LandmarkDefinition>
        {
            new LandmarkDefinition(k_CenterDefinitionName, new []{typeof(LandmarkOutputPoint)})
        };

        readonly List<Vector3> m_LocalVerts = new List<Vector3>();
        readonly List<Vector3> m_BoundingRect = new List<Vector3>();
        Pose m_BoundingRectPose;

        MRPlane? m_MRPlane;

        public override List<LandmarkDefinition> AvailableLandmarkDefinitions
        {
            get
            {
                var list = new List<LandmarkDefinition>(base.AvailableLandmarkDefinitions);
                list.AddRange(k_Definitions);
                return list;
            }
        }

#if !FI_AUTOFILL
        IProvidesCameraOffset IFunctionalitySubscriber<IProvidesCameraOffset>.provider { get; set; }
#endif

        void OnDisable() { m_MRPlane = null; }

        protected  void OnMatchDataChanged(QueryResult queryResult)
        {
            m_MRPlane = queryResult.ResolveValue(this);

            if (m_MRPlane == null)
                return;

            var planeVerts = m_MRPlane.Value.vertices;
            if (planeVerts == null)
                return;

            m_BoundingRect.Clear();
            var centerToCorner = this.GetCameraScale() * 0.5f * m_MRPlane.Value.extents;
            m_BoundingRect.Add(new Vector3(centerToCorner.x, 0f, centerToCorner.y));
            m_BoundingRect.Add(new Vector3(-centerToCorner.x, 0f, centerToCorner.y));
            m_BoundingRect.Add(new Vector3(-centerToCorner.x, 0f, -centerToCorner.y));
            m_BoundingRect.Add(new Vector3(centerToCorner.x, 0f, -centerToCorner.y));
            m_BoundingRectPose = this.ApplyOffsetToPose(new Pose(m_MRPlane.Value.pose.ApplyOffsetTo(m_MRPlane.Value.center), m_MRPlane.Value.pose.rotation));

            m_LocalVerts.Clear();
            foreach (var v in planeVerts)
            {
                m_LocalVerts.Add(this.GetCameraScale() * v);
            }

            SetPolygonLocalSpace(m_LocalVerts, this.ApplyOffsetToPose(m_MRPlane.Value.pose));
        }

        protected  void OnMatchDataLost(QueryResult queryResult)
        {
            m_MRPlane = null;
        }

        public override Action<ILandmarkController> GetLandmarkCalculation(LandmarkDefinition definition)
        {
            if (definition.name.Equals(k_CenterDefinitionName, StringComparison.InvariantCultureIgnoreCase))
                return CalculateCenter;

            if (definition.GetEnumName<BasicPolygonLandmarks>() == BasicPolygonLandmarks.BoundingRect)
                return CalculateBoundingRectFromExtents;

            return base.GetLandmarkCalculation(definition);
        }

        void CalculateCenter(ILandmarkController landmark)
        {
            if (!m_MRPlane.HasValue)
                return;

            var point = landmark.output as LandmarkOutputPoint;
            if (point != null)
            {
                var realWorldCenter = m_MRPlane.Value.pose.ApplyOffsetTo(m_MRPlane.Value.center);
                point.position = this.ApplyOffsetToPosition(realWorldCenter);
            }
        }

        void CalculateBoundingRectFromExtents(ILandmarkController landmark)
        {
            if (!m_MRPlane.HasValue)
                return;

            var polygon = landmark.output as LandmarkOutputPolygon;
            if (polygon != null)
            {
                SetPolygonLocalSpace(m_BoundingRect, m_BoundingRectPose);
            }
        }

        public void OnMatchAcquire(QueryResult queryResult)
        {
            OnMatchDataChanged(queryResult);
            FireDataChangeEvent();

        }

        public void OnMatchUpdate(QueryResult queryResult)
        {
            OnMatchDataChanged(queryResult);
            FireDataChangeEvent();

        }

        public void OnMatchLoss(QueryResult queryResult)
        {
            OnMatchDataLost(queryResult);
            FireDataChangeEvent();
        }
    }
}
