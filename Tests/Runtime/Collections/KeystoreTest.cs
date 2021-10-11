using BlackTundra.Foundation.Collections;

using NUnit.Framework;

namespace BlackTundra.Foundation.Tests.Collections {

    public sealed class KeystoreTest {

        [Test(
            Author = "Alex James Thomson",
            Description = "Tests if the keystore can be serialized and deserialized correctly.",
            TestOf = typeof(Keystore)
        )]
        [Category("Unit")]
        public void SerializationTest() {
            Keystore keystore = new Keystore();
            keystore.Set("test.cat1.var1", 0);
            keystore.Set("test.cat1.var2", 1);
            keystore.Set("test.cat1.var3", 2);
            keystore.Set("test.cat2.var1", true);
            keystore.Set("test.cat2.var2", 123.456f);
            keystore.Set("test.cat2.var3", "Hello World");
            keystore = Keystore.FromBytes(keystore.ToBytes());
            Assert.IsTrue(keystore.ContainsKey("test.cat1.var1"));
            Assert.AreEqual(0, keystore.Get<int>("test.cat1.var1"));
            Assert.IsTrue(keystore.ContainsKey("test.cat1.var2"));
            Assert.AreEqual(1, keystore.Get<int>("test.cat1.var2"));
            Assert.IsTrue(keystore.ContainsKey("test.cat1.var3"));
            Assert.AreEqual(2, keystore.Get<int>("test.cat1.var3"));
            Assert.IsTrue(keystore.ContainsKey("test.cat2.var1"));
            Assert.AreEqual(true, keystore.Get<bool>("test.cat2.var1"));
            Assert.IsTrue(keystore.ContainsKey("test.cat2.var2"));
            Assert.AreEqual(123.456f, keystore.Get<float>("test.cat2.var2"));
            Assert.IsTrue(keystore.ContainsKey("test.cat2.var3"));
            Assert.AreEqual("Hello World", keystore.Get<string>("test.cat2.var3"));
        }

    }

}