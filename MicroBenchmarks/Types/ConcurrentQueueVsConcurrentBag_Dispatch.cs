using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;

namespace MicroBenchmarks.Linq
{
    [Config(typeof(Config))]
    public class ConcurrentQueueVsConcurrentBag_Dispatch
    {
        private ConcurrentQueue<int> queue;
        private ConcurrentBag<int> bag;
        private IProducerConsumerCollection<int> collection;

        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(MemoryDiagnoser.Default);
                Add(StatisticColumn.AllStatistics);
            }
        }

        [Params(false, true)]
        public bool Flag { get; set; }

        [GlobalSetup]
        public void SetUp()
        {
            queue = new ConcurrentQueue<int>();
            bag = new ConcurrentBag<int>();
            if (Flag)
            {
                collection = bag;
            }
            else
            {
                collection = queue;
            }
        }

        [Benchmark(Baseline = true)]
        public void Branching()
        {
            if (Flag)
            {
                bag.Add(5);
            }
            else
            {
                queue.Enqueue(5);
            }
        }

        [Benchmark]
        public void InterfaceDispatch()
        {
            collection.TryAdd(5);
        }
    }
}