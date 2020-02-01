using System.Collections.Generic;
using MARS.Tests.Editor;
using MARS.Tests.Editor.Database;
using NUnit.Framework;
using Unity.Labs.MARS.Query;
using Unity.Labs.Utils;
using UnityEngine;
using TinyRatingSet = Unity.Labs.MARS.Data.Tests.DatabaseTestData.TinyRatingSet;
using UnityObject = UnityEngine.Object;

namespace Unity.Labs.MARS.Data.Tests
{
    public class QueryPipelineTests
    {
        MARSDatabase m_Db;
        MARSQueryBackend m_QueryBackend;
        StandaloneQueryPipeline m_Pipeline;

        long m_DataID;

        GameObject m_GameObject;
        GameObject m_Child1GameObject;
        GameObject m_Child2GameObject;

        Conditions m_Conditions;
        ConditionRatingsData m_ConditionRatings;
        CachedTraitCollection m_TraitCache;

        readonly Dictionary<int, float> m_Ratings = new Dictionary<int, float>();

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly HashSet<int> k_Intersection = new HashSet<int>();

        static List<Dictionary<int, float>> CreateRatingsList(int length = 2)
        {
            var list = new List<Dictionary<int, float>>(length);
            list.Fill(length);
            return list;
        }

        void SetupObject(GameObject go)
        {
            var rwo = go.GetComponent<Proxy>();
            if(rwo == null)
                rwo = go.AddComponent<Proxy>();

            m_Conditions = Conditions.FromGenericIMRObject(rwo);
            m_ConditionRatings = new ConditionRatingsData(m_Conditions);
            m_TraitCache = new CachedTraitCollection(m_Conditions);
            m_Db.FindTraitCollections(m_Conditions, m_TraitCache);
        }

        [OneTimeSetUp]
        public void Setup()
        {
            QuerySimulationModule.TestMode = true;
            m_Db = new MARSDatabase();
            m_Db.LoadModule();

            m_Pipeline = new StandaloneQueryPipeline(m_Db);
            m_Pipeline.SetupData();

            // creating our own instance of the query backend doesn't work because
            // we'll get the error about multiple instance of scriptable settings.
            m_QueryBackend = MARSQueryBackend.instance;
            m_QueryBackend.Pipeline = m_Pipeline;
            m_QueryBackend.LoadModule();
        }

        [SetUp]
        public void SetupBeforeEach()
        {
            SlowTaskModule.instance.ClearTasks();

            m_GameObject = new GameObject("pipeline test condition source");
            SetupObject(m_GameObject);

            m_Child1GameObject = new GameObject("pipeline test member 1");
            m_Child1GameObject.AddComponent<Proxy>();
            m_Child2GameObject = new GameObject("pipeline test member 2");
            m_Child2GameObject.AddComponent<Proxy>();

            Conditions.FromGameObject<Proxy>(m_Child1GameObject);
            Conditions.FromGameObject<Proxy>(m_Child2GameObject);

            m_Ratings.Clear();

            m_Db.Clear();
            k_Intersection.Clear();
        }

        [OneTimeTearDown]
        public void AfterAll()
        {
            SlowTaskModule.instance.ClearTasks();
            m_Db.UnloadModule();
            m_QueryBackend.UnloadModule();

            QuerySimulationModule.TestMode = false;
        }

        [TearDown]
        public void TearDownBeforeEach()
        {
            UnityObject.DestroyImmediate(m_GameObject);
            UnityObject.DestroyImmediate(m_Child1GameObject);
            UnityObject.DestroyImmediate(m_Child2GameObject);
        }

        [Test]
        public void AllStageDataIsWiredOnModuleLoad()
        {
            // we call LoadModule() in setup, so setup should be complete.
            AssertUtils.DataWired(m_Pipeline.CacheTraitReferencesStage.Transformation);
            AssertUtils.DataWired(m_Pipeline.ConditionRatingStage.Transformation);
            AssertUtils.DataWired(m_Pipeline.FindMatchProposalsStage.Transformation);
            AssertUtils.DataWired(m_Pipeline.DataAvailabilityStage.Transformation);
            AssertUtils.DataWired(m_Pipeline.MatchReductionStage.Transformation);
            AssertUtils.DataWired(m_Pipeline.ResultFillStage.Transformation);
        }

        [Test]
        public void CheckTimeouts_UtilityFunction()
        {
            var indices = new List<int>{0, 1, 3};
            // the first should be ignored (negative means no timeout)
            // the second should not time out, as it has more time remaining
            // the third should be skipped over because it is not in the indices
            // the fourth one should time out
            var timeouts = new List<float>{-1f, 5f, 0f, 0.5f};
            const float timeSinceLastCycle = 1f;
            var timeoutIndices = new HashSet<int>();

            StandaloneQueryPipeline.CheckTimeouts(indices, timeouts, timeSinceLastCycle, timeoutIndices);

            // only the one that has not timed out should have its value modified.
            // the one that has timed out is about to be removed, so we do not update its value
            var expectedNewTimeouts = new List<float>{-1f, 5f - timeSinceLastCycle, 0f, 0.5f};
            AssertUtils.DeepEqual(timeouts, expectedNewTimeouts);
        }

        [Test]
        public void HandleTimeouts_RemovesTimedOutIndexes()
        {
            const float sharedTimeout = 1.5f;
            var standaloneArgs = m_GameObject.GetQueryArgs();
            m_QueryBackend.RegisterQuery(standaloneArgs);

            var standaloneTimeoutArgs = m_GameObject.GetQueryArgs();
            standaloneTimeoutArgs.commonQueryData.timeOut = sharedTimeout;
            standaloneTimeoutArgs.commonQueryData.overrideTimeout = true;
            var standaloneTimeoutMatchId = m_QueryBackend.RegisterQuery(standaloneTimeoutArgs);

            Assert.AreEqual(0, m_Pipeline.Data.Count);

            m_QueryBackend.SyncQueryBuffers();        // actually add the registered queries
            m_QueryBackend.OnMarsUpdate();

            Assert.AreEqual(2, m_QueryBackend.Pipeline.Data.Count);

            m_Pipeline.CycleDeltaTime = 1f;
            m_Pipeline.HandleTimeouts();        // nothing should time out this time

            Assert.AreEqual(2, m_QueryBackend.Pipeline.Data.Count);

            // after we advance time by the cycle delta, both standalone & relation queries should have a timeout
            m_Pipeline.HandleTimeouts();
            // queries not actually removed until buffers synced - timing them out adds them to the remove list
            m_QueryBackend.SyncQueryBuffers();

            Assert.AreEqual(0, m_Pipeline.Data.Count);
            Assert.False(m_Pipeline.Data.matchIdToIndex.ContainsKey(standaloneTimeoutMatchId));
        }

        [Test]
        public void MatchRatingDataTransform_SingleEntry()
        {
            DatabaseTestData.PopulateDatabase(m_Db, 3);
            m_GameObject.AddTestFloatCondition();
            m_GameObject.AddTestVector2Condition(0.9f);
            SetupObject(m_GameObject);

            // TODO - better test for this stage now that it filters indices
            MatchRatingDataTransform.RateConditionMatches(m_Conditions, m_TraitCache, m_ConditionRatings);
            Assert.AreEqual(7, m_ConditionRatings[typeof(float)][0].Count);
            Assert.AreEqual(7, m_ConditionRatings[typeof(Vector2)][0].Count);
        }

        [Test]
        public void MatchRatingDataTransform_NonWorkingIndicesNotProcessed()
        {
            DatabaseTestData.PopulateDatabase(m_Db, 3);
            m_GameObject.AddTestFloatCondition();
            m_GameObject.AddTestVector2Condition(0.9f);
            SetupObject(m_GameObject);

            var dataTransform = new MatchRatingDataTransform
            {
                WorkingIndices = new List<int> {1},
                Input1 = new List<Conditions> {m_Conditions, m_Conditions},
                Input2 = new List<CachedTraitCollection> { m_TraitCache, m_TraitCache },
                Output = new List<ConditionRatingsData> {m_ConditionRatings, new ConditionRatingsData(m_Conditions)},
            };

            dataTransform.Complete();
            Assert.Zero(dataTransform.Output[0][typeof(float)][0].Count);          // index 0 wasn't processed, so no matches
            Assert.Zero(dataTransform.Output[0][typeof(Vector2)][0].Count);
            // index 1 was processed, so has matches.  That we expect 7 is calculated by hand from the test data.
            Assert.AreEqual(7, dataTransform.Output[1][typeof(float)][0].Count);
            Assert.AreEqual(7, dataTransform.Output[1][typeof(Vector2)][0].Count);
        }

        [Test]
        public void FindMatchProposalsTransform_FindsSingleQueryIntersection()
        {
            var ratings = TinyRatingSet.Setup();
            FindMatchProposalsTransform.Execute(ratings, k_Intersection);
            Assert.True(k_Intersection.SetEquals(TinyRatingSet.expectedMatchSet));
        }

        [Test]
        public void FindMatchProposalsTransform_SkipsNonWorkingIndices()
        {
            var ratings = TinyRatingSet.Setup();
            // this transform should only process index 1
            var dataTransform = new FindMatchProposalsTransform()
            {
                WorkingIndices = new List<int> {1},
                Input1 = new List<int>(),
                Input2 = new List<ConditionRatingsData> {ratings, ratings},
                Output = new List<HashSet<int>> { new HashSet<int>(), new HashSet<int>() }
            };

            dataTransform.Complete();
            Assert.Zero(dataTransform.Output[0].Count);          // index 0 wasn't processed, so no matches
            Assert.Greater(dataTransform.Output[1].Count, 0);          // index 1 was processed, so has matches
        }

        [Test]
        public void MatchReductionTransform_SingleEntry()
        {
            var ratings = TinyRatingSet.Setup();
            MatchReductionTransform.Execute(ratings, TinyRatingSet.expectedMatchSet, m_Ratings);

            Assert.AreEqual(TinyRatingSet.expectedMatchSet.Count, m_Ratings.Count);
            AssertUtils.MatchRatingsValid(TinyRatingSet.expectedMatchSet, m_Ratings);
            AssertUtils.MatchRatingsSorted(m_Ratings);
        }

        [Test]
        public void MatchReductionTransform_AllEntries()
        {
            var ratings = TinyRatingSet.Setup();
            var dataTransform = new MatchReductionTransform()
            {
                WorkingIndices = new List<int> {1},
                Input1 = new List<ConditionRatingsData> {ratings, ratings},
                Input2 = new List<HashSet<int>> { TinyRatingSet.expectedMatchSet, TinyRatingSet.expectedMatchSet },
                Output = CreateRatingsList()
            };

            dataTransform.Complete();
            Assert.Zero(dataTransform.Output[0].Count);          // index 0 wasn't processed, so no matches
            Assert.AreEqual(dataTransform.Output[1].Count, TinyRatingSet.expectedMatchSet.Count);
        }

        [TestCaseSource(typeof(MatchConflictTestData), nameof(MatchConflictTestData.CaseOneVariations))]
        [TestCaseSource(typeof(MatchConflictTestData), nameof(MatchConflictTestData.CaseTwoVariations))]
        [TestCaseSource(typeof(MatchConflictTestData), nameof(MatchConflictTestData.SingleReservedConflictCases))]
        [TestCaseSource(typeof(MatchConflictTestData), nameof(MatchConflictTestData.CaseFourVariations))]
        public void FindBestStandaloneMatches_ResolveConflicts(MatchConflictTestInput input)
        {
            var ratings = input.ratings;
            var dataTransform = new FindBestMatchTransform(input.ratings.Count);
            var idRequests = dataTransform.BuildRequestMap(input.workingIndices, ratings, input.exclusivities);

            var assignments = dataTransform.ResolveConflicts(idRequests, ratings);

            // uncomment to debug this tests's results
            //assignments.DebugLogBlock();
            AssertUtils.DeepEqual(input.expectedAssignments, assignments);
        }

        [Test]
        public void ResolveStandaloneQueryConflicts_LargerTestCase()
        {
            FindBestStandaloneMatches_ResolveConflicts(MatchConflictTestData.MultipleConflictMediumCase());
        }
    }
}
