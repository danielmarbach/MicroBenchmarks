using BenchmarkDotNet.Attributes;

namespace MicroBenchmarks.ServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using Microsoft.Azure.Amqp;


[Config(typeof(Config))]
public class DataConversion
{
    private IEnumerable<ReadOnlyMemory<byte>> data;

    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.Default.WithInvocationCount(320000));
        }
    }

    [Params(1, 2, 4, 8, 16, 32, 64)]
    public int Elements { get; set; }

    [IterationSetup]
    public void Setup()
    {
        data = Enumerable.Range(0, Elements)
            .Select(i => (ReadOnlyMemory<byte>) Encoding.UTF8.GetBytes($"Hello World {i}"));
    }

    [Benchmark(Baseline = true)]
    public ArraySegment<byte>[] ToArray()
    {
        return ConvertBefore(data);
    }

    static ArraySegment<byte>[] ConvertBefore(IEnumerable<ReadOnlyMemory<byte>> allData)
    {
        return allData.Select(data => new ArraySegment<byte>(data.IsEmpty ? Array.Empty<byte>() : data.ToArray())).ToArray();
    }

    [Benchmark]
    public ArraySegment<byte>[] Marshal()
    {
        return ConvertAfter(data);
    }

    static ArraySegment<byte>[] ConvertAfter(IEnumerable<ReadOnlyMemory<byte>> allData)
    {
        return allData.Select(data =>
        {
            ArraySegment<byte> segment;
            if (!data.IsEmpty)
            {
                if (!MemoryMarshal.TryGetArray(data, out segment))
                {
                    segment = new ArraySegment<byte>(data.ToArray());
                }
            }
            else
            {
                segment = new ArraySegment<byte>(Array.Empty<byte>());
            }

            return segment;
        }).ToArray();
    }
}