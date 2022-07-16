using BlackTundra.Foundation.Collections.Generic;

using NUnit.Framework;

using UnityEngine;

namespace BlackTundra.Foundation.RuntimeTests.Collections {

    [TestFixture(
        Author = "Alex James Thomson",
        Description = "Tests the WeightedList class.",
        TestOf = typeof(WeightedList<int>)
    )]
    public sealed class WeightedListTest {

        WeightedList<int> list;

        [SetUp]
        public void SetUp() {
            list = new WeightedList<int>();
            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(0, list.TotalWeight);
            Assert.IsTrue(list.IsEmpty);
        }

        [Test(
            Author = "Alex James Thomson",
            Description = "Tests adding and subtracting elements to a weighted list.",
            TestOf = typeof(WeightedList<int>)
        )]
        [Category("Unit")]
        public void AddSubtractElementTest() {
            // test add:
            for (int i = 0; i < 10; i++) {
                list.Add(10, i);
            }
            Assert.AreEqual(10, list.Count);
            Assert.AreEqual(100, list.TotalWeight);
            Assert.IsFalse(list.IsEmpty);
            for (int i = 0; i < 10; i++) {
                Assert.AreEqual(list[i].value, i);
            }
            // test subtract:
            for (int i = 0; i < 5; i++) {
                list.RemoveAt(0);
            }
            Assert.AreEqual(5, list.Count);
            Assert.AreEqual(50, list.TotalWeight);
            Assert.IsFalse(list.IsEmpty);
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual(list[i].value, i + 5);
            }
            for (int i = 0; i < 5; i++) {
                list.RemoveAt(0);
            }
            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(0, list.TotalWeight);
            Assert.IsTrue(list.IsEmpty);
        }

        [Test(
            Author = "Alex James Thomson",
            Description = "Tests getting random unweighted elements from a weighted list.",
            TestOf = typeof(WeightedList<int>)
        )]
        [Category("Unit")]
        public void GetRandomTest() {
            for (int i = 0; i < 10; i++) {
                list.Add(10, i);
            }
            int[] frequency = new int[10];
            Assert.AreEqual(list.Count, 10);
            for (int i = 0; i < 10000; i++) {
                int value = list.GetRandom();
                Assert.GreaterOrEqual(value, 0);
                Assert.Less(value, 10);
                frequency[value]++;
            }
            // check frequency of indexes picked:
            for (int i = 0; i < 10; i++) {
                Assert.GreaterOrEqual(frequency[i], 900);
                Assert.LessOrEqual(frequency[i], 1100);
            }
        }

        [Test(
            Author = "Alex James Thomson",
            Description = "Tests getting single random weighted element from a weighted list populated with elements with zero weight.",
            TestOf = typeof(WeightedList<int>)
        )]
        [Category("Unit")]
        public void GetSingleRandomWeightedTest() {
            for (int i = 0; i < 5; i++) {
                list.Add(0, i);
            }
            list.Add(1, -1);
            for (int i = 0; i < 4; i++) {
                list.Add(0, i + 5);
            }
            Assert.AreEqual(10, list.Count);
            Assert.AreEqual(1, list.TotalWeight);
            for (int i = 0; i < 10000; i++) {
                int value = list.GetRandomWeighted();
                Assert.AreEqual(-1, value);
            }
        }

        [Test(
            Author = "Alex James Thomson",
            Description = "Tests getting many random weighted elements from a weighted list.",
            TestOf = typeof(WeightedList<int>)
        )]
        [Category("Unit")]
        public void GetRandomWeightedTest() {
            list.Add(10,  0);
            list.Add(25,  1);
            list.Add(50,  2);
            list.Add(100, 3);
            Assert.AreEqual(4, list.Count);
            Assert.AreEqual(185, list.TotalWeight);
            int[] frequency = new int[4];
            for (int i = 0; i < 10000; i++) {
                int value = list.GetRandomWeighted();
                frequency[value]++;
            }
            // 0 (weighted 10 ~ 540 occurrences):
            Assert.GreaterOrEqual(frequency[0], Mathf.Floor(540 * 0.9f));
            Assert.LessOrEqual   (frequency[0], Mathf.Ceil (540 * 1.1f));
            // 1 (weighted 25 ~ 1351 occurrences):
            Assert.GreaterOrEqual(frequency[1], Mathf.Floor(1351 * 0.9f));
            Assert.LessOrEqual   (frequency[1], Mathf.Ceil (1351 * 1.1f));
            // 2 (weighted 50 ~ 2702 occurrences):
            Assert.GreaterOrEqual(frequency[2], Mathf.Floor(2702 * 0.9f));
            Assert.LessOrEqual   (frequency[2], Mathf.Ceil (2702 * 1.1f));
            // 3 (weighted 100 ~ 5405 occurrences):
            Assert.GreaterOrEqual(frequency[3], Mathf.Floor(5405 * 0.9f));
            Assert.LessOrEqual   (frequency[3], Mathf.Ceil (5405 * 1.1f));
        }

    }

}