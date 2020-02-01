﻿using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Labs.MARS.Query;
using Unity.Labs.MARS.Tests;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS.Data.Tests.QueryData
{
    public class LowLevelSetMemberRegistrationTests
    {
        ParallelSetMemberData m_MemberData;

        [OneTimeSetUp]
        public void Setup()
        {
            const int initialCapacity = 8;
            m_MemberData = new ParallelSetMemberData(initialCapacity);
        }

        [TearDown]
        public void TearDown()
        {
            TestUtils.DestroyDefaultTestContexts();
        }

        [SetUp]
        public void SetupBeforeEach()
        {
            m_MemberData.Clear();
        }

        [TestCaseSource(typeof(QueryArgsSource), nameof(QueryArgsSource.RegisterMemberCases))]
        public void Register(List<IMRObject> imrObjects, List<SetChildArgs> memberArgs)
        {
            var storageIndexToInputIndex= new Dictionary<int, int>();
            var countBefore = m_MemberData.Count;
            var queryMatchId = QueryMatchID.Generate();

            for (var i = 0; i < imrObjects.Count; i++)
            {
                var storedIndex = m_MemberData.Register(queryMatchId, imrObjects[i], memberArgs[i]);
                storageIndexToInputIndex.Add(storedIndex, i);
            }

            // make sure we increment the global counter
            Assert.AreEqual(countBefore + imrObjects.Count, m_MemberData.Count);

            List<int> indices;
            Assert.True(m_MemberData.MatchIdToIndex.TryGetValue(queryMatchId, out indices));

            foreach (var storageIndex in indices)
            {
                var inputIndex = storageIndexToInputIndex[storageIndex];
                var childArgs = memberArgs[inputIndex];
                var innerArgs = childArgs.tryBestMatchArgs;

                Assert.True(m_MemberData.ValidIndices.Contains(storageIndex));
                Assert.AreEqual(m_MemberData.Exclusivities[storageIndex], innerArgs.exclusivity);
                Assert.AreEqual(m_MemberData.Required[storageIndex], childArgs.required);
                Assert.AreEqual(m_MemberData.Conditions[storageIndex], innerArgs.conditions);
                // make sure we got all necessary intermediate / result containers from the pools when we registered
                Assert.NotNull(m_MemberData.CachedTraits[storageIndex]);
                Assert.NotNull(m_MemberData.ConditionRatings[storageIndex]);
                Assert.AreEqual(m_MemberData.TraitRequirements[storageIndex], childArgs.tryBestMatchArgs.traitRequirements);
                Assert.NotNull(m_MemberData.ConditionMatchSets[storageIndex]);
                Assert.NotNull(m_MemberData.ReducedConditionRatings[storageIndex]);
                Assert.NotNull(m_MemberData.QueryResults[storageIndex]);
                // make sure we initialize the best match id to an invalid id
                Assert.AreEqual(m_MemberData.BestMatchDataIds[storageIndex], -1);

                // this is a special case - we don't add anything to this unless it's registered as part of a Set.
                Assert.Null(m_MemberData.RelationMemberships[storageIndex]);
            }
        }

        // register 3, free 2 (which belong to the same set), register another, see if registration uses a freed index
        [Test]
        public void RegisteringUsesFreedIndexIfAvailable()
        {
            GameObject tempGameObject;
            var context = TestUtils.CreateDefaultTestContext(out tempGameObject);
            var args = TestUtils.DefaultSetChildArgs(context);
            m_MemberData.Register(QueryMatchID.Generate(), context, args);

            var queryMatchToFree = QueryMatchID.Generate();

            // the set to free from has two children, so we register two members with the same query match id
            m_MemberData.Register(queryMatchToFree, context, args);

            var member2Context = TestUtils.CreateDefaultTestContext(out tempGameObject, 2);
            m_MemberData.Register(queryMatchToFree, member2Context, TestUtils.DefaultSetChildArgs(member2Context));
            Assert.AreEqual(3, m_MemberData.Count);

            List<int> indicesBeforeRemoval;
            Assert.True(m_MemberData.MatchIdToIndex.TryGetValue(queryMatchToFree, out indicesBeforeRemoval));
            Assert.True(m_MemberData.Remove(queryMatchToFree));

            Assert.AreEqual(2, m_MemberData.FreedIndices.Count);
            Assert.AreEqual(1, m_MemberData.Count);

            // register another query, which should occupy one of the indices previously used by the removed one
            var queryMatchToReplace = QueryMatchID.Generate();
            m_MemberData.Register(queryMatchToReplace, context, args);
            Assert.AreEqual(2, m_MemberData.Count);
            Assert.AreEqual(1, m_MemberData.FreedIndices.Count);
        }

        [TestCaseSource(typeof(QueryArgsSource), nameof(QueryArgsSource.RegisterMemberCases))]
        public void Remove(List<IMRObject> imrObjects, List<SetChildArgs> memberArgs)
        {
            // all inputs to the test belong to the same set query / query match id
            var queryMatchId = QueryMatchID.Generate();
            for (var i = 0; i < imrObjects.Count; i++)
            {
                m_MemberData.Register(queryMatchId, imrObjects[i], memberArgs[i]);
            }

            List<int> storageIndices;
            Assert.True(m_MemberData.MatchIdToIndex.TryGetValue(queryMatchId, out storageIndices));
            // pretend this set query got a match, so we can confirm that the assignments get removed
            foreach (var index in storageIndices)
            {
                m_MemberData.BestMatchDataIds[index] = 2 + index;
            }

            var countBefore = m_MemberData.Count;
            var freedIndicesCountBefore = m_MemberData.FreedIndices.Count;
            Assert.True(m_MemberData.Remove(queryMatchId));

            Assert.AreEqual(countBefore - storageIndices.Count, m_MemberData.Count);
            Assert.AreEqual(freedIndicesCountBefore + storageIndices.Count, m_MemberData.FreedIndices.Count);
            Assert.False(m_MemberData.MatchIdToIndex.ContainsKey(queryMatchId));

            // removing a query match ID from the set member data should remove all indices it is at
            foreach (var index in storageIndices)
            {
                // make sure we added the indices we freed to the queue
                Assert.True(m_MemberData.FreedIndices.Contains(index));
                Assert.False(m_MemberData.ValidIndices.Contains(index));
                Assert.False(m_MemberData.AcquiringIndices.Contains(index));
                Assert.False(m_MemberData.UpdatingIndices.Contains(index));

                Assert.AreEqual(m_MemberData.Exclusivities[index], default(Exclusivity));
                Assert.Null(m_MemberData.Conditions[index]);
                Assert.Null(m_MemberData.TraitRequirements[index]);
                Assert.Null(m_MemberData.CachedTraits[index]);
                Assert.Null(m_MemberData.ConditionRatings[index]);
                Assert.Null(m_MemberData.ConditionMatchSets[index]);
                Assert.Null(m_MemberData.ReducedConditionRatings[index]);
                Assert.Null(m_MemberData.QueryResults[index]);
                Assert.AreEqual(m_MemberData.BestMatchDataIds[index], -1);
                Assert.False(m_MemberData.Required[index]);
            }
        }

        [Test]
        public void Clear()
        {
            const int queryCount = 3;
            // fill with some stub data so we can verify that clearing works
            for (var i = 0; i < queryCount; i++)
            {
                GameObject go;
                var context = TestUtils.CreateDefaultTestContext(out go, i);
                m_MemberData.Register(QueryMatchID.Generate(), context, TestUtils.DefaultSetChildArgs(context));
            }

            Assert.AreEqual(queryCount, m_MemberData.Count);

            m_MemberData.Clear();

            Assert.Zero(m_MemberData.Count);
            Assert.Zero(m_MemberData.MatchIdToIndex.Count);
            // make sure all the working index sets get cleared
            Assert.Zero(m_MemberData.AcquiringIndices.Count);
            Assert.Zero(m_MemberData.FilteredAcquiringIndices.Count);
            Assert.Zero(m_MemberData.PotentialMatchAcquiringIndices.Count);
            Assert.Zero(m_MemberData.DefiniteMatchAcquireIndices.Count);
            Assert.Zero(m_MemberData.UpdatingIndices.Count);
            // make sure all the actual query data gets cleared
            Assert.Zero(m_MemberData.QueryMatchIds.Count);
            Assert.Zero(m_MemberData.Conditions.Count);
            Assert.Zero(m_MemberData.TraitRequirements.Count);
            Assert.Zero(m_MemberData.CachedTraits.Count);
            Assert.Zero(m_MemberData.ConditionRatings.Count);
            Assert.Zero(m_MemberData.ConditionMatchSets.Count);
            Assert.Zero(m_MemberData.ReducedConditionRatings.Count);
            Assert.Zero(m_MemberData.BestMatchDataIds.Count);
            Assert.Zero(m_MemberData.QueryResults.Count);
        }

        public static class QueryArgsSource
        {
            // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
            static readonly List<GameObject> k_ToDestroy = new List<GameObject>();

            public static IEnumerable RegisterMemberCases
            {
                get
                {
                    k_ToDestroy.Clear();
                    foreach (Exclusivity exclusivity in Enum.GetValues(typeof(Exclusivity)))
                    {
                        var objectList = new List<IMRObject>();
                        var argsList = new List<SetChildArgs>();
                        for (var i = 0; i < 3; i++)
                        {
                            GameObject tempGameObject;
                            var context = TestUtils.CreateDefaultTestContext(out tempGameObject, i);
                            k_ToDestroy.Add(tempGameObject);
                            context.exclusivity = exclusivity;
                            objectList.Add(context);
                            argsList.Add(TestUtils.DefaultSetChildArgs(context));
                        }

                        yield return new TestCaseData(objectList, argsList);
                    }

                    foreach (var gameObject in k_ToDestroy)
                    {
                        UnityObjectUtils.Destroy(gameObject);
                    }
                }
            }
        }
    }
}
