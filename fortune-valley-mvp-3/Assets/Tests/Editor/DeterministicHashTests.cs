using NUnit.Framework;
using FortuneValley.Core.Hashing;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class DeterministicHashTests
    {
        [Test]
        public void FromString_SameInput_ReturnsSameHash()
        {
            int a = DeterministicHash.FromString("Lot_Block01");
            int b = DeterministicHash.FromString("Lot_Block01");
            Assert.AreEqual(a, b);
        }

        [Test]
        public void FromString_DifferentInputs_ReturnDifferentHashes()
        {
            int a = DeterministicHash.FromString("Lot_Block01");
            int b = DeterministicHash.FromString("Lot_Block02");
            int c = DeterministicHash.FromString("Lot_Block19");
            Assert.AreNotEqual(a, b);
            Assert.AreNotEqual(b, c);
            Assert.AreNotEqual(a, c);
        }

        [Test]
        public void FromString_Null_ReturnsZero()
        {
            Assert.AreEqual(0, DeterministicHash.FromString(null));
        }

        [Test]
        public void FromString_EmptyString_ReturnsFnvOffsetBasis()
        {
            // Regression sentinel: locks the FNV-1a algorithm. If this test fails, every
            // existing block seed has shifted - re-seed all scenes.
            int expected = unchecked((int)2166136261u);
            Assert.AreEqual(expected, DeterministicHash.FromString(""));
        }

        [Test]
        public void FromString_SingleCharA_ReturnsKnownFnvVector()
        {
            // FNV-1a 32-bit("a") = 0xe40c292c, a published test vector from the
            // FNV reference (isthe.com/chongo/tech/comp/fnv). Locks the algorithm to
            // the canonical FNV-1a constants and byte-order.
            int expected = unchecked((int)0xe40c292cu);
            Assert.AreEqual(expected, DeterministicHash.FromString("a"));
        }
    }
}
