using BlackTundra.Foundation.Collections.Generic;

using NUnit.Framework;

namespace BlackTundra.Foundation.RuntimeTests.Collections {

    [TestFixture(
        Author = "Alex James Thomson",
        Description = "Tests the OrderedList class.",
        TestOf = typeof(OrderedList<int, int>)
    )]
    public sealed class OrderedListTest {

        OrderedList<int, int> list;

        [SetUp]
        public void SetUp() {
            list = new OrderedList<int, int>();
            Assert.AreEqual(0, list.Count);
            Assert.IsTrue(list.IsEmpty);
        }

        [Test(
            Author = "Alex James Thomson",
            Description = "Tests adding and subtracting elements to an ordered list.",
            TestOf = typeof(OrderedList<int, int>)
        )]
        [Category("Unit")]
        public void AddSubtractElementTest() {
            // add elements to the list:
            for (int i = 0; i < 10; i++) {
                list.Add(9 - i, i);
            }
            Assert.AreEqual(10, list.Count);
            Assert.IsFalse(list.IsEmpty);
            // check order of elements:
            for (int i = 0; i < 10; i++) {
                int index = list[i];
                Assert.AreEqual(i, 9 - index);
            }
            // subtract elements:
            for (int i = 0; i < 5; i++) {
                list.RemoveAt(0);
            }
            Assert.AreEqual(5, list.Count);
            Assert.IsFalse(list.IsEmpty);
            for (int i = 0; i < 5; i++) {
                list.RemoveAt(0);
            }
            Assert.AreEqual(0, list.Count);
            Assert.IsTrue(list.IsEmpty);
        }

    }

}
