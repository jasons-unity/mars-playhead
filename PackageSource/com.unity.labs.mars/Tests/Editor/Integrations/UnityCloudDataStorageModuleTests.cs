using NUnit.Framework;
using System.Collections;
using Unity.Labs.ModuleLoader;
using UnityEngine.TestTools;

namespace Unity.Labs.MARS.Tests
{
    public class UnityCloudDataStorageModuleTests
    {
        UnityCloudDataStorageModule m_Module;

        const string k_TypeName = "testType";
        const string k_Key1 = "001";
        const string k_Key2 = "002";
        const string k_Data1 = "abcdef";
        const string k_Data2 = "ghijk";

        bool m_SaveDataInCloudDone;
        bool m_SaveDataInCloudAndLoadItBackDone;

        // We have to do this since Assert.Ignore will not end the test and will only work from the main test and not from a callback
        bool m_SaveDataInCloudIgnoreResult;
        bool m_SaveDataInCloudAndLoadItBackIgnoreResult;

        [OneTimeSetUp]
        public void Setup()
        {
            m_Module = ModuleLoaderCore.instance.GetModule<UnityCloudDataStorageModule>();
        }

        [SetUp]
        public void BeforeEach()
        {
            m_Module.UnloadModule();
            m_Module =  ModuleLoaderCore.instance.GetModule<UnityCloudDataStorageModule>();
            m_Module.LoadModule();
        }

        [UnityTest]
        public IEnumerator SaveDataInCloud()
        {
            m_SaveDataInCloudDone = false;
            m_SaveDataInCloudIgnoreResult = false;

            if (m_Module.IsConnected() == false)
            {
                Assert.Ignore("Could not connect your project to the Unity Cloud");
            }

            m_Module.CloudSaveAsync(k_TypeName, k_Key1, k_Data1, SaveDataInCloud_WasSavedProperly);

            while (!m_SaveDataInCloudDone)
            {
                yield return null;
            }

            if (m_SaveDataInCloudIgnoreResult)
            {
                Assert.Ignore("Server or client issue");
            }
        }

        void SaveDataInCloud_WasSavedProperly(bool saveSuccess)
        {
            m_SaveDataInCloudDone = true;

            if (!saveSuccess)
            {
                m_SaveDataInCloudIgnoreResult = ShouldIgnore();
            }

            if (!m_SaveDataInCloudIgnoreResult)
            {
                Assert.True(saveSuccess);
            }
        }

        [UnityTest]
        public IEnumerator SaveDataInCloudAndLoadItBack()
        {
            m_SaveDataInCloudAndLoadItBackDone = false;
            m_SaveDataInCloudAndLoadItBackIgnoreResult = false;

            if (m_Module.IsConnected() == false)
            {
                Assert.Ignore("Could not connect your project to the Unity Cloud");
            }

            m_Module.CloudSaveAsync(k_TypeName, k_Key2, k_Data2, SaveDataInCloudAndLoadItBack_SaveDone);

            while (!m_SaveDataInCloudAndLoadItBackDone)
            {
                yield return null;
            }

            if (m_SaveDataInCloudIgnoreResult)
            {
                Assert.Ignore("Server or client issue");
            }
        }

        void SaveDataInCloudAndLoadItBack_SaveDone(bool saveSuccess)
        {
            if (!saveSuccess)
            {
                m_SaveDataInCloudAndLoadItBackDone = true;
                m_SaveDataInCloudAndLoadItBackIgnoreResult = ShouldIgnore();
                if (!m_SaveDataInCloudAndLoadItBackIgnoreResult)
                {
                    Assert.True(false);
                }
            }
            else
            {
                m_Module.CloudLoadAsync(k_TypeName, k_Key2, SaveDataInCloudAndLoadItBack_LoadDone);
            }
        }

        [Test]
        public void SetProjectId()
        {
            var previousId = m_Module.GetProjectIdentifier();
            var testId = "test";
            m_Module.SetProjectIdentifier(testId);
            Assert.AreEqual(testId, m_Module.GetProjectIdentifier());
            m_Module.SetProjectIdentifier(previousId);
        }

        [Test]
        public void SetAPIKey()
        {
            var previousKey = m_Module.GetAPIKey();
            var testKey = "test";
            m_Module.SetAPIKey(testKey);
            Assert.AreEqual(testKey, m_Module.GetAPIKey());
            m_Module.SetAPIKey(previousKey);
        }

        void SaveDataInCloudAndLoadItBack_LoadDone(bool loadSuccess, string dataLoaded)
        {
            m_SaveDataInCloudAndLoadItBackDone = true;

            if (loadSuccess)
            {
                Assert.AreEqual(k_Data2, dataLoaded);
            }
            else
            {
                m_SaveDataInCloudAndLoadItBackIgnoreResult = ShouldIgnore();
                if (!m_SaveDataInCloudAndLoadItBackIgnoreResult)
                {
                    Assert.True(false);
                }
            }
        }

        bool ShouldIgnore()
        {
            if (m_Module.GetLastStatusCode() == 429)
            {
                return true;
            }

            return false;
        }
    }
}
