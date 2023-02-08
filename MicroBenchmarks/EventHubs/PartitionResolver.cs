using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.EventHubs;

[Config(typeof(Config))]
public class PartitionResolver
{
    private string inputString;

    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.ShortRun);
        }
    }

    [Params(8, 12, 24, 32, 64, 128, 255, 257, 512)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        inputString = new string('a', Size);
    }

    [Benchmark(Baseline = true)]
    public short Before()
    {
        return PartitionResolverBefore.GenerateHashCode(inputString);
    }

    [Benchmark]
    public short After()
    {
        return PartitionResolverAfter.GenerateHashCode(inputString);
    }
}

public static class PartitionResolverBefore
{
    public static short GenerateHashCode(string partitionKey)
    {
        if (partitionKey == null)
        {
            return 0;
        }

        ComputeHash(Encoding.UTF8.GetBytes(partitionKey), seed1: 0, seed2: 0, out uint hash1, out uint hash2);
        return (short) (hash1 ^ hash2);
    }

    private static void ComputeHash(byte[] data,
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
            a += BitConverter.ToUInt32(data, index);
            b += BitConverter.ToUInt32(data, index + 4);
            c += BitConverter.ToUInt32(data, index + 8);

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
                a += BitConverter.ToUInt32(data, index);
                b += BitConverter.ToUInt32(data, index + 4);
                c += BitConverter.ToUInt32(data, index + 8);
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
                b += BitConverter.ToUInt32(data, index + 4);
                a += BitConverter.ToUInt32(data, index);
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
                a += BitConverter.ToUInt32(data, index);
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

public static class PartitionResolverAfter
{
    [SkipLocalsInit]
    public static short GenerateHashCode(string partitionKey)
    {
        if (partitionKey == null)
        {
            return 0;
        }

        const int MaxStackLimit = 256;
            
        byte[] sharedBuffer = null;
        var partitionKeySpan = partitionKey.AsSpan();
        var encoding = Encoding.UTF8;
        var partitionKeyByteLength = encoding.GetMaxByteCount(partitionKey.Length);
        var hashBuffer = partitionKeyByteLength <= MaxStackLimit
            ? stackalloc byte[MaxStackLimit]
            : sharedBuffer = ArrayPool<byte>.Shared.Rent(partitionKeyByteLength);

        var written = encoding.GetBytes(partitionKeySpan, hashBuffer);
        ComputeHash(hashBuffer[..written], seed1: 0, seed2: 0, out uint hash1, out uint hash2);
        if (sharedBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(sharedBuffer);
        }

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