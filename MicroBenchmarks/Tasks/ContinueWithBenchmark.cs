using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace MicroBenchmarks.Tasks;

[Config(typeof(Config))]
public class ContinueWithBenchmarks
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddColumn(StatisticColumn.AllStatistics);
        }
    }

    private State willRequireClosure = new State();

    [Benchmark(Baseline = true)]
    public Task ContinueWith()
    {
        return Task.CompletedTask
            .ContinueWith(
                (_, state) =>
                {
                    var externalState = (State)state;
                    GC.KeepAlive(externalState);
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

    class State { }
}