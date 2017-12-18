using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;

namespace MicroBenchmarks.Linq
{
    [Config(typeof(Config))]
    public class ForeachVsFor
    {
        private List<int> list;

        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(new BenchmarkDotNet.Diagnosers.MemoryDiagnoser());
                Add(StatisticColumn.AllStatistics);
            }
        }
        [Params(2, 4, 8, 16, 32, 64, 128)]
        public int Elements { get; set; }

        [GlobalSetup]
        public void SetUp()
        {
            list = Enumerable.Range(0, Elements).ToList();
        }

        [Benchmark(Baseline = true)]
        public void Foreach()
        {
            foreach (var i in list)
            {
                GC.KeepAlive(i);
            }
        }

        [Benchmark]
        public void For()
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < list.Count; i++)
            {
                GC.KeepAlive(list[i]);
            }
        }
    }
}