using System;
using System.Buffers;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.ServiceBus
{
    [Config(typeof(Config))]
    public class GuidByteArrayVsRent
    {
        private Consumer consumer = new Consumer();

        private class Config : ManualConfig
        {
            public Config()
            {
                AddExporter(MarkdownExporter.GitHub);
                AddDiagnoser(MemoryDiagnoser.Default);
                AddJob(Job.ShortRun);
            }
        }

        [Params(1, 2, 4, 8, 16, 32, 64)]
        public int Elements { get; set; }

        [Benchmark(Baseline = true)]
        public void ToByteArray()
        {
            for (var i = 0; i < Elements; i++)
            {
                consumer.Consume(new ArraySegment<byte>(Guid.NewGuid().ToByteArray()));
            }
        }

        [Benchmark]
        public void Marshal()
        {
            for (var i = 0; i < Elements; i++)
            {
                var bufferForGuid = ArrayPool<byte>.Shared.Rent(16);
                try
                {
                    var guid = Guid.NewGuid();
                    if (!MemoryMarshal.TryWrite(bufferForGuid, ref guid))
                    {
                        guid.ToByteArray().AsSpan().CopyTo(bufferForGuid);
                    }
                    consumer.Consume( new ArraySegment<byte>(bufferForGuid, 0, 16));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(bufferForGuid);
                }
            }
        }

        [Benchmark]
        public void RentWholeArray()
        {
            var bufferForGuid = ArrayPool<byte>.Shared.Rent(16*Elements);
            try
            {
                for (var i = 0; i < Elements; i++)
                {
                    var guid = Guid.NewGuid();
                    if (!MemoryMarshal.TryWrite(bufferForGuid.AsSpan(i * 16, 16), ref guid))
                    {
                        guid.ToByteArray().AsSpan().CopyTo(bufferForGuid);
                    }
                    consumer.Consume( new ArraySegment<byte>(bufferForGuid, i * 16, 16));
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bufferForGuid);
            }
        }

        // [Benchmark]
        // public void TryWriteBytes()
        // {
        //     for (var i = 0; i < Elements; i++)
        //     {
        //         var bufferForGuid = ArrayPool<byte>.Shared.Rent(16);
        //         try
        //         {
        //             var guid = Guid.NewGuid();
        //             if (!guid.TryWriteBytes(bufferForGuid))
        //             {
        //                 guid.ToByteArray().AsSpan().CopyTo(bufferForGuid);
        //             }
        //             consumer.Consume( new ArraySegment<byte>(bufferForGuid, 0, 16));
        //         }
        //         finally
        //         {
        //             ArrayPool<byte>.Shared.Return(bufferForGuid);
        //         }
        //     }
        // }
    }
}