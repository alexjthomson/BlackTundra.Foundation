using BlackTundra.Foundation.Security;

using NUnit.Framework;

namespace BlackTundra.Foundation.Tests.Security {

    public sealed class SecureTypesTest {

        [Test(
            Author = "Alex James Thomson",
            Description = "Tests if the sshort type functions correctly.",
            TestOf = typeof(sshort)
        )]
        [Category("Unit")]
        public void SShortTest() {
            short i1 = -10, i2 = 25, i3 = 5, i4 = 10;
            sshort si1 = i1, si2 = i2, si3 = i3, si4 = i4;
            Assert.AreEqual(i1, si1);
            Assert.AreEqual(i2, si2);
            Assert.AreEqual(i3, si3);
            Assert.AreEqual(i4, si4);

            Assert.AreEqual(i1 + i2 + i3 + i4, (int)(si1 + si2 + si3 + si4));
            Assert.AreEqual(i1 - i2 - i3 - i4, (int)(si1 - si2 - si3 - si4));
            Assert.AreEqual(i1 * i2 * i3 * i4, (int)(si1 * si2 * si3 * si4));
            Assert.AreEqual(i4 / i1 + i3 / i2, (int)(si4 / si1 + si3 / si2));
            Assert.AreEqual(i1 ^ i2 ^ i3 ^ i4, (int)(si1 ^ si2 ^ si3 ^ si4));

            si1.value = i1;
            Assert.AreEqual(i1, si1);
            si1.value = i2;
            Assert.AreEqual(i2, si1);
            si1.value = i3;
            Assert.AreEqual(i3, si1);
            si1.value = i4;
            Assert.AreEqual(i4, si1);
        }

        [Test(
            Author = "Alex James Thomson",
            Description = "Tests if the sushort type functions correctly.",
            TestOf = typeof(sushort)
        )]
        [Category("Unit")]
        public void SUShortTest() {
            ushort i1 = 10, i2 = 25, i3 = 5, i4 = 50;
            sushort si1 = i1, si2 = i2, si3 = i3, si4 = i4;
            Assert.AreEqual(i1, si1);
            Assert.AreEqual(i2, si2);
            Assert.AreEqual(i3, si3);
            Assert.AreEqual(i4, si4);

            Assert.AreEqual(i1 + i2 + i3 + i4, (int)(si1 + si2 + si3 + si4));
            Assert.AreEqual(i4 - i3 - i2 - i1, (int)(si4 - si3 - si2 - si1));
            Assert.AreEqual(i1 * i2 * i3 * i4, (int)(si1 * si2 * si3 * si4));
            Assert.AreEqual(i4 / i1 + i3 / i2, (int)(si4 / si1 + si3 / si2));
            Assert.AreEqual(i1 ^ i2 ^ i3 ^ i4, (int)(si1 ^ si2 ^ si3 ^ si4));

            si1.value = i1;
            Assert.AreEqual(i1, si1);
            si1.value = i2;
            Assert.AreEqual(i2, si1);
            si1.value = i3;
            Assert.AreEqual(i3, si1);
            si1.value = i4;
            Assert.AreEqual(i4, si1);
        }

        [Test(
            Author = "Alex James Thomson",
            Description = "Tests if the sint type functions correctly.",
            TestOf = typeof(sint)
        )]
        [Category("Unit")]
        public void SIntTest() {
            int i1 = -100, i2 = 250, i3 = 500, i4 = 1000;
            sint si1 = i1, si2 = i2, si3 = i3, si4 = i4;
            Assert.AreEqual(i1, si1);
            Assert.AreEqual(i2, si2);
            Assert.AreEqual(i3, si3);
            Assert.AreEqual(i4, si4);

            Assert.AreEqual(i1 + i2 + i3 + i4, si1 + si2 + si3 + si4);
            Assert.AreEqual(i1 - i2 - i3 - i4, si1 - si2 - si3 - si4);
            Assert.AreEqual(i1 * i2 * i3 * i4, si1 * si2 * si3 * si4);
            Assert.AreEqual(i4 / i1 + i3 / i2, si4 / si1 + si3 / si2);
            Assert.AreEqual(i1 ^ i2 ^ i3 ^ i4, si1 ^ si2 ^ si3 ^ si4);

            si1.value = i1;
            Assert.AreEqual(i1, si1);
            si1.value = i2;
            Assert.AreEqual(i2, si1);
            si1.value = i3;
            Assert.AreEqual(i3, si1);
            si1.value = i4;
            Assert.AreEqual(i4, si1);
        }

        [Test(
            Author = "Alex James Thomson",
            Description = "Tests if the suint type functions correctly.",
            TestOf = typeof(suint)
        )]
        [Category("Unit")]
        public void SUIntTest() {
            uint i1 = 100, i2 = 250, i3 = 500, i4 = 1000;
            suint si1 = i1, si2 = i2, si3 = i3, si4 = i4;
            Assert.AreEqual(i1, si1);
            Assert.AreEqual(i2, si2);
            Assert.AreEqual(i3, si3);
            Assert.AreEqual(i4, si4);

            Assert.AreEqual(i1 + i2 + i3 + i4, si1 + si2 + si3 + si4);
            Assert.AreEqual(i4 - i3 - i2 - i1, si4 - si3 - si2 - si1);
            Assert.AreEqual(i1 * i2 * i3 * i4, si1 * si2 * si3 * si4);
            Assert.AreEqual(i4 / i1 + i3 / i2, si4 / si1 + si3 / si2);
            Assert.AreEqual(i1 ^ i2 ^ i3 ^ i4, si1 ^ si2 ^ si3 ^ si4);

            si1.value = i1;
            Assert.AreEqual(i1, si1);
            si1.value = i2;
            Assert.AreEqual(i2, si1);
            si1.value = i3;
            Assert.AreEqual(i3, si1);
            si1.value = i4;
            Assert.AreEqual(i4, si1);
        }

        [Test(
            Author = "Alex James Thomson",
            Description = "Tests if the slong type functions correctly.",
            TestOf = typeof(slong)
        )]
        [Category("Unit")]
        public void SLongTest() {
            long i1 = -100, i2 = 250, i3 = 500, i4 = 1000;
            slong si1 = i1, si2 = i2, si3 = i3, si4 = i4;
            Assert.AreEqual(i1, si1);
            Assert.AreEqual(i2, si2);
            Assert.AreEqual(i3, si3);
            Assert.AreEqual(i4, si4);

            Assert.AreEqual(i1 + i2 + i3 + i4, si1 + si2 + si3 + si4);
            Assert.AreEqual(i1 - i2 - i3 - i4, si1 - si2 - si3 - si4);
            Assert.AreEqual(i1 * i2 * i3 * i4, si1 * si2 * si3 * si4);
            Assert.AreEqual(i4 / i1 + i3 / i2, si4 / si1 + si3 / si2);
            Assert.AreEqual(i1 ^ i2 ^ i3 ^ i4, si1 ^ si2 ^ si3 ^ si4);

            si1.value = i1;
            Assert.AreEqual(i1, si1);
            si1.value = i2;
            Assert.AreEqual(i2, si1);
            si1.value = i3;
            Assert.AreEqual(i3, si1);
            si1.value = i4;
            Assert.AreEqual(i4, si1);
        }

        [Test(
            Author = "Alex James Thomson",
            Description = "Tests if the sulong type functions correctly.",
            TestOf = typeof(sulong)
        )]
        [Category("Unit")]
        public void SULongTest() {
            ulong i1 = 100, i2 = 250, i3 = 500, i4 = 1000;
            sulong si1 = i1, si2 = i2, si3 = i3, si4 = i4;
            Assert.AreEqual(i1, si1);
            Assert.AreEqual(i2, si2);
            Assert.AreEqual(i3, si3);
            Assert.AreEqual(i4, si4);

            Assert.AreEqual(i1 + i2 + i3 + i4, si1 + si2 + si3 + si4);
            Assert.AreEqual(i1 - i2 - i3 - i4, si1 - si2 - si3 - si4);
            Assert.AreEqual(i1 * i2 * i3 * i4, si1 * si2 * si3 * si4);
            Assert.AreEqual(i4 / i1 + i3 / i2, si4 / si1 + si3 / si2);
            Assert.AreEqual(i1 ^ i2 ^ i3 ^ i4, si1 ^ si2 ^ si3 ^ si4);

            si1.value = i1;
            Assert.AreEqual(i1, si1);
            si1.value = i2;
            Assert.AreEqual(i2, si1);
            si1.value = i3;
            Assert.AreEqual(i3, si1);
            si1.value = i4;
            Assert.AreEqual(i4, si1);
        }

        [Test(
            Author = "Alex James Thomson",
            Description = "Tests if the sfloat type functions correctly.",
            TestOf = typeof(sfloat)
        )]
        [Category("Unit")]
        public void SFloatTest() {
            float f1 = -100, f2 = 250, f3 = 500, f4 = 1000;
            sfloat sf1 = f1, sf2 = f2, sf3 = f3, sf4 = f4;
            Assert.AreEqual(f1, sf1);
            Assert.AreEqual(f2, sf2);
            Assert.AreEqual(f3, sf3);
            Assert.AreEqual(f4, sf4);

            Assert.AreEqual(f1 + f2 + f3 + f4, sf1 + sf2 + sf3 + sf4);
            Assert.AreEqual(f1 - f2 - f3 - f4, sf1 - sf2 - sf3 - sf4);
            Assert.AreEqual(f1 * f2 * f3 * f4, sf1 * sf2 * sf3 * sf4);
            Assert.AreEqual(f4 / f1 + f3 / f2, sf4 / sf1 + sf3 / sf2);

            sf1.value = f1;
            Assert.AreEqual(f1, sf1);
            sf1.value = f2;
            Assert.AreEqual(f2, sf1);
            sf1.value = f3;
            Assert.AreEqual(f3, sf1);
            sf1.value = f4;
            Assert.AreEqual(f4, sf1);
        }

        [Test(
            Author = "Alex James Thomson",
            Description = "Tests if the sdouble type functions correctly.",
            TestOf = typeof(sdouble)
        )]
        [Category("Unit")]
        public void SDoubleTest() {
            double f1 = -100, f2 = 250, f3 = 500, f4 = 1000;
            sdouble sf1 = f1, sf2 = f2, sf3 = f3, sf4 = f4;
            Assert.AreEqual(f1, sf1);
            Assert.AreEqual(f2, sf2);
            Assert.AreEqual(f3, sf3);
            Assert.AreEqual(f4, sf4);

            Assert.AreEqual(f1 + f2 + f3 + f4, sf1 + sf2 + sf3 + sf4);
            Assert.AreEqual(f1 - f2 - f3 - f4, sf1 - sf2 - sf3 - sf4);
            Assert.AreEqual(f1 * f2 * f3 * f4, sf1 * sf2 * sf3 * sf4);
            Assert.AreEqual(f4 / f1 + f3 / f2, sf4 / sf1 + sf3 / sf2);

            sf1.value = f1;
            Assert.AreEqual(f1, sf1);
            sf1.value = f2;
            Assert.AreEqual(f2, sf1);
            sf1.value = f3;
            Assert.AreEqual(f3, sf1);
            sf1.value = f4;
            Assert.AreEqual(f4, sf1);
        }

        [Test(
            Author = "Alex James Thomson",
            Description = "Tests if the sbool type functions correctly.",
            TestOf = typeof(sbool)
        )]
        [Category("Unit")]
        public void SBoolTest() {
            for (int i = 0; i < 256; i++) {
                sbool v = true;
                Assert.IsTrue(v);
                v = false;
                Assert.IsFalse(v);
                v = true;
                Assert.IsTrue(v);
                v = false;
                Assert.IsFalse(v);
            }
        }

    }

}