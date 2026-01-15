using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.EventHubs;

[Config(typeof(Config))]
public class ComputeHash
{
    private byte[] inputBytes;

    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.Default);
        }
    }

    [Params(8, 12, 24, 32, 64, 128, 255)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        inputBytes = Encoding.UTF8.GetBytes(new string('a', Size));
    }

    [Benchmark(Baseline = true)]
    public short Current()
    {
        return ComputeHashBefore.GenerateHashCode(inputBytes);
    }

    [Benchmark]
    public short V1()
    {
        return ComputeHashV1.GenerateHashCode(inputBytes);
    }

    [Benchmark]
    public short V2()
    {
        return ComputeHashV2.GenerateHashCode(inputBytes);
    }
}

public static class ComputeHashBefore
{
    public static short GenerateHashCode(byte[] partitionKey)
    {
        ComputeHash(partitionKey, seed1: 0, seed2: 0, out uint hash1, out uint hash2);
        return (short) (hash1 ^ hash2);
    }

    private static void ComputeHash(ReadOnlySpan<byte> data,
        uint seed1,
        uint seed2,
        out uint hash1,
        out uint hash2)
    {
                uint a, b, c;

        a = b = c = (uint) (0xdeadbeef + data.Length + seed1);
        c += seed2;

        int index = 0, size = data.Length;
        while (size > 12)
        {
            a += BinaryPrimitives.ReadUInt32LittleEndian(data[index..]);
            b += BinaryPrimitives.ReadUInt32LittleEndian(data[(index + 4)..]);
            c += BinaryPrimitives.ReadUInt32LittleEndian(data[(index + 8)..]);

            a -= c;
            a ^= (c << 4) | (c >> 28);
            c += b;

            b -= a;
            b ^= (a << 6) | (a >> 26);
            a += c;

            c -= b;
            c ^= (b << 8) | (b >> 24);
            b += a;

            a -= c;
            a ^= (c << 16) | (c >> 16);
            c += b;

            b -= a;
            b ^= (a << 19) | (a >> 13);
            a += c;

            c -= b;
            c ^= (b << 4) | (b >> 28);
            b += a;

            index += 12;
            size -= 12;
        }

        switch (size)
        {
            case 12:
                a += BinaryPrimitives.ReadUInt32LittleEndian(data[index..]);
                b += BinaryPrimitives.ReadUInt32LittleEndian(data[(index + 4)..]);
                c += BinaryPrimitives.ReadUInt32LittleEndian(data[(index + 8)..]);
                break;
            case 11:
                c += ((uint) data[index + 10]) << 16;
                goto case 10;
            case 10:
                c += ((uint) data[index + 9]) << 8;
                goto case 9;
            case 9:
                c += (uint) data[index + 8];
                goto case 8;
            case 8:
                b += BinaryPrimitives.ReadUInt32LittleEndian(data[(index + 4)..]);
                a += BinaryPrimitives.ReadUInt32LittleEndian(data[index..]);
                break;
            case 7:
                b += ((uint) data[index + 6]) << 16;
                goto case 6;
            case 6:
                b += ((uint) data[index + 5]) << 8;
                goto case 5;
            case 5:
                b += (uint) data[index + 4];
                goto case 4;
            case 4:
                a += BinaryPrimitives.ReadUInt32LittleEndian(data[index..]);
                break;
            case 3:
                a += ((uint) data[index + 2]) << 16;
                goto case 2;
            case 2:
                a += ((uint) data[index + 1]) << 8;
                goto case 1;
            case 1:
                a += (uint) data[index];
                break;
            case 0:
                hash1 = c;
                hash2 = b;
                return;
        }

        c ^= b;
        c -= (b << 14) | (b >> 18);

        a ^= c;
        a -= (c << 11) | (c >> 21);

        b ^= a;
        b -= (a << 25) | (a >> 7);

        c ^= b;
        c -= (b << 16) | (b >> 16);

        a ^= c;
        a -= (c << 4) | (c >> 28);

        b ^= a;
        b -= (a << 14) | (a >> 18);

        c ^= b;
        c -= (b << 24) | (b >> 8);

        hash1 = c;
        hash2 = b;
    }
}

public static class ComputeHashV1
{
    public static short GenerateHashCode(byte[] partitionKey)
    {
        ComputeHash(partitionKey, seed1: 0, seed2: 0, out uint hash1, out uint hash2);
        return (short) (hash1 ^ hash2);
    }

    private static void ComputeHash(ReadOnlySpan<byte> data,
        uint seed1,
        uint seed2,
        out uint hash1,
        out uint hash2)
    {
        uint len = (uint)data.Length;
        uint a = 0xDEADBEEF + len + seed1;
        uint b = a;
        uint c = a + seed2;

        int tripletCount = data.Length > 12 ? (data.Length - 1) / 12 : 0;

        int regionBytes = tripletCount * 12; // must be divisible by 4
        ReadOnlySpan<byte> region = data.Slice(0, regionBytes);
        ReadOnlySpan<uint> words = MemoryMarshal.Cast<byte, uint>(region);

        int i = 0;
        for (; i < tripletCount; i++)
        {
            int idx = i * 3;
            uint w0 = BitConverter.IsLittleEndian ? words[idx] : BinaryPrimitives.ReverseEndianness(words[idx]);
            uint w1 = BitConverter.IsLittleEndian
                ? words[idx + 1]
                : BinaryPrimitives.ReverseEndianness(words[idx + 1]);
            uint w2 = BitConverter.IsLittleEndian
                ? words[idx + 2]
                : BinaryPrimitives.ReverseEndianness(words[idx + 2]);

            a += w0;
            b += w1;
            c += w2;

            a -= c;
            a ^= (c << 4) | (c >> 28);
            c += b;

            b -= a;
            b ^= (a << 6) | (a >> 26);
            a += c;

            c -= b;
            c ^= (b << 8) | (b >> 24);
            b += a;

            a -= c;
            a ^= (c << 16) | (c >> 16);
            c += b;

            b -= a;
            b ^= (a << 19) | (a >> 13);
            a += c;

            c -= b;
            c ^= (b << 4) | (b >> 28);
            b += a;
        }

        int byteIndex = regionBytes;
        int size = data.Length - byteIndex;
        switch (size)
        {
            case 12:
                a += BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(byteIndex));
                b += BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(byteIndex + 4));
                c += BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(byteIndex + 8));
                break;
            case 11:
                c += (uint)data[byteIndex + 10] << 16;
                goto case 10;
            case 10:
                c += (uint)data[byteIndex + 9] << 8;
                goto case 9;
            case 9:
                c += data[byteIndex + 8];
                goto case 8;
            case 8:
                b += BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(byteIndex + 4));
                a += BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(byteIndex));
                break;
            case 7:
                b += (uint)data[byteIndex + 6] << 16;
                goto case 6;
            case 6:
                b += (uint)data[byteIndex + 5] << 8;
                goto case 5;
            case 5:
                b += data[byteIndex + 4];
                goto case 4;
            case 4:
                a += BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(byteIndex));
                break;
            case 3:
                a += (uint)data[byteIndex + 2] << 16;
                goto case 2;
            case 2:
                a += (uint)data[byteIndex + 1] << 8;
                goto case 1;
            case 1:
                a += data[byteIndex];
                break;
            case 0:
                hash1 = c;
                hash2 = b;
                return;
        }

        c ^= b;
        c -= (b << 14) | (b >> 18);

        a ^= c;
        a -= (c << 11) | (c >> 21);

        b ^= a;
        b -= (a << 25) | (a >> 7);

        c ^= b;
        c -= (b << 16) | (b >> 16);

        a ^= c;
        a -= (c << 4) | (c >> 28);

        b ^= a;
        b -= (a << 14) | (a >> 18);

        c ^= b;
        c -= (b << 24) | (b >> 8);

        hash1 = c;
        hash2 = b;
    }
}

public static class ComputeHashV2
{
    public static short GenerateHashCode(byte[] partitionKey)
    {
        ComputeHash(partitionKey, seed1: 0, seed2: 0, out uint hash1, out uint hash2);
        return (short)(hash1 ^ hash2);
    }

    private static void ComputeHash(ReadOnlySpan<byte> data,
        uint seed1,
        uint seed2,
        out uint hash1,
        out uint hash2)
    {
        uint len = (uint)data.Length;
        uint a = 0xDEADBEEF + len + seed1;
        uint b = a;
        uint c = a + seed2;

        int chunks = data.Length > 12 ? (data.Length - 1) / 12 : 0;

        ref byte ptr = ref MemoryMarshal.GetReference(data);
        for (int i = 0; i < chunks; i++)
        {
            uint w0 = Unsafe.ReadUnaligned<uint>(ref ptr);
            uint w1 = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref ptr, 4));
            uint w2 = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref ptr, 8));
            ptr = ref Unsafe.Add(ref ptr, 12);

            if (!BitConverter.IsLittleEndian)
            {
                w0 = BinaryPrimitives.ReverseEndianness(w0);
                w1 = BinaryPrimitives.ReverseEndianness(w1);
                w2 = BinaryPrimitives.ReverseEndianness(w2);
            }

            a += w0;
            b += w1;
            c += w2;

            a -= c;
            a ^= (c << 4) | (c >> 28);
            c += b;

            b -= a;
            b ^= (a << 6) | (a >> 26);
            a += c;

            c -= b;
            c ^= (b << 8) | (b >> 24);
            b += a;

            a -= c;
            a ^= (c << 16) | (c >> 16);
            c += b;

            b -= a;
            b ^= (a << 19) | (a >> 13);
            a += c;

            c -= b;
            c ^= (b << 4) | (b >> 28);
            b += a;
        }

        int consumed = chunks * 12;
        ref byte tail = ref Unsafe.Add(ref MemoryMarshal.GetReference(data), consumed);
        int left = data.Length - consumed;
        switch (left)
        {
            case 12:
                a += BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<uint>(ref tail)
                    : BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref tail));
                b += BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref tail, 4))
                    : BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref tail, 4)));
                c += BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref tail, 8))
                    : BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref tail, 8)));
                break;
            case 11:
                c += (uint)Unsafe.Add(ref tail, 10) << 16;
                goto case 10;
            case 10:
                c += (uint)Unsafe.Add(ref tail, 9) << 8;
                goto case 9;
            case 9:
                c += Unsafe.Add(ref tail, 8);
                goto case 8;
            case 8:
                b += BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref tail, 4))
                    : BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref tail, 4)));
                a += BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<uint>(ref tail)
                    : BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref tail));
                break;
            case 7:
                b += (uint)Unsafe.Add(ref tail, 6) << 16;
                goto case 6;
            case 6:
                b += (uint)Unsafe.Add(ref tail, 5) << 8;
                goto case 5;
            case 5:
                b += Unsafe.Add(ref tail, 4);
                goto case 4;
            case 4:
                a += BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<uint>(ref tail)
                    : BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref tail));
                break;
            case 3:
                a += (uint)Unsafe.Add(ref tail, 2) << 16;
                goto case 2;
            case 2:
                a += (uint)Unsafe.Add(ref tail, 1) << 8;
                goto case 1;
            case 1:
                a += Unsafe.Add(ref tail, 0);
                break;
            case 0:
                hash1 = c;
                hash2 = b;
                return;
        }

        c ^= b;
        c -= (b << 14) | (b >> 18);

        a ^= c;
        a -= (c << 11) | (c >> 21);

        b ^= a;
        b -= (a << 25) | (a >> 7);

        c ^= b;
        c -= (b << 16) | (b >> 16);

        a ^= c;
        a -= (c << 4) | (c >> 28);

        b ^= a;
        b -= (a << 14) | (a >> 18);

        c ^= b;
        c -= (b << 24) | (b >> 8);

        hash1 = c;
        hash2 = b;
    }
}