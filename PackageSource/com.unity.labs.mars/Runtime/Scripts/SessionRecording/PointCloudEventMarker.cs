using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Recording;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Unity.Labs.MARS
{
    class PointCloudEventMarker : Marker, INotification, INotificationOptionProvider
    {
        const NotificationFlags k_Flags = NotificationFlags.Retroactive | NotificationFlags.TriggerInEditMode;

        [SerializeField]
        List<SerializedPointCloudData> m_Data;

        readonly PropertyName m_ID = new PropertyName();

        public PropertyName id { get { return m_ID; } }

        public NotificationFlags flags { get { return k_Flags; } }

        public List<SerializedPointCloudData> Data
        {
            get { return m_Data; }
            set { m_Data = value; }
        }
    }
}
