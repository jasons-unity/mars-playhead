using Unity.Labs.ModuleLoader;
using UnityEngine;

#if INCLUDE_MARS
using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using Unity.Labs.Utils;

#if ENABLE_ULS_TRACKER && (!UNITY_IOS || UNITY_EDITOR)
using ULSTrackerForUnity;
#endif

#if UNITY_EDITOR
[assembly: OptionalDependency("ULSTrackerForUnity.Plugins", "ENABLE_ULS_TRACKER")]
#endif
#endif

namespace Unity.Labs.MARS
{
    // Originally derived from Object3D.cs (which has been deleted) from ULSee package
#if INCLUDE_MARS && ENABLE_ULS_TRACKER && (!UNITY_IOS || UNITY_EDITOR)
    class ULSFaceTrackingProvider : MonoBehaviour, IProvidesFaceTracking, IProvidesCameraOffset,
        IProvidesFacialExpressions, IProvidesCameraIntrinsics, IProvidesCameraPreview, IProvidesCameraPose,
        IProvidesCameraProjectionMatrix, IProvidesTraits<bool>, IProvidesTraits<Pose>, IUsesMARSTrackableData<IMRFace>,
        IUsesCameraTexture
    {
        class ULSFace : IMRFace
        {
            readonly Dictionary<MRFaceLandmark, Pose> m_LandmarkPoses = new Dictionary<MRFaceLandmark, Pose>();
            readonly Dictionary<MRFaceExpression, float> m_Expressions = new Dictionary<MRFaceExpression, float>();

            readonly Dictionary<MRFaceExpression, ULSFaceExpression> m_Actions
                = new Dictionary<MRFaceExpression, ULSFaceExpression>(new MRFaceExpressionComparer());

            readonly ULSLeftEyeCloseExpression m_LeftEyeClose = new ULSLeftEyeCloseExpression();
            readonly ULSRightEyeCloseExpression m_RightEyeClose = new ULSRightEyeCloseExpression();
            readonly ULSLeftEyebrowRaiseExpression m_LeftEyebrowRaise = new ULSLeftEyebrowRaiseExpression();
            readonly ULSRightEyebrowRaiseExpression m_RightEyebrowRaise = new ULSRightEyebrowRaiseExpression();
            readonly ULSLeftLipCornerRaiseExpression m_LeftLipCornerRaise = new ULSLeftLipCornerRaiseExpression();
            readonly ULSRightLipCornerRaiseExpression m_RightLipCornerRaise = new ULSRightLipCornerRaiseExpression();
            readonly ULSMouthOpenExpression m_MouthOpenClose = new ULSMouthOpenExpression();
            readonly ULSBothEyebrowsRaiseExpression m_BothEyebrowsRaise;
            readonly ULSSmileExpression m_Smile;

            public ULSLeftEyeCloseExpression leftEyeClose { get { return m_LeftEyeClose; } }
            public ULSRightEyeCloseExpression rightEyeClose { get { return m_RightEyeClose; } }
            public ULSLeftEyebrowRaiseExpression leftEyebrowRaise { get { return m_LeftEyebrowRaise; } }
            public ULSRightEyebrowRaiseExpression rightEyebrowRaise { get { return m_RightEyebrowRaise; } }
            public ULSLeftLipCornerRaiseExpression leftLipCornerRaise { get { return m_LeftLipCornerRaise; } }
            public ULSRightLipCornerRaiseExpression rightLipCornerRaise { get { return m_RightLipCornerRaise; } }
            public ULSMouthOpenExpression mouthOpenClose { get { return m_MouthOpenClose; } }
            public ULSBothEyebrowsRaiseExpression bothEyebrowsRaise { get { return m_BothEyebrowsRaise; } }
            public ULSSmileExpression smile { get { return m_Smile; } }

            public void SubscribeToExpression(MRFaceExpression expressionName, Action<float> engaged, Action<float> disengaged)
            {
                ULSFaceExpression expression;
                if (m_Actions.TryGetValue(expressionName, out expression))
                {
                    if (engaged != null)
                        expression.Engaged += engaged;
                    if (disengaged != null)
                        expression.Disengaged += disengaged;
                }
            }

            public void UnsubscribeToExpression(MRFaceExpression expressionName, Action<float> engaged, Action<float> disengaged)
            {
                ULSFaceExpression expression;
                if (m_Actions.TryGetValue(expressionName, out expression))
                {
                    if (engaged != null)
                        expression.Engaged -= engaged;
                    if (disengaged != null)
                        expression.Disengaged -= disengaged;
                }
            }

            /// <summary>
            /// The id of this face as determined by the provider
            /// </summary>
            public MarsTrackableId id { get; internal set; }

            /// <summary>
            /// The pose of this face
            /// </summary>
            public Pose pose { get; internal set; }

            /// <summary>
            /// A mesh for this face, if one exists
            /// </summary>
            public Mesh Mesh { get { return null; } }

            /// <summary>
            /// World poses of available face landmarks
            /// </summary>
            public Dictionary<MRFaceLandmark, Pose> LandmarkPoses { get { return m_LandmarkPoses; } }

            /// <summary>
            /// 0-1 coefficients representing the display of available facial expressions
            /// </summary>
            public Dictionary<MRFaceExpression, float> Expressions { get { return m_Expressions; } }

            public ULSFace()
            {
                m_BothEyebrowsRaise = new ULSBothEyebrowsRaiseExpression(m_LeftEyebrowRaise, m_RightEyebrowRaise);
                m_Smile = new ULSSmileExpression(m_LeftLipCornerRaise, m_RightLipCornerRaise);

                m_Actions.Add(MRFaceExpression.LeftEyeClose, leftEyeClose);
                m_Actions.Add(MRFaceExpression.RightEyeClose, rightEyeClose);
                m_Actions.Add(MRFaceExpression.LeftEyebrowRaise, leftEyebrowRaise);
                m_Actions.Add(MRFaceExpression.RightEyebrowRaise, rightEyebrowRaise);
                m_Actions.Add(MRFaceExpression.BothEyebrowsRaise, bothEyebrowsRaise);
                m_Actions.Add(MRFaceExpression.MouthOpen, mouthOpenClose);
                m_Actions.Add(MRFaceExpression.LeftLipCornerRaise, leftLipCornerRaise);
                m_Actions.Add(MRFaceExpression.RightLipCornerRaise, rightLipCornerRaise);
                m_Actions.Add(MRFaceExpression.Smile, smile);
            }
        }

        const float k_MinScale = 0.001f;
        const float k_CameraPreviewScale = 0.01f;

        // Current focal length, scale, and offset values found to match ARKit via trial and error
        const float k_FocalLength = 450;
        static readonly Quaternion k_RotationOffset = Quaternion.AngleAxis(180, Vector3.forward);

        static readonly TraitDefinition[] k_ProvidedTraits =
        {
            TraitDefinitions.Face,
            TraitDefinitions.Pose
        };

        // Tweak scale and offset to line up with real-world values
        [SerializeField]
        float m_PoseScale = 0.0033f;

        [SerializeField]
        Vector3 m_PositionOffset = new Vector3(0, 0.1f, 0.25f);

        [SerializeField]
        Vector3 m_EarOffset = new Vector3(0f, 0.2f, 0.1f);

        [SerializeField]
        [Range(0.75f, 1f)]
        float m_RotationSmoothing = 0.9f;

        [SerializeField]
        [Range(0.75f, 1f)]
        float m_PositionSmoothing = 0.95f;

        [SerializeField]
        float m_PositionThreshold = 0.025f;

        [SerializeField]
        float m_RotationThreshold = 0.9f;

        [Tooltip("Enables 3d estimation of facial landmarks")]
        [SerializeField]
        bool m_TrackLandmarks = true;

        [Tooltip("Enables facial expressions events such as winks & smiles.  Will enable tracking landmarks.")]
        [SerializeField]
        bool m_GenerateExpressions = true;

        readonly float[] m_TrackedPoints = new float[Plugins.MAX_TRACKER_POINTS * 3];
        readonly float[] m_PointConfidences = new float[Plugins.MAX_TRACKER_POINTS];
        readonly float[] m_Matrix = new float[16];
        Matrix4x4 m_TransformationMatrix = Matrix4x4.identity;
        Matrix4x4 m_AdjustMatrix;
        readonly float[] m_IntrinsicCameraMatrix =
        {
            k_FocalLength, 0f, 320f,
            0f, k_FocalLength, 240f,
            0f, 0f, 1f
        };

        Camera m_Camera;
        float m_ImageHalfWidth;
        float m_ImageHalfHeight;

        bool m_InitDone;

        readonly Vector3[][] m_RawLandmarks = new Vector3[Plugins.MAX_TRACKER_FACES][];
        readonly ULSFace[] m_TrackedFaces = new ULSFace[Plugins.MAX_TRACKER_FACES];
        readonly bool[] m_TrackingStates = new bool[Plugins.MAX_TRACKER_FACES];
        readonly float[] m_GazeX = new float[1];
        readonly float[] m_GazeY = new float[1];
        readonly float[] m_GazeZ = new float[1];

        readonly Vector3[] m_SmoothedPositions = new Vector3[Plugins.MAX_TRACKER_FACES];
        readonly Pose[] m_TargetPoses = new Pose[Plugins.MAX_TRACKER_FACES];
        readonly Dictionary<MRFaceLandmark, Pose>[] m_LandmarkTargetPoses
            = new Dictionary<MRFaceLandmark, Pose>[Plugins.MAX_TRACKER_FACES];

        static ULSFacialExpressionSettings s_ExpressionSettings;

        Material m_PreviewMaterial;
        float m_FOV;
        float m_Scale = 1;
        float m_InverseScale;
        CameraDispatch m_Dispatch;
        Matrix4x4 m_OffsetMatrix = Matrix4x4.identity;

#if UNITY_STANDALONE || UNITY_EDITOR
        bool m_RunInEditModeDirty;
        float m_StartFOV;
        float m_StartOrthographicSize;
#endif

#if !FI_AUTOFILL
        IProvidesCameraTexture IFunctionalitySubscriber<IProvidesCameraTexture>.provider { get; set; }
#endif

        internal bool[] trackingStates { get { return m_TrackingStates; } }
        internal Pose[] targetPoses { get { return m_TargetPoses; } }
        internal Vector3[][] rawLandmarks { get { return m_RawLandmarks; } }
        internal float poseScale { get { return m_PoseScale; } }

        public int GetMaxFaceCount() { return Plugins.MAX_TRACKER_FACES; }

        public event Action<IMRFace> FaceAdded;
        public event Action<IMRFace> FaceUpdated;
        public event Action<IMRFace> FaceRemoved;

        public event Action<IProvidesCameraPreview> previewReady;

        // ULSFaceTrackingProvider provides camera pose but never updates it
        // This is to override any other camera pose providers which might try to access the physical camera and cause a crash
#pragma warning disable 67
        public event Action<Pose> poseUpdated;
        public event Action<MRCameraTrackingState> trackingStateChanged;
#pragma warning restore 67

        public Vector3 cameraPositionOffset
        {
            get { return Vector3.zero; }
            set {}
        }

        public float cameraYawOffset
        {
            get { return 0f; }
            set {}
        }

        public float cameraScale
        {
            get { return m_Scale; }
            set
            {
                if (value <= 0)
                    value = k_MinScale;

                m_InverseScale = 1 / value;
                m_Scale = value;
                m_OffsetMatrix = Matrix4x4.Scale(Vector3.one * value);
            }
        }

        public Matrix4x4 CameraOffsetMatrix { get { return m_OffsetMatrix; } }

        public Pose ApplyOffsetToPose(Pose pose)
        {
            pose.position *= m_Scale;
            return pose;
        }

        public Pose ApplyInverseOffsetToPose(Pose pose)
        {
            pose.position *= m_InverseScale;
            return pose;
        }

        public Vector3 ApplyOffsetToPosition(Vector3 position) { return position * m_Scale; }

        /// <summary>
        /// Apply the inverse of the camera offset to a position and return the modified position
        /// </summary>
        /// <param name="position">The position to which the offset will be applied</param>
        /// <returns>The modified position</returns>
        public Vector3 ApplyInverseOffsetToPosition(Vector3 position)
        {
            return position * m_InverseScale;
        }

        public Vector3 ApplyOffsetToDirection(Vector3 direction) { return direction; }

        public Vector3 ApplyInverseOffsetToDirection(Vector3 direction) { return direction; }

        public Quaternion ApplyOffsetToRotation(Quaternion rotation) { return rotation; }

        public Quaternion ApplyInverseOffsetToRotation(Quaternion rotation) { return rotation; }

        public TraitDefinition[] GetProvidedTraits() { return k_ProvidedTraits; }

        public Matrix4x4? GetProjectionMatrix() { return null; }

        void Awake()
        {
            if (m_GenerateExpressions)
                m_TrackLandmarks = true;
        }

        void Start()
        {
            s_ExpressionSettings = ULSFacialExpressionSettings.instance;
            Plugins.OnPreviewStart = InitCameraTexture;
            MarsTime.MarsUpdate += OnMarsUpdate;

            var initTracker = Plugins.ULS_UnityTrackerInit(transform);
            if (initTracker < 0)
                Debug.LogError("Failed to initialize tracker.");
            else
                Debug.Log("Tracker initialization succeeded");

            for (var i = 0; i < Plugins.MAX_TRACKER_FACES; i++)
            {
                var face = new ULSFace();
                var landmarkPoses = face.LandmarkPoses;
                var targets = new Dictionary<MRFaceLandmark, Pose>(new MRFaceLandmarkComparer());
                face.id = MarsTrackableId.Create();
                m_TrackedFaces[i] = face;
                m_RawLandmarks[i] = new Vector3[Plugins.MAX_TRACKER_POINTS];
                m_LandmarkTargetPoses[i] = targets;
                foreach (var landmark in EnumValues<MRFaceLandmark>.Values)
                {
                    landmarkPoses.Add(landmark, new Pose { rotation = Quaternion.identity });
                    targets.Add(landmark, new Pose { rotation = Quaternion.identity });
                }
            }

            m_Dispatch = GetComponentInChildren<CameraDispatch>();
            m_Dispatch.GetCameraTexture = this.GetCameraTexture;

#if UNITY_EDITOR
            if (runInEditMode)
                m_Dispatch.StartRunInEditMode();
#endif
        }

        public void LoadProvider() {}

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var faceTrackingSubscriber = obj as IFunctionalitySubscriber<IProvidesFaceTracking>;
            if (faceTrackingSubscriber != null)
                faceTrackingSubscriber.provider = this;

            var cameraOffsetSubscriber = obj as IFunctionalitySubscriber<IProvidesCameraOffset>;
            if (cameraOffsetSubscriber != null)
                cameraOffsetSubscriber.provider = this;

            var cameraPoseSubscriber = obj as IFunctionalitySubscriber<IProvidesCameraPose>;
            if (cameraPoseSubscriber != null)
                cameraPoseSubscriber.provider = this;

            var cameraProjectionSubscriber = obj as IFunctionalitySubscriber<IProvidesCameraProjectionMatrix>;
            if (cameraProjectionSubscriber != null)
                cameraProjectionSubscriber.provider = this;

            var faceExpressionSubscriber = obj as IFunctionalitySubscriber<IProvidesFacialExpressions>;
            if (faceExpressionSubscriber != null)
                faceExpressionSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() {}

        public Vector3 GetPreviewObjectPosition() { return transform.position; }

        public float GetFOV() { return m_FOV; }

        public Pose GetCameraPose() { return new Pose(); }

        void InitCameraTexture(Texture preview, int rotate)
        {
            var width = preview.width;
            var height = preview.height;
            m_ImageHalfWidth = width * 0.5f;
            m_ImageHalfHeight = height * 0.5f;

            var previewRenderer = GetComponent<Renderer>();
            var firstRun = m_PreviewMaterial == null;
            var oldMaterial = previewRenderer.sharedMaterial;
            m_PreviewMaterial = Instantiate(oldMaterial);
            m_PreviewMaterial.mainTexture = preview;
            previewRenderer.sharedMaterial = m_PreviewMaterial;
            if (!firstRun)
                UnityObjectUtils.Destroy(oldMaterial);

            m_IntrinsicCameraMatrix[2] = m_ImageHalfWidth;
            m_IntrinsicCameraMatrix[5] = m_ImageHalfHeight;

            // get FOV for AR camera by calibration function
            var fovX = new float[1];
            var fovY = new float[1];
            Plugins.ULS_UnityCalibration(m_IntrinsicCameraMatrix, width, height, fovX, fovY);

            transform.localScale = new Vector3(width, height, 1) * k_CameraPreviewScale;

            m_Camera = Camera.main;
            if (m_Camera != null)
            {
#if UNITY_STANDALONE || UNITY_EDITOR
                transform.parent.localScale = new Vector3(-1, -1, 1);
                transform.parent.localPosition = new Vector3(m_ImageHalfWidth, m_ImageHalfHeight, 0);
                m_RunInEditModeDirty = true;

                m_StartFOV = m_Camera.fieldOfView;
                m_StartOrthographicSize = m_Camera.orthographicSize;
                m_Camera.orthographicSize = height * 0.5f;
                m_FOV = fovY[0];
                m_Camera.fieldOfView = m_FOV;
                m_AdjustMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(-1, -1, 1));

#elif UNITY_ANDROID
                if (Screen.orientation == ScreenOrientation.LandscapeLeft || Screen.orientation == ScreenOrientation.LandscapeRight)
                {
                    m_Camera.orthographicSize = m_ImageHalfHeight;
                    var v = (float)(width * Screen.height) / (height * Screen.width);
                    m_Camera.rect = new Rect((1 - v) * 0.5f, 0, v, 1); // AR viewport
                    m_FOV = fovY[0];
                    m_Camera.fieldOfView = m_FOV;
                }
                else
                {
                    var aspect = (float)Screen.height / Screen.width * height / width;
                    var v = 1f / aspect;
                    m_Camera.orthographicSize = m_ImageHalfWidth * aspect;
                    m_Camera.rect = new Rect(0, (1 - v) * 0.5f, 1, v); // AR viewport
                    m_FOV = fovX[0];
                    m_Camera.fieldOfView = m_FOV;
                }

                transform.parent.localPosition = Vector3.zero; //reset position for rotation
                transform.parent.transform.eulerAngles = new Vector3(0, 0, rotate); //orientation
                transform.parent.localPosition = transform.parent.transform.TransformPoint(-transform.localPosition); //move to center

                m_AdjustMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(new Vector3(0, 0, rotate)), new Vector3(1, 1, 1));
#endif
            }

            transform.position = new Vector3(0, 0, k_FocalLength * k_CameraPreviewScale);

            m_InitDone = true;

            if (previewReady != null)
                previewReady(this);
        }

        void ProcessLandmarks(int faceIndex)
        {
            Plugins.ULS_UnityGetConfidence(m_PointConfidences, faceIndex);
            var landmarks = m_RawLandmarks[faceIndex];

            for (var i = 0; i < m_PointConfidences.Length; i++)
            {
                // 0 confidence means failure to acquire that landmark
                if (m_PointConfidences[i] > 0f)
                {
                    var index = i * 3;
                    var trackedPoint = new Vector3(m_TrackedPoints[index], m_TrackedPoints[index + 1], m_TrackedPoints[index + 2]);

                    // Due to float precision we need to apply the scale to the landmark local positions separately from the head pose
                    landmarks[i] = k_RotationOffset * trackedPoint * m_PoseScale;
                }
            }
        }

        void OnMarsUpdate()
        {
            if (!m_InitDone)
                return;

            for (var i = 0; i < Plugins.MAX_TRACKER_FACES; ++i)
            {
                var face = m_TrackedFaces[i];
                var wasTracked = m_TrackingStates[i];
                if (Plugins.ULS_UnityGetPoints3D(m_TrackedPoints, i) <= 0)
                {
                    m_TrackingStates[i] = false;
                    if (wasTracked)
                        OnFaceRemoved(face);

                    continue;
                }

                m_TrackingStates[i] = true;

                Plugins.ULS_UnityGetTransform(m_Matrix, m_IntrinsicCameraMatrix, null, i);
                for (var j = 0; j < 16; ++j)
                {
                    m_TransformationMatrix[j] = m_Matrix[j];
                }

                var matrix = m_AdjustMatrix * m_TransformationMatrix;

                var pose = face.pose;
                var smoothedPosition = m_SmoothedPositions[i];
                var currentRotation = pose.rotation;
                var rotation = ARUtils.GetRotationFromMatrix(ref matrix) * k_RotationOffset;
                var position = ARUtils.GetTranslationFromMatrix(ref matrix);

                var targetPose = m_TargetPoses[i];
                if (Vector3.Distance(targetPose.position, position) > m_PositionThreshold)
                    targetPose.position = position;

                if (Quaternion.Angle(targetPose.rotation, rotation) > m_RotationThreshold)
                    targetPose.rotation = rotation;

                smoothedPosition = Vector3.Lerp(smoothedPosition, targetPose.position, m_PositionSmoothing);
                currentRotation = Quaternion.Lerp(currentRotation, targetPose.rotation, m_RotationSmoothing);

                m_TargetPoses[i] = targetPose;
                m_SmoothedPositions[i] = smoothedPosition;

                face.pose = new Pose
                {
                    position = smoothedPosition * m_PoseScale + currentRotation * m_PositionOffset,
                    rotation = currentRotation
                };

                var landmarks = m_RawLandmarks[i];
                if (m_TrackLandmarks || m_GenerateExpressions)
                    ProcessLandmarks(i);

                if (m_TrackLandmarks)
                    MapToAuthorLandmarks(landmarks, face.LandmarkPoses, position * m_PoseScale, rotation, i);

                if (m_GenerateExpressions)
                    CalculateExpressions(landmarks, face);

                m_TrackedFaces[i] = face;

                if (wasTracked)
                    OnFaceUpdated(face);
                else
                    OnFaceAdded(face);
            }
        }

        void OnFaceAdded(IMRFace face)
        {
            var id = this.AddOrUpdateData(face);
            this.AddOrUpdateTrait(id, TraitNames.Face, true);
            this.AddOrUpdateTrait(id, TraitNames.Pose, face.pose);
            if (FaceAdded != null)
                FaceAdded(face);
        }

        void OnFaceUpdated(IMRFace face)
        {
            var id = this.AddOrUpdateData(face);
            this.AddOrUpdateTrait(id, TraitNames.Pose, face.pose);

            if (FaceUpdated != null)
                FaceUpdated(face);
        }

        void OnFaceRemoved(IMRFace face)
        {
            var id = this.RemoveData(face);
            this.RemoveTrait<bool>(id, TraitNames.Face);
            this.RemoveTrait<Pose>(id, TraitNames.Pose);
            if (FaceRemoved != null)
                FaceRemoved(face);
        }

        public void GetFaces(List<IMRFace> faces)
        {
            for (var i = 0; i < Plugins.MAX_TRACKER_FACES; i++)
            {
                if (m_TrackingStates[i])
                    faces.Add(m_TrackedFaces[i]);
            }
        }

        void MapToAuthorLandmarks(Vector3[] positions, Dictionary<MRFaceLandmark, Pose> landmarkPoses, Vector3 position, Quaternion rotation, int faceIndex)
        {
            var chin = new Pose(position + rotation * positions[(int)ULSFaceLandmarks.ChinMiddle8], rotation);
            var noseBridge = new Pose(position + rotation * positions[(int)ULSFaceLandmarks.NoseBridge27], rotation);
            var noseTip = new Pose(position + rotation * positions[(int)ULSFaceLandmarks.NoseTip30], rotation);

            // ULS labels are the reverse of ARKit
            var rightEyeOuter = positions[(int)ULSFaceLandmarks.LeftEyeOuter45];
            var rightEyeUpper = (positions[(int)ULSFaceLandmarks.LeftEyeUpper43] + positions[(int)ULSFaceLandmarks.LeftEyeUpper44]) * 0.5f;
            var rightEyeInner = positions[(int)ULSFaceLandmarks.LeftEyeInner42];
            var rightEyeLower = (positions[(int)ULSFaceLandmarks.LeftEyeLower46] + positions[(int)ULSFaceLandmarks.LeftEyeLower47]) * 0.5f;
            var rightEyeAverage = (rightEyeOuter + rightEyeUpper + rightEyeInner + rightEyeLower) * 0.25f;

            Plugins.ULS_UnityGetLeftGaze(m_GazeX, m_GazeY, m_GazeZ, faceIndex);
            var rightEyeRotation = rotation * Quaternion.LookRotation(new Vector3(m_GazeX[0], m_GazeY[0], -m_GazeZ[0]));
            var rightEye = new Pose(position + rotation * rightEyeAverage, rightEyeRotation);

            var leftEyeOuter = positions[(int)ULSFaceLandmarks.RightEyeOuter36];
            var leftEyeUpper = (positions[(int)ULSFaceLandmarks.RightEyeUpper37] + positions[(int)ULSFaceLandmarks.RightEyeUpper38]) * 0.5f;
            var leftEyeInner = positions[(int)ULSFaceLandmarks.RightEyeInner39];
            var leftEyeLower = (positions[(int)ULSFaceLandmarks.RightEyeLower40] + positions[(int)ULSFaceLandmarks.RightEyeLower41]) * 0.5f;
            var leftEyeAverage = (leftEyeOuter + leftEyeUpper + leftEyeInner + leftEyeLower) * 0.25f;

            Plugins.ULS_UnityGetRightGaze(m_GazeX, m_GazeY, m_GazeZ, faceIndex);
            var leftEyeRotation = rotation * Quaternion.LookRotation(new Vector3(m_GazeX[0], m_GazeY[0], -m_GazeZ[0]));
            var leftEye = new Pose(position + rotation * leftEyeAverage, leftEyeRotation);

            var rightEyebrowInner = positions[(int)ULSFaceLandmarks.LeftEyebrowInner22];
            var rightEyebrowOuter = positions[(int)ULSFaceLandmarks.LeftEyebrowOuter26];
            var rightEyebrowAverage = (rightEyebrowInner + rightEyebrowOuter) * 0.5f;
            var rightEyebrow = new Pose(position + rotation * rightEyebrowAverage, rotation);

            var leftEyebrowInner = positions[(int)ULSFaceLandmarks.RightEyebrowInner21];
            var leftEyebrowOuter = positions[(int)ULSFaceLandmarks.RightEyebrowOuter17];
            var leftEyebrowAverage = (leftEyebrowInner + leftEyebrowOuter) * 0.5f;
            var leftEyebrow = new Pose(position + rotation * leftEyebrowAverage, rotation);

            var upperLipLeft = positions[(int)ULSFaceLandmarks.UpperLipLeft52];
            var upperLipRight = positions[(int)ULSFaceLandmarks.UpperLipRight50];
            var upperLipAverage = (upperLipLeft + upperLipRight) * 0.5f;
            var upperLip = new Pose(position + rotation * upperLipAverage, rotation);

            var lowerLipLeft = positions[(int)ULSFaceLandmarks.LowerLipLeft56];
            var lowerLipRight = positions[(int)ULSFaceLandmarks.LowerLipRight58];
            var lowerLipAverage = (lowerLipLeft + lowerLipRight) * 0.5f;
            var lowerLip = new Pose(position + rotation * lowerLipAverage, rotation);

            var upperLipMiddle = positions[(int)ULSFaceLandmarks.UpperLipMiddle51];
            var lowerLipMiddle = positions[(int)ULSFaceLandmarks.LowerLipMiddle57];
            var leftLipCorner = positions[(int)ULSFaceLandmarks.LeftLipCorner54];
            var rightLipCorner = positions[(int)ULSFaceLandmarks.RightLipCorner48];
            var mouthAverage = (upperLipMiddle + lowerLipMiddle + leftLipCorner + rightLipCorner) * 0.25f;
            var mouth = new Pose(position + rotation * mouthAverage, rotation);

            // we don't get ear landmarks out of ULS, but the rear jaw landmark is close
            var leftRearJaw = positions[(int)ULSFaceLandmarks.RightJaw0];
            var rightRearJaw = positions[(int)ULSFaceLandmarks.LeftJaw16];
            var rightEar = new Pose(position + rotation * (rightRearJaw + m_EarOffset), rotation);
            var leftEar = new Pose(position + rotation * (leftRearJaw + m_EarOffset), rotation);

            SetLandmarkPose(landmarkPoses, MRFaceLandmark.Chin, chin, faceIndex);
            SetLandmarkPose(landmarkPoses, MRFaceLandmark.NoseBridge, noseBridge, faceIndex);
            SetLandmarkPose(landmarkPoses, MRFaceLandmark.NoseTip, noseTip, faceIndex);
            SetLandmarkPose(landmarkPoses, MRFaceLandmark.Mouth, mouth, faceIndex);
            SetLandmarkPose(landmarkPoses, MRFaceLandmark.UpperLip, upperLip, faceIndex);
            SetLandmarkPose(landmarkPoses, MRFaceLandmark.LowerLip, lowerLip, faceIndex);
            SetLandmarkPose(landmarkPoses, MRFaceLandmark.RightEyebrow, rightEyebrow, faceIndex);
            SetLandmarkPose(landmarkPoses, MRFaceLandmark.LeftEyebrow, leftEyebrow, faceIndex);
            SetLandmarkPose(landmarkPoses, MRFaceLandmark.RightEye, rightEye, faceIndex);
            SetLandmarkPose(landmarkPoses, MRFaceLandmark.LeftEye, leftEye, faceIndex);
            SetLandmarkPose(landmarkPoses, MRFaceLandmark.RightEar, rightEar, faceIndex);
            SetLandmarkPose(landmarkPoses, MRFaceLandmark.LeftEar, leftEar, faceIndex);
        }

        void SetLandmarkPose(Dictionary<MRFaceLandmark, Pose> landmarkPoses, MRFaceLandmark landmark, Pose pose, int faceIndex)
        {
            var currentPose = landmarkPoses[landmark];
            var currentPosition = currentPose.position;
            var currentRotation = currentPose.rotation;
            var position = pose.position;
            var rotation = pose.rotation;

            var targets = m_LandmarkTargetPoses[faceIndex];
            var target = targets[landmark];
            if (Vector3.Distance(target.position, position) > m_PositionThreshold * m_PoseScale)
                target.position = position;

            if (Quaternion.Angle(target.rotation, rotation) > m_RotationThreshold)
                target.rotation = rotation;

            currentPosition = Vector3.Lerp(currentPosition, target.position, m_PositionSmoothing);
            currentRotation = Quaternion.Lerp(currentRotation, target.rotation, m_RotationSmoothing);

            targets[landmark] = target;

            landmarkPoses[landmark] = new Pose { position = currentPosition, rotation = currentRotation };
        }

        static bool HeadPoseWithinExpressionRange(Quaternion rotation)
        {
            var euler = rotation.eulerAngles;
            if (euler.z > 180)
                euler.z = 360 - euler.z;
            if (euler.y > 180)
                euler.y = 360 - euler.y;
            if (euler.x > 180)
                euler.x = 360 - euler.x;

            var overX = euler.x > s_ExpressionSettings.maxHeadAngleX;
            var overY = euler.y > s_ExpressionSettings.maxHeadAngleY;
            var overZ = euler.z > s_ExpressionSettings.maxHeadAngleZ;

            if (overX || overY || overZ)
                return false;

            return true;
        }

        static void CalculateExpressions(Vector3[] landmarks, ULSFace face)
        {
            // Don't try calculating expressions with extreme head poses
            var withinRange = HeadPoseWithinExpressionRange(face.pose.rotation);

            var expressions = face.Expressions;
            expressions[MRFaceExpression.MouthOpen] = face.mouthOpenClose.Update(landmarks, withinRange);
            expressions[MRFaceExpression.LeftEyeClose] = face.leftEyeClose.Update(landmarks, withinRange);
            expressions[MRFaceExpression.RightEyeClose] = face.rightEyeClose.Update(landmarks, withinRange);
            expressions[MRFaceExpression.LeftEyebrowRaise] = face.leftEyebrowRaise.Update(landmarks, withinRange);
            expressions[MRFaceExpression.RightEyebrowRaise] = face.rightEyebrowRaise.Update(landmarks, withinRange);
            expressions[MRFaceExpression.BothEyebrowsRaise] = face.bothEyebrowsRaise.Update(landmarks, withinRange);
            expressions[MRFaceExpression.LeftLipCornerRaise] = face.leftLipCornerRaise.Update(landmarks, withinRange);
            expressions[MRFaceExpression.RightLipCornerRaise] = face.rightLipCornerRaise.Update(landmarks, withinRange);
            expressions[MRFaceExpression.Smile] = face.smile.Update(landmarks, withinRange);
        }

        // right now our interface doesn't support assigning different actions to multiple faces,
        // so subscribing to a face expression on providers that support multiple faces subscribes to it on all faces
        public void SubscribeToExpression(MRFaceExpression expressionName, Action<float> engaged, Action<float> disengaged)
        {
            foreach (var face in m_TrackedFaces)
            {
                face.SubscribeToExpression(expressionName, engaged, disengaged);
            }
        }

        public void UnsubscribeToExpression(MRFaceExpression expressionName, Action<float> engaged, Action<float> disengaged)
        {
            foreach (var face in m_TrackedFaces)
            {
                face.UnsubscribeToExpression(expressionName, engaged, disengaged);
            }
        }

#if UNITY_STANDALONE || UNITY_EDITOR
        void OnDisable()
        {
            MarsTime.MarsUpdate -= OnMarsUpdate;

#if UNITY_EDITOR
            if (runInEditMode)
                m_Dispatch.StopRunInEditMode();
#endif

            if (!m_Camera)
                return;

            if (m_RunInEditModeDirty)
            {
                m_Camera.fieldOfView = m_StartFOV;
                m_Camera.orthographicSize = m_StartOrthographicSize;
            }

            for (var i = 0; i < Plugins.MAX_TRACKER_FACES; i++)
            {
                if (m_TrackingStates[i])
                {
                    OnFaceRemoved(m_TrackedFaces[i]);
                    m_TrackingStates[i] = false;
                }
            }
        }
#else
        void OnDisable()
        {
            MarsTime.MarsUpdate -= OnMarsUpdate;
            Plugins.ULS_UnityTrackerTerminate();
        }
#endif
    }
#else
    class ULSFaceTrackingProvider : MonoBehaviour
    {
        [SerializeField]
        float m_PoseScale;

        [SerializeField]
        Vector3 m_PositionOffset;

        [SerializeField]
        Vector3 m_EarOffset;

        [SerializeField]
        float m_RotationSmoothing;

        [SerializeField]
        float m_PositionSmoothing;

        [SerializeField]
        float m_PositionThreshold;

        [SerializeField]
        float m_RotationThreshold;

        [SerializeField]
        bool m_TrackLandmarks;

        [SerializeField]
        bool m_GenerateExpressions;

        void Awake()
        {
            gameObject.SetActive(false);
        }
    }
#endif
}
