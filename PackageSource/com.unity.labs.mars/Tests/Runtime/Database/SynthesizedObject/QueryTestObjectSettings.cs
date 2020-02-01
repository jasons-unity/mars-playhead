using UnityEngine;
using Unity.Labs.Utils;

namespace Unity.Labs.MARS.Tests
{
    /// <summary>
    /// Holds references to pre-configured game objects for use in the runtime query system tests
    /// </summary>
    [ScriptableSettingsPath("MARS/Tests/Runtime/Database/Query Objects")]
    public class QueryTestObjectSettings : ScriptableSettings<QueryTestObjectSettings>
    {
#pragma warning disable 649
        [SerializeField]
        GameObject m_NonRequiredChildrenSet;
#pragma warning restore 649

        public GameObject NonRequiredChildrenSet { get { return m_NonRequiredChildrenSet; } }
    }
}
