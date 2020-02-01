using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Labs.MARS.Query;
using Unity.Labs.MARS.Tests;
using UnityEngine;

namespace Unity.Labs.MARS.Data.Tests
{
    public class MultiTraitMatchRatingTests
    {
        const int k_DefaultRatingsCapacity = 8;
        static NativeConditionRatings s_Ratings;
        static int s_ConditionIndex;

        [TearDown]
        public void AfterEach()
        {
            s_Ratings.Dispose();
            s_ConditionIndex = 0;
        }

        [Test]
        public void NativeConditionRatings_AddSucceeds_IfGreaterThanZero()
        {
            s_Ratings = new NativeConditionRatings(1, k_DefaultRatingsCapacity);
            Assert.True(s_Ratings.Add(0.0001f));
            Assert.True(s_Ratings.Add(1f));
            Assert.AreEqual(2, s_Ratings.CurrentDataIdOffset);
        }

        [Test]
        public void NativeConditionRatings_AddFails_IfZeroOrLess()
        {
            s_Ratings = new NativeConditionRatings(1, k_DefaultRatingsCapacity);
            Assert.False(s_Ratings.Add(0f));
            Assert.False(s_Ratings.Add(-1f));
            // add a failed rating should just increment this counter
            Assert.AreEqual(2, s_Ratings.CurrentDataIdOffset);
        }

        [Test]
        public void NativeConditionRatings_SetDataIds()
        {
            var idIntersection = new HashSet<int>(Data.ExpectedIntFloatIntersection);
            s_Ratings = new NativeConditionRatings(1, idIntersection.Count);
            s_Ratings.SetDataIds(idIntersection);

            foreach (var id in s_Ratings.DataIds)
            {
                Assert.True(idIntersection.Contains(id));
            }
        }

        [Test]
        public void RateConditionWithTwoTypes_Success()
        {
            // fake the output of the previous step, finding the intersection of the two trait's IDs
            var idIntersection = new HashSet<int>(Data.ExpectedIntFloatIntersection);
            s_Ratings = new NativeConditionRatings(1, idIntersection.Count);
            var condition = new TestTwoTypeCondition();

            var success = MatchRatingDataTransform.RateMatches(condition,
                Data.IntTraitValues, Data.FloatTraitValues, idIntersection, ref s_Ratings, ref s_ConditionIndex);

            Assert.True(success);
            Assert.AreEqual(1, s_ConditionIndex);
            AssertValidRatingCount(ref s_Ratings, Data.ExpectedIntFloatConditionPassed.Length);
        }

        [Test]
        public void RateConditionWithTwoTypes_Failure()
        {
            var idIntersection = new HashSet<int>(Data.FailingIntFloatIntersection);
            s_Ratings = new NativeConditionRatings(1, idIntersection.Count);
            var condition = new TestTwoTypeCondition();

            var success = MatchRatingDataTransform.RateMatches(condition,
                Data.IntTraitValues, Data.FloatTraitValues, idIntersection, ref s_Ratings, ref s_ConditionIndex);

            Assert.False(success);
            Assert.AreEqual(1, s_ConditionIndex);
            AssertValidRatingCount(ref s_Ratings, 0);
        }

        [Test]
        public void RateConditionWithThreeTypes_Success()
        {
            // id 3 should fail the test condition, since it has a value of 0
            var traitIds = new[] { 3, 6, 8 };
            var expectedAfterConditionMatching = new[] { 6, 8 };
            var idIntersection = new HashSet<int>(traitIds);
            s_Ratings = new NativeConditionRatings(1, idIntersection.Count);
            var condition = new TestThreeTypeCondition();

            var success = MatchRatingDataTransform.RateMatches(condition,
                Data.IntTraitValues, Data.FloatTraitValues, Data.IntTraitValues2,
                idIntersection, ref s_Ratings, ref s_ConditionIndex);

            Assert.True(success);
            // we expect rating functions to increment their condition index
            Assert.AreEqual(1, s_ConditionIndex);
            AssertValidRatingCount(ref s_Ratings, expectedAfterConditionMatching.Length);
        }

        [Test]
        public void RateConditionWithThreeTypes_Failure()
        {
            var idIntersection = new HashSet<int>(Data.FailingIntFloatIntersection);
            s_Ratings = new NativeConditionRatings(1, idIntersection.Count);
            var condition = new TestThreeTypeCondition();

            var success = MatchRatingDataTransform.RateMatches(condition,
                Data.IntTraitValues, Data.FloatTraitValues, Data.IntTraitValues2,
                idIntersection, ref s_Ratings, ref s_ConditionIndex);

            Assert.False(success);
            Assert.AreEqual(1, s_ConditionIndex);
            AssertValidRatingCount(ref s_Ratings, 0);
        }

        [Test]
        public void RateMultiTypeRelation_Success()
        {
            s_Ratings = new NativeConditionRatings(1, k_DefaultRatingsCapacity);
            var relation = new TestMultiTypeRelation();

            var input = Data.GetRelationInput();
            RelationRatingTransform.RateMatches(relation, ref input, ref s_Ratings, ref s_ConditionIndex);
            input.Dispose();

            Assert.AreEqual(1, s_ConditionIndex);
            // we expect ids 0 and 3 to fail - see Data.GetRelationInput()
            int[] expectedMatchedIds = { 1, 2 };
            AssertValidRatingCount(ref s_Ratings, expectedMatchedIds.Length);
        }

        [Test]
        public void RateMultiTypeRelation_Failure()
        {
            s_Ratings = new NativeConditionRatings(1, k_DefaultRatingsCapacity);
            var relation = new TestMultiTypeRelation();

            var input = Data.GetUnmatchableRelationInput();
            RelationRatingTransform.RateMatches(relation, ref input, ref s_Ratings, ref s_ConditionIndex);
            input.Dispose();

            Assert.AreEqual(1, s_ConditionIndex);
            AssertValidRatingCount(ref s_Ratings, 0);
        }

        static void AssertValidRatingCount(ref NativeConditionRatings ratingBuffer, int expectedCount)
        {
            var validRatingCount = 0;
            foreach (var rating in ratingBuffer.Buffer)
            {
                if (rating > 0f)
                    validRatingCount++;
                else if(rating < 0f)
                    Assert.Fail("ratings less than 0 should not be written to the ratings buffer");
            }

            Assert.AreEqual(expectedCount, validRatingCount);
        }

        internal static class Data
        {
            public static int[] ExpectedIntFloatIntersection = { 0, 3, 6, 8, 12 };
            // we expect this data id to fail the test condition because one of the source traits is 0
            public static int[] FailingIntFloatIntersection = { 3 };
            // all int trait ids with a value of 0 should fail the test condition
            public static int[] ExpectedIntFloatConditionPassed = { 0, 6, 8, 12 };

            public static Dictionary<int, float> FloatTraitValues = new Dictionary<int, float>()
            {
                {0, 1f}, {2, 0.5f}, {3, 0.3f}, {5, 0.6f}, {6, 0.7f}, {8, 0.8f}, {10, 0.9f}, {12, 0.5f}, {14, 0.4f}
            };

            public static Dictionary<int, int> IntTraitValues = new Dictionary<int, int>()
            {
                {0, 1}, {1, 0}, {3, 0}, {4, 1}, {6, 1}, {8, 1}, {11, 0}, {12, 1}, {13, 0}, {15, 1}, {16, 2}
            };

            public static Dictionary<int, int> IntTraitValues2 = new Dictionary<int, int>()
            {
                {1, 1}, {3, 0}, {4, 1}, {6, 2}, {8, 1}, {9, 1}, {11, 1}, {13, 2}, {14, 1}, {15, 1}, {16, 2},
                {7, 1}, {5, 1}, {17, 1}, {18, 2}, {19, 1}, {20, 1}
            };

            public static RelationInput<TestIntFloatChildValues, TestIntChildValues> GetRelationInput()
            {
                const int count = 4;
                var input = new RelationInput<TestIntFloatChildValues, TestIntChildValues>(count, Allocator.Temp);
                for (var i = 0; i < count; i++)
                {
                    var child1 = new TestIntFloatChildValues(count - i, 0.1f * (i - 1));
                    var child2 = new TestIntChildValues(i);
                    input.Add(i, child1, child2);
                }

                return input;
            }

            public static RelationInput<TestIntFloatChildValues, TestIntChildValues> GetUnmatchableRelationInput()
            {
                const int count = 4;
                var input = new RelationInput<TestIntFloatChildValues, TestIntChildValues>(count, Allocator.Temp);
                for (var i = 0; i < count; i++)
                {
                    // because every child's values have a 0 in them, the test condition should fail every entry
                    var child1 = new TestIntFloatChildValues(0, 0.1f * (i - 1));
                    var child2 = new TestIntChildValues(i);
                    input.Add(i, child1, child2);
                }

                return input;
            }
        }
    }
}
