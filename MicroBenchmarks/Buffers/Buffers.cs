using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.Buffers;

[Config(typeof(Config))]
public class Buffers
{
    private class Config : ManualConfig
    {
        public Config()
        {
            Add(MarkdownExporter.GitHub);
            Add(MemoryDiagnoser.Default);
            Add(StatisticColumn.AllStatistics);
            // Add(Job.Default.With(Platform.X64).With(new GcMode()
            // {
            //     Force = false // tell BenchmarkDotNet not to force GC collections after every iteration
            // }));
        }
    }

    [Params(1,
        2,
        8,
        16,
        32,
        64)]
    public int Size { get; set; }

    private ArrayPool<Task> taskListPool;
    private DeadSimpleDictionaryBuffer dictionaryBuffer;

    [GlobalSetup]
    public void GlobalSetup()
    {
        taskListPool = ArrayPool<Task>.Create();
        dictionaryBuffer = new DeadSimpleDictionaryBuffer();
    }

    [Benchmark(Baseline = true)]
    public void Task_Allocate()
        => DeadCodeEliminationHelper.KeepAliveWithoutBoxing(new Task[Size]);

    [Benchmark]
    public void Task_RentAndReturn_Shared()
    {
        var pool = ArrayPool<Task>.Shared;
        var array = pool.Rent(Size);
        pool.Return(array, clearArray: true);
    }

    [Benchmark]
    public void Task_RentAndReturn_Aware()
    {
        var pool = taskListPool;
        var array = pool.Rent(Size);
        pool.Return(array, clearArray: true);
    }

    class DeadSimpleDictionaryBuffer
    {
        ConcurrentBag<Dictionary<string, string>> buffer = new ConcurrentBag<Dictionary<string, string>>();

        public DeadSimpleDictionaryBuffer()
        {
            for (int i = 0; i < 64; i++)
            {
                buffer.Add(new Dictionary<string, string>());
            }
        }

        public Dictionary<string, string> Rent()
        {
            buffer.TryTake(out var dictionary);
            return dictionary;
        }

        public void Return(Dictionary<string, string> dictionary)
        {
            buffer.Add(dictionary);
        }
    }
}