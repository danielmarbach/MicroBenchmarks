using System.Collections.Concurrent;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.Types;

[Config(typeof(Config))]
public class ConcurrentQueueVsConcurrentBag_Adding
{
    private ConcurrentQueue<int> queue;
    private ConcurrentBag<int> bag;

    private class Config : ManualConfig
    {
        public Config()
        {
            Add(MarkdownExporter.GitHub);
            Add(MemoryDiagnoser.Default);
            Add(StatisticColumn.AllStatistics);
            Add(Job.MediumRun);
        }
    }

    [GlobalSetup]
    public void SetUp()
    {
        queue = new ConcurrentQueue<int>();
        bag = new ConcurrentBag<int>();
    }

    [Benchmark(Baseline = true)]
    public void ConcurrentQueue_Enqueue()
    {
        Parallel.For(0, 1000, i => queue.Enqueue(i));
    }

    [Benchmark]
    public void ConcurrentBag_Add()
    {
        Parallel.For(0, 1000, i => bag.Add(i));
    }
}