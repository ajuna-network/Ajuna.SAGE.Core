using System;
using System.Buffers.Binary;

namespace Ajuna.SAGE.Core.Model
{
    public static class ByteArrayExtensions
    {
        /// <summary>
        /// Converts a byte array into a hexadecimal string.
        /// </summary>
        public static string ToHexString(this byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        /// <summary>
        /// Sets the bit at the specified absolute bit index in the byte array.
        /// Bit index 0 corresponds to the least‑significant bit of byte 0.
        /// </summary>
        /// <param name="bytes">The target byte array.</param>
        /// <param name="bitIndex">The absolute bit index (0..(bytes.Length*8)-1).</param>
        /// <param name="value">True to set the bit; false to clear it.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the bit index is less than 0 or beyond the array's bounds.
        /// </exception>
        public static void SetBit(this byte[] bytes, int bitIndex, bool value)
        {
            if (bitIndex < 0 || bitIndex >= bytes.Length * 8)
            {
                throw new ArgumentOutOfRangeException(nameof(bitIndex), "Bit index is out of range.");
            }

            int byteIndex = bitIndex / 8;
            int bitPosition = bitIndex % 8; // Assuming LSB is bit 0.

            if (value)
            {
                bytes[byteIndex] |= (byte)(1 << bitPosition);
            }
            else
            {
                bytes[byteIndex] &= (byte)~(1 << bitPosition);
            }
        }

        /// <summary>
        /// Reads the bit at the specified absolute bit index in the byte array.
        /// Bit index 0 corresponds to the least‑significant bit of byte 0.
        /// </summary>
        /// <param name="bytes">The source byte array.</param>
        /// <param name="bitIndex">The absolute bit index (0..(bytes.Length*8)-1).</param>
        /// <returns>True if the bit is set; otherwise, false.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the bit index is less than 0 or beyond the array's bounds.
        /// </exception>
        public static bool ReadBit(this byte[] bytes, int bitIndex)
        {
            if (bitIndex < 0 || bitIndex >= bytes.Length * 8)
            {
                throw new ArgumentOutOfRangeException(nameof(bitIndex), "Bit index is out of range.");
            }

            int byteIndex = bitIndex / 8;
            int bitPosition = bitIndex % 8; // Assuming LSB is bit 0.
            return (bytes[byteIndex] & (1 << bitPosition)) != 0;
        }

        /// <summary>
        /// Reads a block of bytes from the array starting at the given position.
        /// </summary>
        /// <param name="bytes">The source byte array.</param>
        /// <param name="pos">The starting position.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>A new byte array containing the requested block.</returns>
        public static byte[] Read(this byte[] bytes, int pos, int length)
        {
            if (pos < 0 || pos + length > bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(pos), "Out of bounds position.");
            }

            byte[] result = new byte[length];
            Array.Copy(bytes, pos, result, 0, length);
            return result;
        }

        /// <summary>
        /// Writes a block of bytes into the array starting at the given position.
        /// </summary>
        /// <param name="bytes">The target byte array.</param>
        /// <param name="pos">The starting position.</param>
        /// <param name="value">The block of bytes to write.</param>
        public static void Set(this byte[] bytes, int pos, byte[] value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (pos < 0 || pos + value.Length > bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(pos), "Out of bounds position.");
            }

            Array.Copy(value, 0, bytes, pos, value.Length);
        }

        /// <summary>
        /// Writes a value of type T into the byte array starting at the given position.
        /// Supports types: byte, ushort, uint, ulong, and enums (only if their underlying type is byte).
        /// Data is written in little‑endian order by default unless <paramref name="littleEndian"/> is false.
        /// </summary>
        public static void Set<T>(this byte[] bytes, int pos, T value, bool littleEndian = true) where T : struct
        {
            int size = GetSizeOfType<T>();
            if (pos < 0 || pos + size > bytes.Length)
            {
                throw new NotSupportedException("Out of bounds position.");
            }

            if (typeof(T).IsEnum)
            {
                bytes[pos] = (byte)(object)value;
            }
            else if (typeof(T) == typeof(byte))
            {
                bytes[pos] = (byte)(object)value;
            }
            else if (typeof(T) == typeof(ushort))
            {
                if (littleEndian)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(new Span<byte>(bytes, pos, 2), (ushort)(object)value);
                }
                else
                {
                    BinaryPrimitives.WriteUInt16BigEndian(new Span<byte>(bytes, pos, 2), (ushort)(object)value);
                }
            }
            else if (typeof(T) == typeof(uint))
            {
                if (littleEndian)
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(bytes, pos, 4), (uint)(object)value);
                }
                else
                {
                    BinaryPrimitives.WriteUInt32BigEndian(new Span<byte>(bytes, pos, 4), (uint)(object)value);
                }
            }
            else if (typeof(T) == typeof(ulong))
            {
                if (littleEndian)
                {
                    BinaryPrimitives.WriteUInt64LittleEndian(new Span<byte>(bytes, pos, 8), (ulong)(object)value);
                }
                else
                {
                    BinaryPrimitives.WriteUInt64BigEndian(new Span<byte>(bytes, pos, 8), (ulong)(object)value);
                }
            }
            else
            {
                throw new NotSupportedException($"Type {typeof(T).Name} is not supported.");
            }
        }

        /// <summary>
        /// Reads a value of type T from the byte array starting at the given position.
        /// Supports types: byte, ushort, uint, ulong, and enums (only if their underlying type is byte).
        /// Data is read assuming little‑endian order by default unless <paramref name="littleEndian"/> is false.
        /// </summary>
        public static T Read<T>(this byte[] bytes, int pos, bool littleEndian = true) where T : struct
        {
            int size = GetSizeOfType<T>();
            if (pos < 0 || pos + size > bytes.Length)
            {
                throw new NotSupportedException("Out of bounds position.");
            }

            if (typeof(T).IsEnum)
            {
                byte val = bytes[pos];
                return (T)Enum.ToObject(typeof(T), val);
            }
            else if (typeof(T) == typeof(byte))
            {
                return (T)(object)bytes[pos];
            }
            else if (typeof(T) == typeof(ushort))
            {
                ushort val = littleEndian
                    ? BinaryPrimitives.ReadUInt16LittleEndian(new ReadOnlySpan<byte>(bytes, pos, 2))
                    : BinaryPrimitives.ReadUInt16BigEndian(new ReadOnlySpan<byte>(bytes, pos, 2));
                return (T)(object)val;
            }
            else if (typeof(T) == typeof(uint))
            {
                uint val = littleEndian
                    ? BinaryPrimitives.ReadUInt32LittleEndian(new ReadOnlySpan<byte>(bytes, pos, 4))
                    : BinaryPrimitives.ReadUInt32BigEndian(new ReadOnlySpan<byte>(bytes, pos, 4));
                return (T)(object)val;
            }
            else if (typeof(T) == typeof(ulong))
            {
                ulong val = littleEndian
                    ? BinaryPrimitives.ReadUInt64LittleEndian(new ReadOnlySpan<byte>(bytes, pos, 8))
                    : BinaryPrimitives.ReadUInt64BigEndian(new ReadOnlySpan<byte>(bytes, pos, 8));
                return (T)(object)val;
            }
            else
            {
                throw new NotSupportedException($"Type {typeof(T).Name} is not supported.");
            }
        }

        /// <summary>
        /// Returns the number of bytes required for type T.
        /// If T is an enum, only the byte underlying type is supported.
        /// </summary>
        private static int GetSizeOfType<T>() where T : struct
        {
            if (typeof(T) == typeof(byte))
            {
                return 1;
            }

            if (typeof(T) == typeof(ushort))
            {
                return 2;
            }

            if (typeof(T) == typeof(uint))
            {
                return 4;
            }

            if (typeof(T) == typeof(ulong))
            {
                return 8;
            }

            if (typeof(T).IsEnum)
            {
                return 1;
            }

            throw new NotSupportedException($"Type {typeof(T).Name} is not supported.");
        }
    }
}