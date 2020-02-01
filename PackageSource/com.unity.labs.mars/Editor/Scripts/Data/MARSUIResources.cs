using System;
using System.Collections.Generic;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

namespace Unity.Labs.MARS
{
    [ScriptableSettingsPath("MARS/Editor")]
    public class MARSUIResources : EditorScriptableSettings<MARSUIResources>
    {
#pragma warning disable 649
        [SerializeField]
        VideoClip m_DefaultRecordedVideo;

        [SerializeField]
        Texture2D m_NextEnvironmentIcon;

        [SerializeField]
        Texture2D m_PreviousEnvironmentIcon;

        [SerializeField]
        Texture2D m_StopIcon;

        [SerializeField]
        Texture2D m_WarningTexture;

        [Header("Create Icons")]

        [SerializeField]
        DarkLightIconPair m_CreationPanelDefaultIcon;

        [SerializeField]
        DarkLightIconPair m_ProxyObject;

        [SerializeField]
        DarkLightIconPair m_Set;

        [SerializeField]
        DarkLightIconPair m_Replicator;

        [SerializeField]
        DarkLightIconPair m_SyntheticObject;

        [Header("Condition Icons")]

        [SerializeField]
        ConditionIconData m_ElevationIcons;

        [SerializeField]
        ConditionIconData m_ProximityIcons;

        [SerializeField]
        ConditionIconData m_SizeIcons;

        [SerializeField]
        ConditionIconData m_AngleIcons;

        [Header("Hierarchy Icons")]

        [SerializeField]
        ClientIconData m_FaceIcons;

        [SerializeField]
        ClientIconData m_MarkerIcons;

        [SerializeField]
        ClientIconData m_ContextIcons;

        [SerializeField]
        ClientIconData m_SetIcons;

        [SerializeField]
        Texture2D m_SimulationViewIcon;

        [SerializeField]
        Texture2D m_CanEditIcon;

        [SerializeField]
        Texture2D m_CanNotEditIcon;

        [Header("Editor GUI")]

        [SerializeField]
        Texture2D m_ToolbarBackground;

        [SerializeField]
        Texture2D m_ToolbarBackground2X;

        [SerializeField]
        Texture2D m_ToolbarBackgroundPro;

        [SerializeField]
        Texture2D m_ToolbarBackgroundPro2X;

        [SerializeField]
        Texture2D m_ToolbarButton;

        [SerializeField]
        Texture2D m_ToolbarButton2X;

        [SerializeField]
        Texture2D m_ToolbarDropdown;

        [SerializeField]
        Texture2D m_ToolbarDropdownPro;

        [Header("Snapping GUI")]

        [SerializeField]
        DarkLightIconPair m_OrientToSurface;

        [SerializeField]
        DarkLightIconPair m_PivotSnapping;

        [SerializeField]
        DarkLightIconPair m_XUp;

        [SerializeField]
        DarkLightIconPair m_XDown;

        [SerializeField]
        DarkLightIconPair m_YUp;

        [SerializeField]
        DarkLightIconPair m_YDown;

        [SerializeField]
        DarkLightIconPair m_ZUp;

        [SerializeField]
        DarkLightIconPair m_ZDown;
#pragma warning restore 649

        Dictionary<Type, ConditionIconData> m_Icons;

        public Texture2D OrientToSurfaceIcon => m_OrientToSurface.Icon;
        public Texture2D PivotSnappingIcon => m_PivotSnapping.Icon;
        public Texture2D XUpIcon => m_XUp.Icon;
        public Texture2D XDownIcon => m_XDown.Icon;
        public Texture2D YUpIcon => m_YUp.Icon;
        public Texture2D YDownIcon => m_YDown.Icon;
        public Texture2D ZUpIcon => m_ZUp.Icon;
        public Texture2D ZDownIcon => m_ZDown.Icon;

        public Texture2D ToolbarBackground => EditorGUIUtility.isProSkin ? m_ToolbarBackgroundPro : m_ToolbarBackground;

        public Texture2D ToolbarBackground2X => EditorGUIUtility.isProSkin ? m_ToolbarBackgroundPro2X : m_ToolbarBackground2X;

        public Texture2D ToolbarButton => m_ToolbarButton;
        public Texture2D ToolbarButton2X => m_ToolbarButton2X;

        public Texture2D ToolbarDropdown => EditorGUIUtility.isProSkin ? m_ToolbarDropdownPro : m_ToolbarDropdown;

        public VideoClip DefaultRecordedVideo => m_DefaultRecordedVideo;
        public Texture2D NextEnvironmentIcon => m_NextEnvironmentIcon;
        public Texture2D PreviousEnvironmentIcon => m_PreviousEnvironmentIcon;
        public Texture2D StopIcon => m_StopIcon;
        public Texture2D WarningTexture => m_WarningTexture;
        public Texture2D CreationPanelDefaultIcon => m_CreationPanelDefaultIcon.Icon;
        public Texture2D RealWorldObjectIcon => m_ProxyObject.Icon;
        public Texture2D SetIcon => m_Set.Icon;
        public Texture2D ReplicatorIcon => m_Replicator.Icon;
        public Texture2D SyntheticObjectIcon => m_SyntheticObject.Icon;
        public Texture2D SimulationViewIcon => m_SimulationViewIcon;
        public Texture2D CanEditIcon => m_CanEditIcon;
        public Texture2D CanNotEditIcon => m_CanNotEditIcon;

        public DarkLightIconPair CreationPanelDefaultIconPair => m_CreationPanelDefaultIcon;
        public DarkLightIconPair ProxyObjectIconPair => m_ProxyObject;
        public DarkLightIconPair SetIconPair => m_Set;
        public DarkLightIconPair ReplicatorIconPair => m_Replicator;
        public DarkLightIconPair SyntheticObjectIconPair => m_SyntheticObject;
        public DarkLightIconPair MarkerIconsTrackingPair => m_MarkerIcons.Tracking;
        public DarkLightIconPair FaceIconsTrackingPair => m_FaceIcons.Tracking;

        public Dictionary<Type, ConditionIconData> ConditionIcons =>
            m_Icons ?? (m_Icons = new Dictionary<Type, ConditionIconData>
            {
                { typeof(ElevationRelation), m_ElevationIcons },
                { typeof(DistanceRelation), m_ProximityIcons },
                { typeof(PlaneSizeCondition), m_SizeIcons },
                { typeof(AngleAxisCondition), m_AngleIcons }
            });

        public Texture2D GetIconForGameObject(GameObject gameObject)
        {
            if (!gameObject.activeInHierarchy)
                return null;

            var realWorldObject = gameObject.GetComponent<Proxy>();
            var set = gameObject.GetComponent<ProxyGroup>();
            QueryState queryState;
            ClientIconData iconData;

            if (realWorldObject)
            {
                queryState = realWorldObject.queryState;
                iconData = gameObject.GetComponent<IsFaceCondition>() != null ? m_FaceIcons : m_ContextIcons;
            }
            else if (set)
            {
                queryState = set.queryState;
                iconData = m_SetIcons;
            }
            else
            {
                return null;
            }

            switch (queryState)
            {
                case QueryState.Unknown:
                case QueryState.Querying:
                case QueryState.Resuming:
                    return iconData.Seeking.Icon;
                case QueryState.Tracking:
                case QueryState.Acquiring:
                    return iconData.Tracking.Icon;
                default:
                    return iconData.Unavailable.Icon;
            }
        }
    }

    [Serializable]
    public struct DarkLightIconPair
    {
#pragma warning disable 649
        [SerializeField]
        Texture2D m_Dark;

        [SerializeField]
        Texture2D m_Light;
#pragma warning restore 649

        public Texture2D Dark => m_Dark;
        public Texture2D Light => m_Light;

        public Texture2D Icon => EditorGUIUtility.isProSkin ? m_Dark : m_Light;
    }

    [Serializable]
    public class ConditionIconData
    {
#pragma warning disable 649
        [SerializeField]
        DarkLightIconPair m_Inactive;

        [SerializeField]
        DarkLightIconPair m_Active;
#pragma warning restore 649


        public DarkLightIconPair Inactive => m_Inactive;
        public DarkLightIconPair Active => m_Active;
    }

    [Serializable]
    public class ClientIconData
    {
#pragma warning disable 649
        [SerializeField]
        DarkLightIconPair m_Seeking;

        [SerializeField]
        DarkLightIconPair m_Tracking;

        [SerializeField]
        DarkLightIconPair m_Unavailable;
#pragma warning restore 649

        public DarkLightIconPair Seeking => m_Seeking;
        public DarkLightIconPair Tracking => m_Tracking;
        public DarkLightIconPair Unavailable => m_Unavailable;
    }
}
