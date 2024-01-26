using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;

namespace MicroBenchmarks.Linq;

[Config(typeof(Config))]
public class LazyVsEagerEnumeration
{
    private IEnumerable<int> first;
    private IEnumerable<int> second;
    private Random random;
    private Consumer consumer;

    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddColumn(StatisticColumn.AllStatistics);
        }
    }
    [Params(0, 1, 2, 4, 8, 16, 32)]
    public int Elements { get; set; }

    [GlobalSetup]
    public void SetUp()
    {
        consumer = new Consumer();
        random = new Random(Seed);
        first = Enumerable.Range(0, Elements).Select(i => random.Next());
        second = Enumerable.Range(0, Elements).Select(i => random.Next());
    }

    [Benchmark(Baseline = true)]
    public void Lazy()
    {
        var concat = first.Concat(second);

        if (!concat.Any())
        {
            return;
        }

        concat.Consume(consumer);
    }

    [Benchmark]
    public void Eager()
    {
        var concat = first.Concat(second).ToArray();

        if (concat.Length == 0)
        {
            return;
        }

        concat.Consume(consumer);
    }

    private const int Seed = 12345; // we always use the same seed to have repeatable results!
}