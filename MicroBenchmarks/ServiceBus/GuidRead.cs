namespace MicroBenchmarks.ServiceBus;

using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;

[Config(typeof(Config))]
public class GuidRead
{
    private byte[] scratchBuffer;

    [IterationSetup]
    public void Setup()
    {
        scratchBuffer = Guid.NewGuid().ToByteArray();
    }

    [Benchmark(Baseline = true)]
    public Guid Primitives()
    {
        var scratchBufferSpan = scratchBuffer.AsSpan();
        var a = BinaryPrimitives.ReadInt32BigEndian(scratchBufferSpan);
        var b = BinaryPrimitives.ReadInt16BigEndian(scratchBufferSpan.Slice(4, 2));
        var c = BinaryPrimitives.ReadInt16BigEndian(scratchBufferSpan.Slice(6, 2));
        var pos = 0;
        var d = scratchBufferSpan[pos++];
        var e = scratchBufferSpan[pos++];
        var f = scratchBufferSpan[pos++];
        var g = scratchBufferSpan[pos++];
        var h = scratchBufferSpan[pos++];
        var i = scratchBufferSpan[pos++];
        var j = scratchBufferSpan[pos++];
        var k = scratchBufferSpan[pos++];
        return new Guid(a, b, c, d, e, f, g, h, i, j, k);
    }

    [Benchmark]
    public Guid MarshalBigEndian()
    {
        var bigEndianSpan = scratchBuffer.AsSpan();
        Span<byte> guidBytes = stackalloc byte[16];

        if (BitConverter.IsLittleEndian)
        {
            BinaryPrimitives.WriteInt32LittleEndian(guidBytes.Slice(0, 4),
                BinaryPrimitives.ReadInt32BigEndian(bigEndianSpan.Slice(0, 4)));

            BinaryPrimitives.WriteInt16LittleEndian(guidBytes.Slice(4, 2),
                BinaryPrimitives.ReadInt16BigEndian(bigEndianSpan.Slice(4, 2)));

            BinaryPrimitives.WriteInt16LittleEndian(guidBytes.Slice(6, 2),
                BinaryPrimitives.ReadInt16BigEndian(bigEndianSpan.Slice(6, 2)));

            bigEndianSpan.Slice(8, 8).CopyTo(guidBytes.Slice(8, 8));
        }
        else
        {
            bigEndianSpan.CopyTo(guidBytes);
        }

        // Now safe to read Guid directly from guidBytes buffer
        return MemoryMarshal.Read<Guid>(guidBytes);
    }

    private class Config : ManualConfig
    {
        public Config()
        {
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.Default.WithInvocationCount(96000000));
        }
    }
}