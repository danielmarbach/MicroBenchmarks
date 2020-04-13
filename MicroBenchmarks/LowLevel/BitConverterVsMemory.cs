using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.LowLevel
{
    [Config(typeof(Config))]
    public class BitConverterVsMemory
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(MemoryDiagnoser.Default);
                Add(Job.ShortRun);
            }
        }

        [Params(1, 100, 1000, 10000)]
        public int Iterations { get; set; }

        [Benchmark(Baseline = true)]
        public void Converter()
        {
            int _deliveryCount = 0;
            for (var i = 0; i < Iterations; i++)
            {
                var deliveryTag = new ArraySegment<byte>(BitConverter.GetBytes(Interlocked.Increment(ref _deliveryCount)));
            }
        }

        [Benchmark]
        public void Mem()
        {
            int _deliveryCount = 0;

            for (var i = 0; i < Iterations; i++)
            {
                ArraySegment<byte> deliveryTag;
                using (var owner = MemoryPool<byte>.Shared.Rent(4))
                {
                    var memory = owner.Memory;
                    BinaryPrimitives.WriteInt32LittleEndian(memory.Span, Interlocked.Increment(ref _deliveryCount));
                    MemoryMarshal.TryGetArray(memory, out deliveryTag);
                }
            }
        }
    }
}