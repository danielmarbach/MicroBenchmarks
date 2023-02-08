using BenchmarkDotNet.Attributes;

namespace MicroBenchmarks.ServiceBus;

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

[Config(typeof(Config))]
public class GuidWrite
{
    private Guid data;
    private byte[] scratchBuffer;

    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.Default.WithInvocationCount(9600000));
        }
    }
    [IterationSetup]
    public void Setup()
    {
        data = Guid.NewGuid();
        scratchBuffer = new byte[16];
    }

    [Benchmark(Baseline = true)]
    public Guid Unsafe()
    {
        WriteUuid(scratchBuffer, data);
        return data;
    }

    static unsafe void WriteUuid(byte[] buffer, Guid data)
    {
        fixed (byte* d = &buffer[0])
        {
            byte* p = (byte*)&data;
            d[0] = p[3];
            d[1] = p[2];
            d[2] = p[1];
            d[3] = p[0];

            d[4] = p[5];
            d[5] = p[4];

            d[6] = p[7];
            d[7] = p[6];

            *((ulong*)&d[8]) = *((ulong*)&p[8]);
        }
    }

    [Benchmark]
    public Guid Marshal()
    {
        return WriteUuidMarshal(scratchBuffer, data);
    }

    private static Guid WriteUuidMarshal(byte[] buffer, Guid data)
    {
        var bufferAsSpan = buffer.AsSpan();
        MemoryMarshal.Write(bufferAsSpan, ref data);
        Swap(bufferAsSpan, 0, 3);
        Swap(bufferAsSpan, 1, 2);
        Swap(bufferAsSpan, 4, 5);
        Swap(bufferAsSpan, 6, 7);
        return data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Swap(Span<byte> array, int index1, int index2)
    {
        var temp = array[index1];
        array[index1] = array[index2];
        array[index2] = temp;
    }

    [Benchmark]
    public Guid Span()
    {
        WriteUuidSpan(scratchBuffer, data);
        return data;
    }

    // like AMQP does it
    static void WriteUuidSpan(byte[] buffer, Guid data)
    {
        var destination = buffer.AsSpan();

        GuidData guidData = System.Runtime.CompilerServices.Unsafe.As<Guid, GuidData>(ref data);

        destination[15] = guidData._k; // hoist bounds checks
        BinaryPrimitives.WriteInt32BigEndian(destination, guidData._a);
        BinaryPrimitives.WriteInt16BigEndian(destination[4..], guidData._b);
        BinaryPrimitives.WriteInt16BigEndian(destination[6..], guidData._c);
        destination[8] = guidData._d;
        destination[9] = guidData._e;
        destination[10] = guidData._f;
        destination[11] = guidData._g;
        destination[12] = guidData._h;
        destination[13] = guidData._i;
        destination[14] = guidData._j;
    }

    readonly struct GuidData
    {
        public readonly int         _a;
        public readonly short       _b;
        public readonly short       _c;
        public readonly byte       _d;
        public readonly byte       _e;
        public readonly byte       _f;
        public readonly byte       _g;
        public readonly byte       _h;
        public readonly byte       _i;
        public readonly byte       _j;
        public readonly byte       _k;

        public GuidData(uint a, ushort b, ushort c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k)
        {
            _a = (int) a;
            _b = (short) b;
            _c = (short) c;
            _d = d;
            _e = e;
            _f = f;
            _g = g;
            _h = h;
            _i = i;
            _j = j;
            _k = k;
        }
    }
}