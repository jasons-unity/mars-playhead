using Unity.Labs.MARS.Data;
using Unity.Labs.MARS.Query;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// An action that scales child content by the bounds of its matching AR Object
    /// </summary>
    [ComponentTooltip("Scales children to fit the bounds of their parent Real World Object.")]
    [MonoBehaviourComponentMenu(typeof(StretchToExtentsAction), "Action/Stretch to Extents")]
    public class StretchToExtentsAction : TransformAction, ISpawnable, IUsesMARSTrackableData<MRPlane>, IUsesCameraOffset
    {
        static readonly Quaternion k_RotateQuarter = Quaternion.Euler(0.0f, 90.0f, 0.0f);

#if !FI_AUTOFILL
        IProvidesCameraOffset IFunctionalitySubscriber<IProvidesCameraOffset>.provider { get; set; }
#endif
        /// <summary>
        /// The modes of operation this stretch action can operate in
        /// </summary>
        enum PlanarStretchMode
        {
            /// <summary> Scale the content by the smallest axis of the match data </summary>
            Uniform = 0,
            /// <summary> Scale each axis of the content separately by the axes of the match data </summary>
            NonUniform,
            /// <summary> A non-uniform scale where the X axis gets scaled by the largest axis of the bounds </summary>
            AlignLongSideToX
        }

        enum VerticalStretchMode
        {
            /// <summary> Vertical scale of the content is set to one </summary>
            None,
            /// <summary> Vertical scale of the content is set to the smallest axis of the match data </summary>
            Minimum,
            /// <summary> Vertical scale of the content is set to the average of both axes of match data </summary>
            Average
        }

        [SerializeField]
        [Tooltip("How content should be scaled to match the extents of an AR Object ")]
        PlanarStretchMode m_PlanarStretch = PlanarStretchMode.Uniform;

        [SerializeField]
        [Tooltip("How content should be scaled vertically to match the extents of an AR Object ")]
        VerticalStretchMode m_VerticalStretch = VerticalStretchMode.None;

        void OnValidate()
        {
            transform.localPosition = Vector3.zero;
        }

        void OnEnable()
        {
            var parentObject = GetComponentInParent<Proxy>();
            if (parentObject.GetComponent<IsPlaneCondition>() == null && parentObject.GetComponent<PlaneSizeCondition>() == null)
            {
                Debug.LogWarning("Stretch to Extents requires plane data!  Make sure your query has a plane condition.");
                enabled = false;
                return;
            }

            transform.localPosition = Vector3.zero;
        }

        public void OnMatchAcquire(QueryResult queryResult)
        {
            UpdateScale(queryResult);
        }

        public void OnMatchUpdate(QueryResult queryResult)
        {
            UpdateScale(queryResult);
        }

        void UpdateScale(QueryResult queryResult)
        {
            var mrPlane = queryResult.ResolveValue(this);
            var planeScale = this.GetCameraScale();
            var bounds = mrPlane.extents * planeScale;

            var smallestBounds = Mathf.Min(bounds.x, bounds.y);
            var newScale = transform.localScale;

            switch (m_PlanarStretch)
            {
                case PlanarStretchMode.Uniform:
                    newScale.x = smallestBounds;
                    newScale.z = smallestBounds;
                    break;
                case PlanarStretchMode.NonUniform:
                    newScale.x = bounds.x;
                    newScale.z = bounds.y;
                    break;
                case PlanarStretchMode.AlignLongSideToX:
                    if (bounds.x < bounds.y)
                    {
                        transform.localRotation = k_RotateQuarter;
                        newScale.x = bounds.y;
                        newScale.z = bounds.x;
                    }
                    else
                    {
                        newScale.x = bounds.x;
                        newScale.z = bounds.y;
                    }
                    break;
            }

            switch (m_VerticalStretch)
            {
                case VerticalStretchMode.Minimum:
                    newScale.y = smallestBounds;
                    break;
                case VerticalStretchMode.Average:
                    newScale.y = (bounds.x + bounds.y) * 0.5f;
                    break;
            }

            transform.localScale = newScale;

            transform.SetWorldPose(this.ApplyOffsetToPose(mrPlane.pose.TranslateLocal(mrPlane.center)));
        }
    }
}
