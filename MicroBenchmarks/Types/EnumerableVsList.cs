using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;

namespace MicroBenchmarks.Linq
{
    [Config(typeof(Config))]
    public class HashSetVsListWithContains
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                //Add(new MemoryDiagnoser());
                Add(StatisticColumn.AllStatistics);
            }
        }
        [Params(2, 4, 8, 16, 32, 64)]
        public int Elements { get; set; }

        [Benchmark(Baseline = true)]
        public void HashSet()
        {
            var hashSet = new HashSet<string>();
            for (int i = 0; i < Elements; i++)
            {
                hashSet.Add(i.ToString());
            }
        }

        [Benchmark]
        public void ListWithContains()
        {
            var hashSet = new List<string>();
            for (int i = 0; i < Elements; i++)
            {
                var value = i.ToString();
                if (!hashSet.Contains(value))
                {
                    hashSet.Add(value);
                }
            }
        }
    }
}