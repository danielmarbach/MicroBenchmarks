using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;

namespace MicroBenchmarks.Linq;

using BenchmarkDotNet.Engines;

[Config(typeof(Config))]
public class ForeachVsFor
{
    private int[] list;
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
    [Params(2, 4, 8, 16, 32, 64, 128)]
    public int Elements { get; set; }

    [GlobalSetup]
    public void SetUp()
    {
        list = Enumerable.Range(0, Elements).ToArray();

        consumer = new Consumer();
    }

    [Benchmark(Baseline = true)]
    public void Foreach()
    {
        foreach (var i in list)
        {
            consumer.Consume(i);
        }
    }

    [Benchmark]
    public void For()
    {
        // ReSharper disable once ForCanBeConvertedToForeach
        for (int i = 0; i < list.Length; i++)
        {
            consumer.Consume(list[i]);
        }
    }
}