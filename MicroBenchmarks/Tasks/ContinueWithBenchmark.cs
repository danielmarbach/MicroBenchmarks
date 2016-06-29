using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using System;
using System.Threading.Tasks;

namespace MicroBenchmarks.Tasks
{
    [Config(typeof(Config))]
    public class ContinueWithBenchmarks
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(new MemoryDiagnoser());
            }
        }

        private object willRequireClosure = new object();

        [Benchmark(Baseline = true)]
        public Task ContinueWith()
        {
            return Task.CompletedTask
                .ContinueWith(
                    (_, state) =>
                    {
                        GC.KeepAlive(state);
                    }, willRequireClosure);
        }

        [Benchmark]
        public Task ContinueWithClosure()
        {
            return Task.CompletedTask
                .ContinueWith(
                    _ =>
                    {
                        GC.KeepAlive(willRequireClosure);
                    });
        }
    }
}
