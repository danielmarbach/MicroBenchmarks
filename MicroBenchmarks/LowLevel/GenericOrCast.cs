using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.LowLevel
{
    [Config(typeof(Config))]
    public class GenericOrCast
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(new BenchmarkDotNet.Diagnosers.MemoryDiagnoser());
                Add(StatisticColumn.AllStatistics);
                Add(Job.Default.With(Platform.X64));
                Add(Job.Default.With(Platform.X86));
            }
        }

        [Benchmark(Baseline = true)]
        public void MethodBase()
        {
            Method(new ChildWork());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Method(Work work)
        {
            GC.KeepAlive(work);
        }

        [Benchmark]
        public void MethodGeneric()
        {
            GenericMethod(new ChildWork());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void GenericMethod<TWork>(TWork work)
            where TWork : Work
        {
            GC.KeepAlive(work);
        }

        class Work { }

        class ChildWork : Work { }
    }
}