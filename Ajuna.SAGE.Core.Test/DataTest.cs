using Ajuna.SAGE.Core.Model;

namespace Ajuna.SAGE.Core.Test
{
    public class DataTest
    {
        public const int DATA_SIZE = 32;

        [Test]
        public void SetTest()
        {
            var dna = new byte[DATA_SIZE];

            Assert.That(dna, Is.Not.Null);
            Assert.That(dna.ToHexString, Has.Length.EqualTo(64)); 
            Assert.That(dna.ToHexString, Is.EqualTo("0000000000000000000000000000000000000000000000000000000000000000"));

            dna.Set<byte>(0, 1);
            Assert.That(dna.ToHexString, Is.EqualTo("0100000000000000000000000000000000000000000000000000000000000000"));

            dna.Set<byte>(0, 2);
            Assert.That(dna.ToHexString, Is.EqualTo("0200000000000000000000000000000000000000000000000000000000000000"));

            dna.Set<byte>(0, 255);
            Assert.That(dna.ToHexString, Is.EqualTo("FF00000000000000000000000000000000000000000000000000000000000000"));
        }

        [Test]
        public void ReadTest()
        {
            var dna = Utils.HexToBytes("1234000000000000000000000000000000000000000000000000000000000000");

            Assert.That(dna, Is.Not.Null);
            Assert.That(dna.ToHexString, Has.Length.EqualTo(64));
            Assert.That(dna.ToHexString, Is.EqualTo("1234000000000000000000000000000000000000000000000000000000000000"));

            var result2 = dna.Read<byte>(0);
            Assert.That(result2, !Is.Null);
            Assert.That(result2, Is.EqualTo(18));
        }
    }
}