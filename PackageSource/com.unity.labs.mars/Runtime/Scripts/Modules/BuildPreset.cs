#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [CreateAssetMenu(fileName = "BuildPreset", menuName = "MARS/BuildPreset")]
    public class BuildPreset : ScriptableObject
    {
        [SerializeField]
        UIOrientation m_DefaultOrientation = UIOrientation.AutoRotation;

        [SerializeField]
        bool m_Portrait = true;

        [SerializeField]
        bool m_PortraitUpsideDown = true;

        [SerializeField]
        bool m_LandscapeRight = true;

        [SerializeField]
        bool m_LandscapeLeft = true;

        [SerializeField]
        bool m_ARCoreEnabled = true;

        public UIOrientation defaultInterfaceOrientation
        {
            get { return m_DefaultOrientation; }
        }

        public bool allowedAutorotateToPortrait
        {
            get { return m_Portrait; }
        }

        public bool allowedAutorotateToLandscapeLeft
        {
            get { return m_PortraitUpsideDown; }
        }

        public bool allowedAutorotateToLandscapeRight
        {
            get { return m_LandscapeRight; }
        }

        public bool allowedAutorotateToPortraitUpsideDown
        {
            get { return m_LandscapeLeft; }
        }

        public bool ARCoreEnabled
        {
            get { return m_ARCoreEnabled; }
        }
    }
}
#endif
