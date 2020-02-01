using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Labs.MARS.Query;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Unity.Labs.MARS.Data.Tests
{
    public class SetMatchSearchStageTests
    {
        public class SetExpectation
        {
            public float ApproximateExpectedRating;
            public SetRatingConfiguration RatingConfiguration;
            public KeyValuePair<int, float>[] Members;
            public RelationDataPair[] LocalRelationIndices;
            public RelationRatingsData RelationRatings;
        }

        static readonly SetRatingConfiguration k_DefaultRatingConfig = new SetRatingConfiguration(0.5f);
        static readonly SetRatingConfiguration k_MemberWeightedRatingConfig = new SetRatingConfiguration(0.7f);
        static readonly SetRatingConfiguration k_RelationWeightedRatingConfig = new SetRatingConfiguration(0.3f);

        [OneTimeSetUp]
        public void Setup()
        {
        }

        [SetUp]
        public void SetupBeforeEach()
        {
        }

        [OneTimeTearDown]
        public void AfterAll()
        {
        }

        [TearDown]
        public void TearDownBeforeEach()
        {
        }

        [TestCaseSource(typeof(DatabaseTestData), nameof(DatabaseTestData.RatingSets))]
        public void GetSetSearchSpaceSizeTest(List<Dictionary<int, float>> allMemberRatings, int expectedCount)
        {
            var memberIndices = new [] { 1, 2, 3, 4 };
            var size = SetMatchSearchTransform.GetSetSearchSpaceSize(memberIndices, allMemberRatings);
            Assert.AreEqual(expectedCount, size);
        }

        [TestCaseSource(typeof(SetAssignmentRatingData), nameof(SetAssignmentRatingData.NoDuplicatesCases))]
        public void CheckDuplicateAssignments_NoDuplicatesCases(KeyValuePair<int, float>[] hypothesis)
        {
            Assert.False(SetMatchSearchTransform.AnyDuplicates(hypothesis));
        }

        [TestCaseSource(typeof(SetAssignmentRatingData), nameof(SetAssignmentRatingData.DuplicatesCases))]
        public void CheckDuplicateAssignments_DuplicatesPresentCases(KeyValuePair<int, float>[] hypothesis)
        {
            Assert.True(SetMatchSearchTransform.AnyDuplicates(hypothesis));
        }

        // this tests the averaging function, and the below case tests that it gets called at the right time
        [TestCaseSource(typeof(SetAssignmentRatingData), nameof(SetAssignmentRatingData.NoRelationsCases))]
        public void AverageMemberRating(KeyValuePair<int, float>[] hypothesis, float expectedRating)
        {
            Assert.True(Mathf.Approximately(expectedRating, SetMatchSearchTransform.AverageMemberRating(hypothesis)));
        }

        // When a set has valid Relations, the match rating should come from a combination of member & relation ratings
        [TestCaseSource(typeof(SetAssignmentRatingData), nameof(SetAssignmentRatingData.RelationSuccessCases))]
        public void TryRateAssignmentSet_RelationsPresent_SuccessCases(SetExpectation set)
        {
            Assert.True(SetMatchSearchTransform.TryRateAssignmentSet(set.Members,
                set.RelationRatings, set.LocalRelationIndices, set.RatingConfiguration, out var rating));

            Assert.True(Mathf.Approximately(set.ApproximateExpectedRating, rating));
        }

        [TestCaseSource(typeof(SetAssignmentRatingData), nameof(SetAssignmentRatingData.RelationFailureCases))]
        public void TryRateAssignmentSet_RelationsPresent_FailureCases(SetExpectation set)
        {
            Assert.False(SetMatchSearchTransform.TryRateAssignmentSet(set.Members,
                set.RelationRatings, set.LocalRelationIndices, set.RatingConfiguration, out var rating));

            Assert.Zero(rating);
        }

        [Test]
        public void CalculateSolveOrder()
        {
            var weights = new [] {2.4f, 3.2f, 1.7f, 1.3f, 2.8f};
            // index 3 is not in these indices, so it should not show up in the results
            var workingIndices = new List<int> {0, 1, 2, 4};
            var indexToWeight = new List<KeyValuePair<int, float>>();
            SetMatchSearchTransform.CalculateSolveOrder(weights, workingIndices, indexToWeight);

            var expectedOrder = new[] {1, 4, 0, 2};
            Assert.AreEqual(expectedOrder.Length, indexToWeight.Count);

            var previousWeight = float.MaxValue;
            for (var i = 0; i < expectedOrder.Length; i++)
            {
                var expected = expectedOrder[i];
                var actualKvp = indexToWeight[i];
                Assert.AreEqual(expected, actualKvp.Key);
                // make sure what we get back is sorted in order of descending weight
                Assert.LessOrEqual(actualKvp.Value, previousWeight);
                previousWeight = actualKvp.Value;
            }
        }

        [TestCaseSource(typeof(SetAssignmentRatingData), nameof(SetAssignmentRatingData.GetIterationTargetsCases))]
        public void GetIterationTargets(float portion, Dictionary<int, float>[] ratings, int[] expectedTargets)
        {
            var targets = new int[ratings.Length];
            SetMatchSearchTransform.GetIterationTargets(portion, ratings, targets);

            for (var i = 0; i < expectedTargets.Length; i++)
            {
                Assert.AreEqual(expectedTargets[i], targets[i]);
            }
        }

        [Test]
        public void RemoveClaimedData()
        {
            var combo = new [] { 4, 5, 1 };
            var allMemberExclusivities = new List<Exclusivity>
            {
                Exclusivity.Shared, Exclusivity.Reserved, Exclusivity.ReadOnly,
                Exclusivity.Reserved, Exclusivity.Shared, Exclusivity.ReadOnly
            };
            var globalRatingSet = new List<Dictionary<int, float>>
            {
                new Dictionary<int, float> {{0, 0.9f}, {1, 0.7f}, {4, 0.5f}, {5, 0.2f}},
                new Dictionary<int, float> {{5, 0.9f}},
                new Dictionary<int, float> {{1, 0.9f}},
                new Dictionary<int, float> {{2, 0.8f}, {3, 0.7f}, {4, 0.6f}, {5, 0.5f}, {10, 0.4f}, {11, 0.2f}},
                new Dictionary<int, float> {{4, 0.8f}, {5, 0.7f}, {13, 0.6f}, {14, 0.5f}, {1, 0.3f}, {17, 0.2f}},
                new Dictionary<int, float> {{7, 1f}, {4, 0.9f}, {9, 0.7f}, {5, 0.5f}, {30, 0.7f}},
            };

            var selfMemberIndices = new [] { 0, 1 ,2 };
            var otherSetMemberIndices = new [] { 3, 4, 5 };

            var member1 = globalRatingSet[3];
            var member2 = globalRatingSet[4];
            var member3 = globalRatingSet[5];

            var member1CountBefore = member1.Count;
            var member2CountBefore = member2.Count;
            var member3CountBefore = member3.Count;

            SetMatchSearchTransform.RemoveClaimedData(combo, selfMemberIndices, otherSetMemberIndices,
                allMemberExclusivities, globalRatingSet);

            // member 1 is reserved, so both shared and reserved IDs (4 & 5) should be gone
            Assert.AreEqual(member1CountBefore - 2, member1.Count);
            Assert.False(member1.ContainsKey(combo[0]));
            Assert.False(member1.ContainsKey(combo[1]));

            // because member 2 is shared, we only remove the reserved ID from its options
            Assert.AreEqual(member2CountBefore - 1, member2.Count);
            Assert.True(member2.ContainsKey(combo[0]));
            Assert.False(member2.ContainsKey(combo[1]));
            Assert.True(member2.ContainsKey(combo[2]));

            // member 3 is readonly, so we don't remove any data
            Assert.AreEqual(member3CountBefore, member3.Count);
            Assert.True(member3.ContainsKey(combo[0]));
            Assert.True(member3.ContainsKey(combo[1]));
        }

        public static class SetAssignmentRatingData
        {
            // The test cases depend on these specific Relation ratings.  Tests will break if they are changed!
            public static Dictionary<RelationDataPair, float> SimpleRelationRatings1 =
                new Dictionary<RelationDataPair, float>()
                {
                    {new RelationDataPair(1, 4), 0.75f}, {new RelationDataPair(5, 2), 0.7f},
                    {new RelationDataPair(8, 2), 0.92f}, {new RelationDataPair(2, 4), 0.9f}
                };

            public static Dictionary<RelationDataPair, float> SimpleRelationRatings2 =
                new Dictionary<RelationDataPair, float>()
                {
                    {new RelationDataPair(1, 8), 0.6f}, {new RelationDataPair(1, 2), 0.5f},
                    {new RelationDataPair(8, 2), 0.82f}, {new RelationDataPair(4, 5), 0.4f}
                };

            public static Dictionary<RelationDataPair, float> SimpleRelationRatings3 =
                new Dictionary<RelationDataPair, float>()
                {
                    {new RelationDataPair(3, 8), 0.4f}, {new RelationDataPair(2, 4), 0.6f},
                    {new RelationDataPair(10, 4), 0.32f}, {new RelationDataPair(10, 8), 0.2f}
                };

            public static IEnumerable NoRelationsCases
            {
                get
                {
                    var hypothesis = new []
                    {
                        new KeyValuePair<int, float>(1, 0.5f), new KeyValuePair<int, float>(4, 1f),
                        new KeyValuePair<int, float>(8, 0.3f), new KeyValuePair<int, float>(10, 0.9f),
                    };

                    yield return new TestCaseData(hypothesis, 2.7f / 4f);

                    hypothesis = new []
                    {
                        new KeyValuePair<int, float>(1, 0.5f), new KeyValuePair<int, float>(4, 1f),
                        new KeyValuePair<int, float>(8, 0.3f), new KeyValuePair<int, float>(10, 0.9f),
                        new KeyValuePair<int, float>(12, 0.6f), new KeyValuePair<int, float>(10, 0.4f),
                    };

                    yield return new TestCaseData(hypothesis, 3.7f / 6f);
                }
            }

            static float GetExpected(float membersAverage, float reducedRelations, SetRatingConfiguration config)
            {
                var members = membersAverage * config.MemberMatchWeight;
                var relations = reducedRelations * (1f - config.MemberMatchWeight);
                return members + relations;
            }

            // A very simple Set - 2 members with one Relation between them.
            public static SetExpectation GetSimpleSetCase(SetRatingConfiguration ratingConfig)
            {
                var relationRatings = new RelationRatingsData().Initialize(1);
                relationRatings[0] = SimpleRelationRatings1;
                return new SetExpectation()
                {
                    RatingConfiguration = ratingConfig,
                    // since we only have one relation, rated 0.75, it contributes all the relation ratings
                    //ApproximateExpectedRating = ((0.8f + 0.9f) * 0.5f + 0.75f) * 0.5f,
                    ApproximateExpectedRating = GetExpected((0.8f + 0.9f) * 0.5f, 0.75f, ratingConfig),
                    Members = new[]
                    {
                        new KeyValuePair<int, float>(1, 0.8f), new KeyValuePair<int, float>(4, 0.9f),
                    },
                    RelationRatings = relationRatings,
                    LocalRelationIndices = new[] {new RelationDataPair(0, 1)}
                };
            }

            // this version should fail, because the member assignments do not form a valid Relation
            public static SetExpectation GetSimpleFailingSetCase()
            {
                var relationRatings = new RelationRatingsData().Initialize(1);
                relationRatings[0] = SimpleRelationRatings1;
                return new SetExpectation()
                {
                    Members = new[]
                    {
                        new KeyValuePair<int, float>(4, 0.8f), new KeyValuePair<int, float>(2, 0.9f),
                    },
                    RelationRatings = relationRatings,
                    LocalRelationIndices = new[] {new RelationDataPair(0, 1)}
                };
            }

            // Here, the first relation's assignments are valid, but the second is not, so it should fail
            public static SetExpectation GetMediumFailingSetCase()
            {
                var relationRatings = new RelationRatingsData().Initialize(2);
                relationRatings[0] = SimpleRelationRatings1;
                relationRatings[1] = SimpleRelationRatings2;
                return new SetExpectation()
                {
                    Members = new[]
                    {
                        new KeyValuePair<int, float>(1, 0.8f),
                        new KeyValuePair<int, float>(4, 0.9f),
                        new KeyValuePair<int, float>(10, 0.72f),
                    },
                    RelationRatings = relationRatings,
                    LocalRelationIndices = new[] {new RelationDataPair(0, 1), new RelationDataPair(0, 2)}
                };
            }

            // 3 members with 2 relations between them - member 1 has relations to both other members
            public static SetExpectation GetMediumSetCase(SetRatingConfiguration ratingConfig)
            {
                var relationRatings = new RelationRatingsData().Initialize(2);
                relationRatings[0] = SimpleRelationRatings1;
                relationRatings[1] = SimpleRelationRatings2;

                var memberContribution = (0.8f + 0.9f + 0.72f) / 3f;
                var relationContribution = Mathf.Pow(0.75f * 0.6f, 1f / 2f);

                return new SetExpectation()
                {
                    RatingConfiguration = ratingConfig,
                    // since we only have one relation, rated 0.75, it contributes all the relation ratings
                    ApproximateExpectedRating = GetExpected(memberContribution, relationContribution, ratingConfig),
                    Members = new[]
                    {
                        new KeyValuePair<int, float>(1, 0.8f),
                        new KeyValuePair<int, float>(4, 0.9f),
                        new KeyValuePair<int, float>(8, 0.72f),
                    },
                    RelationRatings = relationRatings,
                    LocalRelationIndices = new[] {new RelationDataPair(0, 1), new RelationDataPair(0, 2)}
                };
            }

            // 5 members with 3 relations between them
            // member 1 has relations to 2 & 3
            // member 4 has a relation to member 3, member 5 has no relations
            public static SetExpectation GetMediumLargeSetCase(SetRatingConfiguration ratingConfig)
            {
                var relationRatings = new RelationRatingsData().Initialize(3);
                relationRatings[0] = SimpleRelationRatings1;
                relationRatings[1] = SimpleRelationRatings2;
                relationRatings[2] = SimpleRelationRatings3;

                var memberContribution = (0.8f + 0.9f + 0.72f + 0.5f + 0.3f) / 5f;
                var relationContribution = Mathf.Pow(0.75f * 0.6f * 0.4f, 1f / 3f);

                return new SetExpectation()
                {
                    RatingConfiguration = ratingConfig,
                    // since we only have one relation, rated 0.75, it contributes all the relation ratings
                    //ApproximateExpectedRating = (memberContribution + relationContribution) * 0.5f,
                    ApproximateExpectedRating = GetExpected(memberContribution, relationContribution, ratingConfig),
                    Members = new[]
                    {
                        new KeyValuePair<int, float>(1, 0.8f),
                        new KeyValuePair<int, float>(4, 0.9f),
                        new KeyValuePair<int, float>(8, 0.72f),
                        new KeyValuePair<int, float>(3, 0.5f),
                        new KeyValuePair<int, float>(10, 0.3f),
                    },
                    RelationRatings = relationRatings,
                    LocalRelationIndices = new[]
                    {
                        new RelationDataPair(0, 1), new RelationDataPair(0, 2), new RelationDataPair(3, 2),
                    }
                };
            }

            public static IEnumerable RelationSuccessCases
            {
                get
                {
                    // test 3 different sizes / complexities of Sets, with 3 different rating configurations each
                    yield return new TestCaseData(GetSimpleSetCase(k_DefaultRatingConfig));
                    yield return new TestCaseData(GetSimpleSetCase(k_MemberWeightedRatingConfig));
                    yield return new TestCaseData(GetSimpleSetCase(k_RelationWeightedRatingConfig));
                    yield return new TestCaseData(GetMediumSetCase(k_DefaultRatingConfig));
                    yield return new TestCaseData(GetMediumSetCase(k_MemberWeightedRatingConfig));
                    yield return new TestCaseData(GetMediumSetCase(k_RelationWeightedRatingConfig));
                    yield return new TestCaseData(GetMediumLargeSetCase(k_DefaultRatingConfig));
                    yield return new TestCaseData(GetMediumLargeSetCase(k_MemberWeightedRatingConfig));
                    yield return new TestCaseData(GetMediumLargeSetCase(k_RelationWeightedRatingConfig));
                }
            }

            public static IEnumerable RelationFailureCases
            {
                get
                {
                    yield return new TestCaseData(GetSimpleFailingSetCase());
                    yield return new TestCaseData(GetMediumFailingSetCase());
                }
            }

            internal static readonly Dictionary<int, float>[] LocalRatingSet1 =
            {
                new Dictionary<int, float> {{0, 0.9f}, {1, 0.5f}, {5, 0.2f}},
                new Dictionary<int, float> {{11, 0.8f}, {12, 0.7f}, {13, 0.6f}, {14, 0.5f}, {15, 0.3f}, {20, 0.2f}},
            };

            // 120 total possibilities
            internal static readonly Dictionary<int, float>[] LocalRatingSet2 =
            {
                new Dictionary<int, float> {{0, 0.9f}, {1, 0.3f}},
                new Dictionary<int, float> {{2, 0.8f}, {3, 0.7f}, {4, 0.6f}, {5, 0.5f}},
                new Dictionary<int, float> {{11, 0.8f}, {12, 0.7f}, {13, 0.6f}, {14, 0.5f}, {15, 0.3f}},
                new Dictionary<int, float> {{7, 1f}, {8, 0.9f}, {9, 0.7f}}
            };

            // 2160 total possibilities
            internal static readonly Dictionary<int, float>[] LocalRatingSet3 =
            {
                new Dictionary<int, float> {{0, 0.9f}, {1, 0.7f}, {4, 0.5f}, {5, 0.2f}},
                new Dictionary<int, float> {{2, 0.8f}, {3, 0.7f}, {4, 0.6f}, {5, 0.5f}, {10, 0.4f}, {11, 0.2f}},
                new Dictionary<int, float> {{11, 0.8f}, {12, 0.7f}, {13, 0.6f}, {14, 0.5f}, {15, 0.3f}, {17, 0.2f}},
                new Dictionary<int, float> {{7, 1f}, {8, 0.9f}, {9, 0.7f}, {25, 0.5f}, {30, 0.7f}},
                new Dictionary<int, float> {{20, 0.7f}, {9, 0.5f}, {25, 0.4f}}
            };

            public static IEnumerable GetIterationTargetsCases
            {
                get
                {
                    const float portion1 = 0.8f;
                    var expectedTargets1 = new [] {3, 5};
                    yield return new TestCaseData(portion1, LocalRatingSet1, expectedTargets1);

                    // 120 possibilities * 0.5 = ~60 count target
                    const float portion2 = 0.5f;
                    // {2, 3, 5, 2} would give exactly 60, and be an exact right answer, but this is close enough
                    var expectedTargets2 = new [] {2, 3, 4, 2};
                    yield return new TestCaseData(portion2, LocalRatingSet2, expectedTargets2);

                    const float portion3 = 0.02f;
                    var expectedTargets3 = new [] {2, 9, 5, 6};
                    var largeRandomSet = new[]
                    {
                        NewRandomRatings(4), NewRandomRatings(25), NewRandomRatings(15), NewRandomRatings(18)
                    };

                    yield return new TestCaseData(portion3, largeRandomSet, expectedTargets3);

                    const float portion4 = 0.3333f;
                    var expectedTargets4 = new [] {1, 6, 1, 13};
                    var unevenRandomSet = new[]
                    {
                        NewRandomRatings(1), NewRandomRatings(8), NewRandomRatings(1), NewRandomRatings(30)
                    };

                    yield return new TestCaseData(portion4, unevenRandomSet, expectedTargets4);
                }
            }

            static Dictionary<int, float> NewRandomRatings(int count)
            {
                var dictionary = new Dictionary<int, float>(count);
                dictionary.Clear();
                for (var i = 0; i < count; i++)
                {
                    dictionary.Add(count - i, Random.Range(0.1f, 1f));
                }

                return dictionary;
            }

            public static IEnumerable NoDuplicatesCases
            {
                get
                {
                    var hypothesis1 = new[]
                    {
                        new KeyValuePair<int, float>(1, 0.7f), new KeyValuePair<int, float>(10, 0.9f),
                        new KeyValuePair<int, float>(4, 0.5f), new KeyValuePair<int, float>(11, 0.5f)
                    };
                    yield return new TestCaseData(hypothesis1);

                    var hypothesis2 = new[]
                    {
                        new KeyValuePair<int, float>(2, 0.5f), new KeyValuePair<int, float>(4, 0.5f),
                        new KeyValuePair<int, float>(5, 0.5f), new KeyValuePair<int, float>(9, 0.5f),
                        new KeyValuePair<int, float>(10, 0.5f), new KeyValuePair<int, float>(12, 0.5f)
                    };
                    yield return new TestCaseData(hypothesis2);
                }
            }

            public static IEnumerable DuplicatesCases
            {
                get
                {
                    var hypothesis1 = new[]
                    {
                        new KeyValuePair<int, float>(15, 0.8f), new KeyValuePair<int, float>(6, 1f),
                        new KeyValuePair<int, float>(4, 0.7f), new KeyValuePair<int, float>(15, 0.4f)
                    };
                    yield return new TestCaseData(hypothesis1);

                    var hypothesis2 = new[]
                    {
                        new KeyValuePair<int, float>(2, 0.8f), new KeyValuePair<int, float>(4, 0.7f),
                        new KeyValuePair<int, float>(5, 0.9f), new KeyValuePair<int, float>(9, 0.6f),
                        new KeyValuePair<int, float>(12, 0.8f), new KeyValuePair<int, float>(5, 0.8f)
                    };
                    yield return new TestCaseData(hypothesis2);
                }
            }
        }
    }
}
