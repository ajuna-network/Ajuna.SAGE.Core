using Ajuna.SAGE.Core.Model;

namespace Ajuna.SAGE.Core.Test.Model
{
    public enum TestEnum : byte
    {
        None = 0,
        A = 1,
        B = 2,
        C = 3
    }

    [TestFixture]
    public class ByteArrayExtensionsTests
    {
        [Test]
        public void ToHexString_ReturnsCorrectHex()
        {
            byte[] bytes = new byte[] { 0x12, 0xAB, 0xCD, 0xEF };
            string hex = bytes.ToHexString();
            Assert.That(hex, Is.EqualTo("12ABCDEF"));
        }

        [Test]
        public void SetBit_SetsCorrectBit()
        {
            byte[] data = new byte[2]; // 16 bits, all initialized to 0.

            // Set bit 3 (in byte 0, LSB ordering)
            data.SetBit(3, true);
            // Verify: in byte 0, bit 3 should be 1.
            Assert.That((data[0] & (1 << 3)) != 0, Is.True);

            // Also verify that other bits remain unchanged (still 0)
            for (int i = 0; i < 8; i++)
            {
                if (i == 3) continue;
                Assert.That((data[0] & (1 << i)) == 0, Is.True);
            }
        }

        [Test]
        public void SetBit_ClearsCorrectBit()
        {
            byte[] data = new byte[1] { 0xFF }; // All bits set in the byte.

            // Clear bit 5.
            data.SetBit(5, false);
            // Verify: bit 5 is cleared.
            Assert.That((data[0] & (1 << 5)) == 0, Is.True);

            // Verify: other bits remain set.
            for (int i = 0; i < 8; i++)
            {
                if (i == 5) continue;
                Assert.That((data[0] & (1 << i)) != 0, Is.True);
            }
        }

        [Test]
        public void ReadBit_ReturnsTrueIfBitIsSet()
        {
            byte[] data = new byte[1];
            // Set bit 2.
            data.SetBit(2, true);
            bool result = data.ReadBit(2);
            Assert.That(result, Is.True);
        }

        [Test]
        public void ReadBit_ReturnsFalseIfBitIsCleared()
        {
            byte[] data = new byte[1] { 0xFF }; // All bits set.
            // Clear bit 7.
            data.SetBit(7, false);
            bool result = data.ReadBit(7);
            Assert.That(result, Is.False);
        }

        [Test]
        public void ReadBit_OutOfRange_ThrowsException()
        {
            byte[] data = new byte[1]; // 8 bits.
            Assert.Throws<ArgumentOutOfRangeException>(() => data.ReadBit(8));
        }

        [Test]
        public void SetBit_OutOfRange_ThrowsException()
        {
            byte[] data = new byte[1]; // 8 bits.
            Assert.Throws<ArgumentOutOfRangeException>(() => data.SetBit(-1, true));
        }

        [Test]
        public void SetAndReadBits_AcrossMultipleBytes()
        {
            byte[] data = new byte[3]; // 24 bits.
            // Set bit 10. Bit 10 is in the second byte (10/8 = 1) at position (10 % 8 = 2).
            data.SetBit(10, true);
            Assert.That(data.ReadBit(10), Is.True);

            // Also set bit 16 (in third byte, bit position 0)
            data.SetBit(16, true);
            Assert.That(data.ReadBit(16), Is.True);

            // Clear bit 10 and verify.
            data.SetBit(10, false);
            Assert.That(data.ReadBit(10), Is.False);
        }

        [Test]
        public void SetAndRead_Byte_LittleEndian()
        {
            byte[] data = new byte[10];
            byte expected = 0x42;
            data.Set<byte>(2, expected, littleEndian: true);
            byte actual = data.Read<byte>(2, littleEndian: true);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void SetAndRead_Byte_BigEndian()
        {
            byte[] data = new byte[10];
            byte expected = 0x7F;
            data.Set<byte>(3, expected, littleEndian: false);
            byte actual = data.Read<byte>(3, littleEndian: false);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void SetAndRead_UShort_LittleEndian()
        {
            byte[] data = new byte[10];
            ushort expected = 0x1234;
            data.Set<ushort>(1, expected, littleEndian: true);
            ushort actual = data.Read<ushort>(1, littleEndian: true);
            Assert.That(actual, Is.EqualTo(expected));

            // Validate underlying bytes (little-endian)
            byte[] expectedBytes = BitConverter.GetBytes(expected);
            Assert.That(data[1], Is.EqualTo(expectedBytes[0]));
            Assert.That(data[2], Is.EqualTo(expectedBytes[1]));
        }

        [Test]
        public void SetAndRead_UShort_BigEndian()
        {
            byte[] data = new byte[10];
            ushort expected = 0x1234;
            data.Set<ushort>(2, expected, littleEndian: false);
            ushort actual = data.Read<ushort>(2, littleEndian: false);
            Assert.That(actual, Is.EqualTo(expected));

            // Validate underlying bytes (big-endian)
            byte[] expectedBytes = BitConverter.GetBytes(expected);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(expectedBytes);
            Assert.That(data[2], Is.EqualTo(expectedBytes[0]));
            Assert.That(data[3], Is.EqualTo(expectedBytes[1]));
        }

        [Test]
        public void SetAndRead_UInt_LittleEndian()
        {
            byte[] data = new byte[12];
            uint expected = 0x12345678;
            data.Set<uint>(4, expected, littleEndian: true);
            uint actual = data.Read<uint>(4, littleEndian: true);
            Assert.That(actual, Is.EqualTo(expected));

            byte[] expectedBytes = BitConverter.GetBytes(expected);
            Assert.That(data[4], Is.EqualTo(expectedBytes[0]));
            Assert.That(data[5], Is.EqualTo(expectedBytes[1]));
            Assert.That(data[6], Is.EqualTo(expectedBytes[2]));
            Assert.That(data[7], Is.EqualTo(expectedBytes[3]));
        }

        [Test]
        public void SetAndRead_UInt_BigEndian()
        {
            byte[] data = new byte[12];
            uint expected = 0x89ABCDEF;
            data.Set<uint>(3, expected, littleEndian: false);
            uint actual = data.Read<uint>(3, littleEndian: false);
            Assert.That(actual, Is.EqualTo(expected));

            byte[] expectedBytes = BitConverter.GetBytes(expected);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(expectedBytes);
            Assert.That(data[3], Is.EqualTo(expectedBytes[0]));
            Assert.That(data[4], Is.EqualTo(expectedBytes[1]));
            Assert.That(data[5], Is.EqualTo(expectedBytes[2]));
            Assert.That(data[6], Is.EqualTo(expectedBytes[3]));
        }

        [Test]
        public void SetAndRead_ULong_LittleEndian()
        {
            byte[] data = new byte[20];
            ulong expected = 0x1122334455667788UL;
            data.Set<ulong>(5, expected, littleEndian: true);
            ulong actual = data.Read<ulong>(5, littleEndian: true);
            Assert.That(actual, Is.EqualTo(expected));

            byte[] expectedBytes = BitConverter.GetBytes(expected);
            Assert.That(data[5], Is.EqualTo(expectedBytes[0]));
            Assert.That(data[6], Is.EqualTo(expectedBytes[1]));
            Assert.That(data[7], Is.EqualTo(expectedBytes[2]));
            Assert.That(data[8], Is.EqualTo(expectedBytes[3]));
            Assert.That(data[9], Is.EqualTo(expectedBytes[4]));
            Assert.That(data[10], Is.EqualTo(expectedBytes[5]));
            Assert.That(data[11], Is.EqualTo(expectedBytes[6]));
            Assert.That(data[12], Is.EqualTo(expectedBytes[7]));
        }

        [Test]
        public void SetAndRead_ULong_BigEndian()
        {
            byte[] data = new byte[20];
            ulong expected = 0xAABBCCDDEEFF0011UL;
            data.Set<ulong>(7, expected, littleEndian: false);
            ulong actual = data.Read<ulong>(7, littleEndian: false);
            Assert.That(actual, Is.EqualTo(expected));

            byte[] expectedBytes = BitConverter.GetBytes(expected);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(expectedBytes);
            Assert.That(data[7], Is.EqualTo(expectedBytes[0]));
            Assert.That(data[8], Is.EqualTo(expectedBytes[1]));
            Assert.That(data[9], Is.EqualTo(expectedBytes[2]));
            Assert.That(data[10], Is.EqualTo(expectedBytes[3]));
            Assert.That(data[11], Is.EqualTo(expectedBytes[4]));
            Assert.That(data[12], Is.EqualTo(expectedBytes[5]));
            Assert.That(data[13], Is.EqualTo(expectedBytes[6]));
            Assert.That(data[14], Is.EqualTo(expectedBytes[7]));
        }

        [Test]
        public void SetAndRead_Enum_LittleEndian()
        {
            byte[] data = new byte[10];
            TestEnum expected = TestEnum.B;
            data.Set<TestEnum>(4, expected, littleEndian: true);
            TestEnum actual = data.Read<TestEnum>(4, littleEndian: true);
            Assert.That(actual, Is.EqualTo(expected));
            // Underlying byte should match
            Assert.That(data[4], Is.EqualTo((byte)expected));
        }

        [Test]
        public void SetAndRead_Enum_BigEndian()
        {
            byte[] data = new byte[10];
            TestEnum expected = TestEnum.A;
            data.Set<TestEnum>(6, expected, littleEndian: false);
            TestEnum actual = data.Read<TestEnum>(6, littleEndian: false);
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(data[6], Is.EqualTo((byte)expected));
        }

        [Test]
        public void OutOfBoundsWrite_ThrowsException()
        {
            byte[] data = new byte[4];
            // For ushort, size is 2; writing at pos = 3 would exceed the array.
            Assert.That(() => data.Set<ushort>(3, 0x1234, littleEndian: true), Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void OutOfBoundsRead_ThrowsException()
        {
            byte[] data = new byte[4];
            // For uint, size is 4; reading at pos = 1 exceeds the array bounds.
            Assert.That(() => data.Read<uint>(1, littleEndian: true), Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void UnsupportedType_ThrowsException()
        {
            byte[] data = new byte[10];
            // Using float should throw NotSupportedException.
            Assert.That(() => data.Set<float>(2, 3.14f, littleEndian: true), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => data.Read<float>(2, littleEndian: true), Throws.TypeOf<NotSupportedException>());
        }
    }
}