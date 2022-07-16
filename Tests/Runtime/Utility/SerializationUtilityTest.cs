using BlackTundra.Foundation.Serialization;
using BlackTundra.Foundation.Utility;

using NUnit.Framework;

using UnityEngine;

namespace BlackTundra.Foundation.RuntimeTests.Utility {

    [TestFixture(
        Author = "Alex James Thomson",
        Description = "Tests the serialization utilities packaged with the foundation package.",
        TestOf = typeof(ObjectUtility)
    )]
    public sealed class SerializationUtilityTest {

        [Test(
            Author = "Alex James Thomson",
            Description = "Tests if custom serialized types can be serialized and deserialized correctly.",
            TestOf = typeof(ObjectUtility)
        )]
        [Category("Unit")]
        public void CustomSerialization() {
            ValidateSerialization(new Vector2(1.0f, 2.0f));
            ValidateSerialization(new Vector3(1.0f, 2.0f, 3.0f));
            ValidateSerialization(new Vector4(1.0f, 2.0f, 3.0f, 4.0f));
            ValidateSerialization(new Quaternion(1.0f, 2.0f, 3.0f, 4.0f));
        }

        [Test(
            Author = "Alex James Thomson",
            Description = "Tests if serializable objects can be serialized and deserialized correctly.",
            TestOf = typeof(ObjectUtility)
        )]
        [Category("Unit")]
        public void Unit_StandardSerialization() {
            ValidateSerialization(1);
            ValidateSerialization(1u);
            ValidateSerialization(1.0f);
            ValidateSerialization(1.0d);
            ValidateSerialization(short.MaxValue);
            ValidateSerialization(ushort.MaxValue);
            ValidateSerialization(long.MaxValue);
            ValidateSerialization(ulong.MaxValue);
            ValidateSerialization(true);
            ValidateSerialization(false);
            ValidateSerialization(string.Empty);
            ValidateSerialization("Hello world!");
        }

        private void ValidateSerialization<T>(in T obj) {
            byte[] bytes = ObjectSerializer.SerializeToBytes(obj);
            Assert.NotNull(bytes);
            Assert.IsNotEmpty(bytes, "Empty serialized bytes.");
            T newObj = ObjectSerializer.ToObject<T>(bytes);
            Assert.AreEqual(obj, newObj, "New object is not equal to original.");
        }

    }

}
