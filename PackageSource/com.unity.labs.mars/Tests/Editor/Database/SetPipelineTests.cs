using System.Collections.Generic;
using MARS.Tests.Editor;
using NUnit.Framework;
using Unity.Labs.MARS.Query;
using Unity.Labs.MARS.Tests;
using UnityEngine;

namespace Unity.Labs.MARS.Data.Tests.QueryData
{
    public class SetPipelineTests : ScriptableObject
    {
        // This should be set by default references
        [SerializeField]
#pragma warning disable 649
        GameObject m_SetQueryTestObject;
#pragma warning restore 649

        MARSDatabase m_Db;
        SetQueryPipeline m_Pipeline;

        [OneTimeSetUp]
        public void Setup()
        {
            m_Db = new MARSDatabase();
            m_Db.LoadModule();
            m_Pipeline = new SetQueryPipeline(m_Db);
        }

        [SetUp]
        public void SetupBeforeEach()
        {
            m_Db.Clear();
        }

        [OneTimeTearDown]
        public void AfterAll()
        {
            m_Db.UnloadModule();
        }

        [Test]
        // this is the same test as in the standalone data, because set members share many stages
        public void AllSetMemberDataIsWiredOnModuleLoad()
        {
            // we call LoadModule() in setup, so setup should be complete.
            AssertUtils.DataWired(m_Pipeline.MemberTraitCacheStage.Transformation);
            AssertUtils.DataWired(m_Pipeline.MemberConditionRatingStage.Transformation);
            AssertUtils.DataWired(m_Pipeline.MemberMatchIntersectionStage.Transformation);
            AssertUtils.DataWired(m_Pipeline.MemberDataAvailabilityStage.Transformation);
            AssertUtils.DataWired(m_Pipeline.MemberMatchReductionStage.Transformation);
            AssertUtils.DataWired(m_Pipeline.MemberResultFillStage.Transformation);
        }

        [Test]
        // Test that when we insert a set's arguments, we insert one entry into the set data,
        // one entry per member into the member data, and generate an index mapping between them.
        // Since we have the lower-level query data tests, this mostly tests the mapping between sets & members
        public void RegisterSetData()
        {
            var relations = TestUtils.GetRelations(m_SetQueryTestObject);
            var args = TestUtils.DefaultSetArgs(relations);
            var queryMatchId = QueryMatchID.Generate();
            m_Pipeline.Register(queryMatchId, args);

            Assert.True(m_Pipeline.Data.MatchIdToIndex.TryGetValue(queryMatchId, out var setIndex));

            // was the set order weight calculated upon registration?
            Assert.Greater(m_Pipeline.Data.OrderWeights[setIndex], 0f);

            // one entry in the member indices for every member ?
            var memberIndices = m_Pipeline.Data.MemberIndices[setIndex];
            Assert.AreEqual(relations.children.Count, memberIndices.Length);

            foreach (var memberIndex in memberIndices)
            {
                // did all member objects get correctly inserted to the member data ?
                var memberObject = m_Pipeline.MemberData.ObjectReferences[memberIndex];
                Assert.Contains(memberObject, relations.children.Keys);
                // is the member index within the bounds of the member data?
                Assert.Less(memberIndex, m_Pipeline.MemberData.Count);

                var memberships = m_Pipeline.MemberData.RelationMemberships[memberIndex];
                // it's OK for the memberships entry to be null - that means this set member is not in any Relations
                if (memberships == null)
                    continue;

                foreach (var membership in memberships)
                {
                    Assert.LessOrEqual(membership.RelationIndex, relations.Count);
                }
            }

            // one entry in the relation index pairs indices for each relation ?
            var relationIndexPairs = m_Pipeline.Data.RelationIndexPairs[setIndex];
            Assert.AreEqual(relations.Count, relationIndexPairs.Length);

            foreach (var pair in relationIndexPairs)
            {
                Assert.AreNotEqual(pair.Child1, pair.Child2);
                // every member of these pairs should belong to our member indices
                Assert.Contains(pair.Child1, memberIndices);
                Assert.Contains(pair.Child2, memberIndices);
            }

            var localRelationIndexPairs = m_Pipeline.Data.LocalRelationIndexPairs[setIndex];
            Assert.AreEqual(relations.Count, localRelationIndexPairs.Length);
            foreach (var pair in localRelationIndexPairs)
            {
                Assert.AreNotEqual(pair.Child1, pair.Child2);
                // every member of these pairs should be an index less than the count of members this set has
                Assert.Less(pair.Child1, memberIndices.Length);
                Assert.Less(pair.Child2, memberIndices.Length);
            }

            // check that SearchData got initialized
            var searchData = m_Pipeline.Data.SearchData[setIndex];
            Assert.NotNull(searchData);
            Assert.AreEqual(searchData.MatchBuffer.SetSize, memberIndices.Length);
            Assert.AreEqual(searchData.MemberRatings.Length, memberIndices.Length);
        }

        [Test]
        public void MapGlobalToLocalMemberIndices()
        {
            var globalIndices = new [] {2, 3, 4};
            var globalRelationPairs = new []
            {
                new RelationDataPair(2, 3), new RelationDataPair(4, 2), new RelationDataPair(3, 4),
            };
            var expectedLocalPairs = new []
            {
                new RelationDataPair(0, 1), new RelationDataPair(2, 0), new RelationDataPair(1, 2),
            };

            const int pairCount = 3;
            var actualLocalPairs = new RelationDataPair[pairCount];
            ParallelSetData.MapGlobalToLocalRelationPairs(globalIndices, globalRelationPairs, actualLocalPairs);

            for (var i = 0; i < pairCount; i++)
            {
                Assert.AreEqual(expectedLocalPairs[i], actualLocalPairs[i]);
            }
        }

        [TestCase(0)]
        [TestCase(2)]
        public void GetSetOrderWeightTest(int relationCount)
        {
            var weights = new SetOrderWeights(0.2f, 0.5f, 1f);
            var memberIndices = new [] { 0, 1, 2, 3 };
            var exclusivities = new List<Exclusivity>
            {
                // the readonly member is here to test that we don't add any weight for read only members
                Exclusivity.Reserved, Exclusivity.Reserved, Exclusivity.Shared, Exclusivity.ReadOnly
            };

            var weight = SetQueryPipeline.GetSetOrderWeight
                (memberIndices, relationCount, exclusivities, weights);

            var expected = relationCount * weights.RelationWeight +
                2 * weights.ReservedMemberWeight +
                weights.SharedMemberWeight;

            Assert.AreEqual(expected, weight);
        }
    }
}
