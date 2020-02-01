#if UNITY_EDITOR
using UnityEngine;

namespace Unity.Labs.MARS.Data.Tests
{
    [AddComponentMenu("")]
    public class AddTraitsTest : DatabasePerformanceTest
    {
        protected MARSTraitDataProvider<float> m_Provider;

        public void OnEnable()
        {
            ConnectDb();
            m_Db.GetTraitProvider(out m_Provider);
            m_StartFrame = Time.frameCount;

            for (int i = 0; i < s_DataCount; i++)
                m_TraitNames[i] = "trait" + i;
        }

        public void Update()
        {
            for (int i = 0; i < s_DataCount; i++)
                m_Provider.AddOrUpdateTrait(i, m_TraitNames[i], i);
        }
    }
}
#endif
