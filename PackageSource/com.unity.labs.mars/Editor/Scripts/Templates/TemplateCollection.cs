using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [CreateAssetMenu(fileName = "Templates", menuName = "MARS/Template Collection")]
    public class TemplateCollection : ScriptableObject
    {
#pragma warning disable 649
        [SerializeField]
        TemplateData[] m_Templates;
#pragma warning restore 649

        public TemplateData[] templates { get { return m_Templates; } }
    }

    [Serializable]
    public struct TemplateData
    {
        public string name;
        public SceneAsset scene;
        public Texture2D icon;
        public EnvironmentMode environmentMode;
    }
}
