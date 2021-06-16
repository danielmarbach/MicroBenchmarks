namespace MicroBenchmarks.ServiceBus
{
    using System;
    using System.Runtime.InteropServices;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Exporters;
    using BenchmarkDotNet.Jobs;

    [Config(typeof(Config))]
    public class GuidRead
    {
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
            scratchBuffer = Guid.NewGuid().ToByteArray();
        }

        [Benchmark(Baseline = true)]
        public Guid Unsafe()
        {
            return ReadUuid(scratchBuffer);
        }

        static unsafe Guid ReadUuid(byte[] buffer)
        {
            Guid data;
            fixed (byte* p = &buffer[0])
            {
                byte* d = (byte*)&data;
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

            return data;
        }

        [Benchmark]
        public Guid Marshal()
        {
            return MemoryMarshal.Read<Guid>(scratchBuffer.AsSpan());
        }
    }
}