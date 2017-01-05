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
    public class EnumerableVsList
    {
        private List<int> list;
        private IList<int> ilist;

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

        [Setup]
        public void SetUp()
        {
            list = Enumerable.Range(0, Elements).ToList();
            ilist = list;
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
        public void IList()
        {
            foreach (var i in ilist)
            {
                GC.KeepAlive(i);
            }
        }

        [Benchmark]
        public void Enumberable()
        {
            foreach (var i in list.AsEnumerable())
            {
                GC.KeepAlive(i);
            }
        }
    }
}