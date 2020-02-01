using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [ScriptableSettingsPath(MARSCore.UserSettingsFolder)]
    public class SessionRecordingSettings : ScriptableSettings<SessionRecordingSettings>
    {
        [SerializeField]
        [Tooltip("The interval on which we record the camera pose, in seconds")]
        float m_CameraPoseInterval = 0.0334f;

        public float CameraPoseInterval { get { return m_CameraPoseInterval; } }
    }
}
