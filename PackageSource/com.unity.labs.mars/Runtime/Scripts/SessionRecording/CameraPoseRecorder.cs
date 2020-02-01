using System;
using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using UnityEngine;
using UnityEngine.Timeline;
using UnityObject = UnityEngine.Object;

namespace Unity.Labs.MARS
{
    public class CameraPoseRecorder : DataRecorder, IUsesCameraPose, IUsesSlowTasks
    {
        struct PoseEvent
        {
            public float time;
            public Pose pose;
        }

        List<PoseEvent> m_PoseEvents = new List<PoseEvent>();

        Action m_RecordCameraTask;
        float m_PoseRecordInterval;
        Pose m_LastPose;
        bool m_LastPoseUnchanged;

#if !FI_AUTOFILL
        IProvidesCameraPose IFunctionalitySubscriber<IProvidesCameraPose>.provider { get; set; }
        IProvidesSlowTasks IFunctionalitySubscriber<IProvidesSlowTasks>.provider { get; set; }
#endif

        public CameraPoseRecorder()
        {
            m_RecordCameraTask = RecordCurrentCameraPose;
            m_PoseRecordInterval = SessionRecordingSettings.instance.CameraPoseInterval;
        }

        public override DataRecording CreateDataRecording(TimelineAsset timeline, List<UnityObject> newAssets)
        {
            var eventsCount = m_PoseEvents.Count;
            if (eventsCount < 2)
                throw new InvalidOperationException("There must be at least 2 pose events to create a camera pose recording");

            var xPosCurve = new AnimationCurve();
            var yPosCurve = new AnimationCurve();
            var zPosCurve = new AnimationCurve();
            var xRotCurve = new AnimationCurve();
            var yRotCurve = new AnimationCurve();
            var zRotCurve = new AnimationCurve();
            var wRotCurve = new AnimationCurve();

            // When creating the animation curves we assign tangents such that the curve linearly interpolates between keyframes

            var firstPoseEvent = m_PoseEvents[0];
            var firstTime = firstPoseEvent.time;
            var firstPosition = firstPoseEvent.pose.position;
            var firstRotation = firstPoseEvent.pose.rotation;
            var secondPoseEvent = m_PoseEvents[1];
            var secondTime = secondPoseEvent.time;
            var secondPosition = secondPoseEvent.pose.position;
            var secondRotation = secondPoseEvent.pose.rotation;
            var firstDeltaTime = secondTime - firstTime;
            xPosCurve.AddKey(new Keyframe(
                firstTime, firstPosition.x, 0f, (secondPosition.x - firstPosition.x) / firstDeltaTime));
            yPosCurve.AddKey(new Keyframe(
                firstTime, firstPosition.y, 0f, (secondPosition.y - firstPosition.y) / firstDeltaTime));
            zPosCurve.AddKey(new Keyframe(
                firstTime, firstPosition.z, 0f, (secondPosition.z - firstPosition.z) / firstDeltaTime));
            xRotCurve.AddKey(new Keyframe(
                firstTime, firstRotation.x, 0f, (secondRotation.x - firstRotation.x) / firstDeltaTime));
            yRotCurve.AddKey(new Keyframe(
                firstTime, firstRotation.y, 0f, (secondRotation.y - firstRotation.y) / firstDeltaTime));
            zRotCurve.AddKey(new Keyframe(
                firstTime, firstRotation.z, 0f, (secondRotation.z - firstRotation.z) / firstDeltaTime));
            wRotCurve.AddKey(new Keyframe(
                firstTime, firstRotation.w, 0f, (secondRotation.w - firstRotation.w) / firstDeltaTime));

            for (var i = 1; i < eventsCount - 1; i++)
            {
                var poseEvent = m_PoseEvents[i];
                var time = poseEvent.time;
                var position = poseEvent.pose.position;
                var rotation = poseEvent.pose.rotation;
                var nextPoseEvent = m_PoseEvents[i + 1];
                var nextTime = nextPoseEvent.time;
                var nextPosition = nextPoseEvent.pose.position;
                var nextRotation = nextPoseEvent.pose.rotation;
                var deltaTime = nextTime - firstTime;
                var prevIndex = i - 1;
                xPosCurve.AddKey(new Keyframe(
                    time, position.x, xPosCurve.keys[prevIndex].outTangent, (nextPosition.x - position.x) / deltaTime));
                yPosCurve.AddKey(new Keyframe(
                    time, position.y, yPosCurve.keys[prevIndex].outTangent, (nextPosition.y - position.y) / deltaTime));
                zPosCurve.AddKey(new Keyframe(
                    time, position.z, zPosCurve.keys[prevIndex].outTangent, (nextPosition.z - position.z) / deltaTime));
                xRotCurve.AddKey(new Keyframe(
                    time, rotation.x, xRotCurve.keys[prevIndex].outTangent, (nextRotation.x - rotation.x) / deltaTime));
                yRotCurve.AddKey(new Keyframe(
                    time, rotation.y, yRotCurve.keys[prevIndex].outTangent, (nextRotation.y - rotation.y) / deltaTime));
                zRotCurve.AddKey(new Keyframe(
                    time, rotation.z, zRotCurve.keys[prevIndex].outTangent, (nextRotation.z - rotation.z) / deltaTime));
                wRotCurve.AddKey(new Keyframe(
                    time, rotation.w, wRotCurve.keys[prevIndex].outTangent, (nextRotation.w - rotation.w) / deltaTime));
            }

            var lastPoseEvent = m_PoseEvents[eventsCount - 1];
            var lastTime = lastPoseEvent.time;
            var lastPosition = lastPoseEvent.pose.position;
            var lastRotation = lastPoseEvent.pose.rotation;
            var lastPrevIndex = eventsCount - 2;
            xPosCurve.AddKey(new Keyframe(lastTime, lastPosition.x, xPosCurve.keys[lastPrevIndex].outTangent, 0f));
            yPosCurve.AddKey(new Keyframe(lastTime, lastPosition.y, yPosCurve.keys[lastPrevIndex].outTangent, 0f));
            zPosCurve.AddKey(new Keyframe(lastTime, lastPosition.z, zPosCurve.keys[lastPrevIndex].outTangent, 0f));
            xRotCurve.AddKey(new Keyframe(lastTime, lastRotation.x, xRotCurve.keys[lastPrevIndex].outTangent, 0f));
            yRotCurve.AddKey(new Keyframe(lastTime, lastRotation.y, yRotCurve.keys[lastPrevIndex].outTangent, 0f));
            zRotCurve.AddKey(new Keyframe(lastTime, lastRotation.z, zRotCurve.keys[lastPrevIndex].outTangent, 0f));
            wRotCurve.AddKey(new Keyframe(lastTime, lastRotation.w, wRotCurve.keys[lastPrevIndex].outTangent, 0f));

            var animationClip = new AnimationClip { name = "Camera Pose" };
            animationClip.SetCurve("", typeof(Transform), "localPosition.x", xPosCurve);
            animationClip.SetCurve("", typeof(Transform), "localPosition.y", yPosCurve);
            animationClip.SetCurve("", typeof(Transform), "localPosition.z", zPosCurve);
            animationClip.SetCurve("", typeof(Transform), "localRotation.x", xRotCurve);
            animationClip.SetCurve("", typeof(Transform), "localRotation.y", yRotCurve);
            animationClip.SetCurve("", typeof(Transform), "localRotation.z", zRotCurve);
            animationClip.SetCurve("", typeof(Transform), "localRotation.w", wRotCurve);
            newAssets.Add(animationClip);

            var animationTrack = timeline.CreateTrack<AnimationTrack>(null, "Camera Pose");
            var timelineClip = animationTrack.CreateClip(animationClip);
            var animationPlayable = (AnimationPlayableAsset)timelineClip.asset;
            animationPlayable.removeStartOffset = false;

            var recording = ScriptableObject.CreateInstance<CameraPoseRecording>();
            recording.hideFlags = HideFlags.NotEditable;
            recording.AnimationTrack = animationTrack;
            return recording;
        }

        protected override void Setup()
        {
            this.AddSlowTask(m_RecordCameraTask, m_PoseRecordInterval);
            m_LastPose = this.GetPose();
            m_LastPoseUnchanged = false;
            m_PoseEvents.Clear();
            m_PoseEvents.Add(new PoseEvent
            {
                time = 0f,
                pose = m_LastPose
            });
        }

        protected override void TearDown() { this.RemoveSlowTask(m_RecordCameraTask); }

        protected override void FinalizeRecording()
        {
            m_PoseEvents.Add(new PoseEvent
            {
                time = TimeFromStart,
                pose = this.GetPose()
            });
        }

        void RecordCurrentCameraPose()
        {
            var currentPose = this.GetPose();
            if (currentPose == m_LastPose)
            {
                // No need to continually record the same pose
                m_LastPoseUnchanged = true;
                return;
            }

            var currentTime = TimeFromStart;
            if (m_LastPoseUnchanged)
            {
                // Pose that was not changing has now changed, so we need to record an event for the end of the elapsed time
                m_LastPoseUnchanged = false;
                m_PoseEvents.Add(new PoseEvent
                {
                    time = currentTime - m_PoseRecordInterval,
                    pose = m_LastPose
                });
            }

            m_PoseEvents.Add(new PoseEvent
            {
                time = currentTime,
                pose = currentPose
            });

            m_LastPose = currentPose;
        }
    }
}
