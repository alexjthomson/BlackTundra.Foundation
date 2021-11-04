using NUnit.Framework;

namespace BlackTundra.Foundation.RuntimeTests {

    public sealed class VersionTest {

        [Test(
            Author = "Alex James Thomson",
            Description = "Tests if the Version struct can parse a version string correctly and convert it back to a string.",
            TestOf = typeof(Version)
        )]
        [Category("Unit")]
        public void Parse() {
            string[] versions = new string[] {
                "1.0.0a",
                "1.0.0b",
                "1.0.0f",
                "12.34.56a",
                "78.90.100b",
                "123.456.789f"
            };
            for (int i = 0; i < versions.Length; i++) {
                string versionString = versions[i];
                Assert.AreEqual(versionString, Version.Parse(versionString).ToString());
            }
        }

        [Test(
            Author = "Alex James Thomson",
            Description = "Tests if the Version struct can identify invalid version strings.",
            TestOf = typeof(Version)
        )]
        [Category("Unit")]
        public void Validation() {
            string[] versions = new string[] {
                "2020.0.1",
                "a.b.ca",
                "1.0",
                "3",
                "1.2.3c",
                "0.0.0a"
            };
            for (int i = 0; i < versions.Length; i++) {
                string versionString = versions[i];
                Assert.False(Version.IsValid(versionString), "Version struct failed to identify invalid version: " + versionString);
            }
            versions = new string[] {
                "1.0.0a",
                "1.0.0b",
                "1.0.0f",
                "12.34.56a",
                "78.90.100b",
                "123.456.789f"
            };
            for (int i = 0; i < versions.Length; i++) {
                string versionString = versions[i];
                Assert.True(Version.IsValid(versionString), "Version struct failed to identify valid version: " + versionString);
            }
        }

    }

}
