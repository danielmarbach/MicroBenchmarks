using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Exporters;

namespace MicroBenchmarks.Linq
{
    [Config(typeof(Config))]
    public class ReadonlyCollectionVsList
    {
        private List<int> list;
        private ReadOnlyCollection<int> readonlyList;

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
            readonlyList = new ReadOnlyCollection<int>(list);
        }

        [Benchmark(Baseline = true)]
        public void List()
        {
            foreach (var i in list)
            {
                GC.KeepAlive(i);
            }
        }

        [Benchmark]
        public void ReadonlyCollection()
        {
            foreach (var i in readonlyList)
            {
                GC.KeepAlive(i);
            }
        }
    }
}