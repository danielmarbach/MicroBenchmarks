using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Exporters;

namespace MicroBenchmarks.Linq
{
    [Config(typeof(Config))]
    public class LinqWhere
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
        [Params(2, 4, 8, 16, 32, 64)]
        public int Elements { get; set; }

        [Setup]
        public void SetUp()
        {
            list = Enumerable.Range(0, Elements).ToList();
        }

        [Benchmark(Baseline = true)]
        public void Foreach()
        {
            foreach (var i in list)
            {
                if (i > 2)
                {
                    GC.KeepAlive(i);
                }
            }
        }

        [Benchmark]
        public void For()
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 2)
                {
                    GC.KeepAlive(i);
                }
            }
        }

        [Benchmark]
        public void Where()
        {
            foreach (var result in list.Where(i => i > 2))
            {
                GC.KeepAlive(result);
            }
        }
    }
}