using NUnit.Framework;
using System;
using System.Collections;
using Unity.Labs.MARS.Query;
using Unity.Labs.MARS.Tests;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Unity.Labs.MARS.Data.Tests.QueryData
{
    public class LowLevelStandaloneContextRegistrationTests
    {
        ParallelQueryData m_Data;

        [OneTimeSetUp]
        public void Setup()
        {
            const int initialCapacity = 8;
            m_Data = new ParallelQueryData(initialCapacity);
        }

        [TearDown]
        public void TearDown()
        {
            TestUtils.DestroyDefaultTestContexts();
        }

        [SetUp]
        public void SetupBeforeEach()
        {
            m_Data.Clear();
        }

        [TestCaseSource(typeof(QueryArgsSource), nameof(QueryArgsSource.RegisterCases))]
        public void Register(QueryArgs args)
        {
            var queryMatchId = QueryMatchID.Generate();

            var countBefore = m_Data.Count;
            m_Data.Register(queryMatchId, args);

            // make sure we increment the global counter
            Assert.AreEqual(countBefore + 1, m_Data.Count);

            int index;
            Assert.True(m_Data.matchIdToIndex.TryGetValue(queryMatchId, out index));
            Assert.True(m_Data.ValidIndices.Contains(index));

            Assert.AreEqual(m_Data.queryArgs[index], args);
            Assert.AreEqual(m_Data.exclusivities[index], args.exclusivity);
            Assert.AreEqual(m_Data.updateMatchInterval[index], args.commonQueryData.updateMatchInterval);
            Assert.AreEqual(m_Data.timeOuts[index], args.commonQueryData.timeOut);
            Assert.AreEqual(m_Data.reAcquireOnLoss[index], args.commonQueryData.reacquireOnLoss);
            Assert.AreEqual(m_Data.conditions[index], args.conditions);
            Assert.AreEqual(m_Data.traitRequirements[index], args.traitRequirements);
            Assert.AreEqual(m_Data.acquireHandlers[index], args.onAcquire);
            Assert.AreEqual(m_Data.updateHandlers[index], args.onMatchUpdate);
            Assert.AreEqual(m_Data.lossHandlers[index], args.onLoss);
            Assert.AreEqual(m_Data.timeoutHandlers[index], args.onTimeout);
            // make sure we got all necessary intermediate / result containers from the pools when we registered
            Assert.NotNull(m_Data.cachedTraits[index]);
            Assert.NotNull(m_Data.conditionRatings[index]);
            Assert.NotNull(m_Data.conditionMatchSets[index]);
            Assert.NotNull(m_Data.reducedConditionRatings[index]);
            Assert.NotNull(m_Data.queryResults[index]);
            // make sure we initialize the best match id to an invalid id
            Assert.AreEqual(m_Data.bestMatchDataIds[index], -1);
        }

        // register 3, free 1, register another and see if the last one uses the freed
        [Test]
        public void RegisteringUsesFreedIndexIfAvailable()
        {
            GameObject tempGameObject;
            var context = TestUtils.CreateDefaultTestContext(out tempGameObject);

            var args = GetTestQueryArgs(context);
            m_Data.Register(QueryMatchID.Generate(), args);
            var queryMatchToFree = QueryMatchID.Generate();
            m_Data.Register(queryMatchToFree, args);

            int indexToFree;
            Assert.True(m_Data.matchIdToIndex.TryGetValue(queryMatchToFree, out indexToFree));
            m_Data.Register(QueryMatchID.Generate(), args);
            m_Data.Remove(queryMatchToFree);

            // register another query, which should occupy the index previously used by the removed one
            Assert.AreEqual(1, m_Data.FreedIndices.Count);
            Assert.AreEqual(2, m_Data.Count);
            var queryMatchToReplace = QueryMatchID.Generate();
            m_Data.Register(queryMatchToReplace, args);
            Assert.AreEqual(3, m_Data.Count);
            Assert.AreEqual(0, m_Data.FreedIndices.Count);

            int indexOfReplacement;
            Assert.True(m_Data.matchIdToIndex.TryGetValue(queryMatchToReplace, out indexOfReplacement));
            Assert.AreEqual(indexToFree, indexOfReplacement);
        }

        [TestCaseSource(typeof(QueryArgsSource), nameof(QueryArgsSource.RegisterCases))]
        public void Remove(QueryArgs args)
        {
            var queryMatchId = QueryMatchID.Generate();
            m_Data.Register(queryMatchId, args);

            int index;
            Assert.True(m_Data.matchIdToIndex.TryGetValue(queryMatchId, out index));
            // pretend this query got a match, so we can confirm that this number is removed
            m_Data.bestMatchDataIds[index] = 2;

            var countBefore = m_Data.Count;
            var freedIndicesCountBefore = m_Data.FreedIndices.Count;
            Assert.True(m_Data.Remove(queryMatchId));

            // make sure we decrement the global counter
            Assert.AreEqual(countBefore - 1, m_Data.Count);
            // make sure we added the index we free to the queue
            Assert.AreEqual(freedIndicesCountBefore + 1, m_Data.FreedIndices.Count);
            Assert.True(m_Data.FreedIndices.Contains(index));

            Assert.False(m_Data.ValidIndices.Contains(index));
            Assert.False(m_Data.matchIdToIndex.ContainsKey(queryMatchId));

            Assert.AreEqual(m_Data.exclusivities[index], default(Exclusivity));
            Assert.AreEqual(m_Data.updateMatchInterval[index], default(float));
            Assert.AreEqual(m_Data.timeOuts[index], default(float));
            Assert.AreEqual(m_Data.reAcquireOnLoss[index], default(bool));
            Assert.Null(m_Data.queryArgs[index]);
            Assert.Null(m_Data.conditions[index]);
            Assert.Null(m_Data.traitRequirements[index]);
            Assert.Null(m_Data.acquireHandlers[index]);
            Assert.Null(m_Data.updateHandlers[index]);
            Assert.Null(m_Data.lossHandlers[index]);
            Assert.Null(m_Data.timeoutHandlers[index]);
            Assert.Null(m_Data.cachedTraits[index]);
            Assert.Null(m_Data.conditionRatings[index]);
            Assert.Null(m_Data.conditionMatchSets[index]);
            Assert.Null(m_Data.reducedConditionRatings[index]);
            Assert.Null(m_Data.queryResults[index]);
            Assert.AreEqual(m_Data.bestMatchDataIds[index], -1);
        }

        [Test]
        public void Clear()
        {
            const int queryCount = 3;
            // fill with some stub data so we can verify that clearing works
            for (var i = 0; i < queryCount; i++)
            {
                var context = TestUtils.CreateDefaultTestContext(out GameObject go, i);
                m_Data.Register(QueryMatchID.Generate(), GetTestQueryArgs(context));
                UnityObject.DestroyImmediate(go);
            }

            Assert.AreEqual(queryCount, m_Data.Count);

            m_Data.Clear();

            Assert.Zero(m_Data.Count);
            Assert.Zero(m_Data.matchIdToIndex.Count);
            // make sure all the working index sets get cleared
            Assert.Zero(m_Data.acquiringIndices.Count);
            Assert.Zero(m_Data.filteredAcquiringIndices.Count);
            Assert.Zero(m_Data.potentialMatchAcquiringIndices.Count);
            Assert.Zero(m_Data.definiteMatchAcquireIndices.Count);
            Assert.Zero(m_Data.updatingIndices.Count);
            // make sure all the actual query data gets cleared
            Assert.Zero(m_Data.queryMatchIds.Count);
            Assert.Zero(m_Data.queryArgs.Count);
            Assert.Zero(m_Data.conditions.Count);
            Assert.Zero(m_Data.traitRequirements.Count);
            Assert.Zero(m_Data.cachedTraits.Count);
            Assert.Zero(m_Data.conditionRatings.Count);
            Assert.Zero(m_Data.conditionMatchSets.Count);
            Assert.Zero(m_Data.reducedConditionRatings.Count);
            Assert.Zero(m_Data.bestMatchDataIds.Count);
            Assert.Zero(m_Data.queryResults.Count);
            Assert.Zero(m_Data.reAcquireOnLoss.Count);
            Assert.Zero(m_Data.timeOuts.Count);
            Assert.Zero(m_Data.updateMatchInterval.Count);
            // event handlers all get removed ?
            Assert.Zero(m_Data.acquireHandlers.Count);
            Assert.Zero(m_Data.updateHandlers.Count);
            Assert.Zero(m_Data.lossHandlers.Count);
            Assert.Zero(m_Data.timeoutHandlers.Count);
        }

        static QueryArgs GetTestQueryArgs(Proxy context, Exclusivity exclusivity = Exclusivity.ReadOnly)
        {
            return new QueryArgs
            {
                commonQueryData = new CommonQueryData
                {
                    timeOut = 20f,
                    overrideTimeout = false,
                    reacquireOnLoss = true,
                    updateMatchInterval = 0.1f
                },
                conditions = new Conditions(context),
                exclusivity = exclusivity,
                onAcquire = (result) => { },
                onMatchUpdate = (result) => { },
                onLoss = (result) => { },
                onTimeout = (queryArgs) => { }
            };
        }

        static class QueryArgsSource
        {
            public static IEnumerable RegisterCases
            {
                get
                {
                    var go = new GameObject("query registration test", typeof(Proxy));
                    go.AddTestFloatCondition();
                    go.AddTestVector2Condition();
                    var realWorldObject = go.GetComponent<Proxy>();

                    foreach (Exclusivity exclusivity in Enum.GetValues(typeof(Exclusivity)))
                    {
                        var args = GetTestQueryArgs(realWorldObject, exclusivity);
                        yield return new TestCaseData(args);
                    }

                    UnityObject.DestroyImmediate(go);
                }
            }
        }
    }
}
