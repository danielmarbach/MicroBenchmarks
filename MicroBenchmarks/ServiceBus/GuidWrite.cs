
namespace MicroBenchmarks.ServiceBus;

using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
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
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.Default.WithInvocationCount(96000000));
        }
    }
    [IterationSetup]
    public void Setup()
    {
        data = Guid.NewGuid();
        scratchBuffer = new byte[16];
    }

    [Benchmark]
    public Guid Marshal()
    {
        var span = scratchBuffer.AsSpan();
        Span<byte> guidBytes = stackalloc byte[16];
        MemoryMarshal.Write(guidBytes, in data);

        if (BitConverter.IsLittleEndian)
        {
            BinaryPrimitives.WriteInt32BigEndian(span.Slice(0, 4),
                BinaryPrimitives.ReadInt32LittleEndian(guidBytes.Slice(0, 4)));

            BinaryPrimitives.WriteInt16BigEndian(span.Slice(4, 2),
                BinaryPrimitives.ReadInt16LittleEndian(guidBytes.Slice(4, 2)));

            BinaryPrimitives.WriteInt16BigEndian(span.Slice(6, 2),
                BinaryPrimitives.ReadInt16LittleEndian(guidBytes.Slice(6, 2)));

            guidBytes.Slice(8, 8).CopyTo(span.Slice(8, 8));
        }
        else
        {
            guidBytes.CopyTo(span);
        }

        return new Guid(span);
    }

    [Benchmark(Baseline = true)]
    public Guid Unsafe()
    {
        var span = scratchBuffer.AsSpan();
        WriteGuidToSpan(span, data);

        return new Guid(span);
    }

    private static unsafe void WriteGuidToSpan(Span<byte> span, Guid data)
    {
        byte* p = (byte*)&data;

        BinaryPrimitives.WriteInt32BigEndian(span, *((int*)p));
        BinaryPrimitives.WriteInt16BigEndian(span.Slice(4, 2), *((short*)(p + 4)));
        BinaryPrimitives.WriteInt16BigEndian(span.Slice(6, 2), *((short*)(p + 6)));
        for (var i = 0; i < 8; i++)
        {
            span[i] = *(p + i);
        }
    }
}