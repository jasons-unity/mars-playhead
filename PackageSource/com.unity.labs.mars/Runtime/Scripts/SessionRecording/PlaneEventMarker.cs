using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Unity.Labs.MARS
{
    public class PlaneEventMarker : Marker, INotification, INotificationOptionProvider
    {
        const NotificationFlags k_Flags = NotificationFlags.Retroactive | NotificationFlags.TriggerInEditMode;

        [SerializeField]
        MRPlane m_Plane;

        [SerializeField]
        PlaneEventType m_EventType;

        readonly PropertyName m_ID = new PropertyName();

        public PropertyName id { get { return m_ID; } }

        public NotificationFlags flags { get { return k_Flags; } }

        public MRPlane Plane
        {
            get { return m_Plane; }
            set { m_Plane = value; }
        }

        public PlaneEventType EventType
        {
            get { return m_EventType; }
            set { m_EventType = value; }
        }
    }
}
