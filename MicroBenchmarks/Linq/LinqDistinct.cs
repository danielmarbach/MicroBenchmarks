using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;

namespace MicroBenchmarks.Linq;

[Config(typeof(Config))]
public class LinqDistinct
{
    private List<int> list;

    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddColumn(StatisticColumn.AllStatistics);
        }
    }
    [Params(2, 4, 8, 16, 32, 64)]
    public int Elements { get; set; }

    [GlobalSetup]
    public void SetUp()
    {
        list = Enumerable.Range(0, Elements).ToList();
        list.AddRange(Enumerable.Range(0, Elements).ToList());
    }

    [Benchmark(Baseline = true)]
    public void HashSet()
    {
        var hasSet = new HashSet<int>(list);
        foreach (var i in hasSet)
        {
            GC.KeepAlive(i);
        }
    }

    [Benchmark]
    public void Distinct()
    {
        foreach (var i in list.Distinct())
        {
            GC.KeepAlive(i);
        }
    }
}