using BenchmarkDotNet.Attributes;

namespace MicroBenchmarks.ServiceBus;

using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

[Config(typeof(Config))]
public class GuidConversation
{
    private ArraySegment<byte> data;

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
        data = new ArraySegment<byte>(Guid.NewGuid().ToByteArray());
    }

    [Benchmark(Baseline = true)]
    public Guid BufferAndBlockCopy()
    {
        var guidBuffer = new byte[16];
        Buffer.BlockCopy(data.Array, data.Offset, guidBuffer, 0, 16);
        return new Guid(guidBuffer);
    }

    [Benchmark]
    public Guid SpanAndMemoryMarshal()
    {
        Span<byte> guidBytes = stackalloc byte[16];
        data.AsSpan().CopyTo(guidBytes);
        if (!MemoryMarshal.TryRead<Guid>(guidBytes, out var lockTokenGuid))
        {
            lockTokenGuid = new Guid(guidBytes.ToArray());
        }
        return lockTokenGuid;
    }
}