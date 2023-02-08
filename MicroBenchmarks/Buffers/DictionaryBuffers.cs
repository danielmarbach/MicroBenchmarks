using System;
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
public class DictionaryBuffers
{
    private class Config : ManualConfig
    {
        public Config()
        {
            Add(MarkdownExporter.GitHub);
            Add(MemoryDiagnoser.Default);
            Add(StatisticColumn.AllStatistics);
        }
    }

    [Params(1,
        2,
        8,
        16,
        32,
        64)]
    public int Size { get; set; }

    private DeadSimpleDictionaryBuffer dictionaryBuffer;
    private Dictionary<string, string> templateDictionary;

    [GlobalSetup]
    public void GlobalSetup()
    {
        dictionaryBuffer = new DeadSimpleDictionaryBuffer(true);
        templateDictionary = new Dictionary<string, string>();
        for (var i = 0; i < Size; i++)
        {
            templateDictionary.Add(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        }
    }

    [Benchmark(Baseline = true)]
    public async Task Dictionary_Allocate()
    {
        var tasks = new List<Task>(10);

        for (var i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (var j = 0; j < 100; j++)
                {
                    DeadCodeEliminationHelper.KeepAliveWithoutBoxing(FillDictionary(new Dictionary<string, string>(Size)));
                }
            }));
        }

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task Dictionary_RentAndReturn_Aware()
    {
        var tasks = new List<Task>(10);

        for (var i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (var j = 0; j < 100; j++)
                {
                    var pool = dictionaryBuffer;
                    var dictionary = FillDictionary(pool.Rent(Size));
                    pool.Return(dictionary, Size, clear: true);
                }
            }));
        }

        await Task.WhenAll(tasks);
    }

    private Dictionary<string, string> FillDictionary(Dictionary<string, string> dictionary)
    {
        foreach (var pair in templateDictionary)
        {
            dictionary.Add(pair.Key, pair.Value);
        }

        return dictionary;
    }

    class DeadSimpleDictionaryBuffer
    {
        ConcurrentBag<Dictionary<string, string>> buffer1 = new ConcurrentBag<Dictionary<string, string>>();
        ConcurrentBag<Dictionary<string, string>> buffer2 = new ConcurrentBag<Dictionary<string, string>>();
        ConcurrentBag<Dictionary<string, string>> buffer8 = new ConcurrentBag<Dictionary<string, string>>();
        ConcurrentBag<Dictionary<string, string>> buffer16 = new ConcurrentBag<Dictionary<string, string>>();
        ConcurrentBag<Dictionary<string, string>> buffer32 = new ConcurrentBag<Dictionary<string, string>>();
        ConcurrentBag<Dictionary<string, string>> buffer64 = new ConcurrentBag<Dictionary<string, string>>();

        public DeadSimpleDictionaryBuffer(bool preallocate = true)
        {
            if (!preallocate) return;

            for (int i = 0; i < 10; i++)
            {
                buffer1.Add(new Dictionary<string, string>(1));
            }

            for (int i = 0; i < 10; i++)
            {
                buffer2.Add(new Dictionary<string, string>(2));
            }

            for (int i = 0; i < 10; i++)
            {
                buffer8.Add(new Dictionary<string, string>(8));
            }

            for (int i = 0; i < 10; i++)
            {
                buffer16.Add(new Dictionary<string, string>(16));
            }

            for (int i = 0; i < 10; i++)
            {
                buffer32.Add(new Dictionary<string, string>(32));
            }

            for (int i = 0; i < 10; i++)
            {
                buffer64.Add(new Dictionary<string, string>(64));
            }
        }

        public Dictionary<string, string> Rent(int size)
        {
            Dictionary<string, string> dictionary = null;
            switch (size)
            {
                case 1:
                    if (!buffer1.TryTake(out dictionary))
                    {
                        dictionary = new Dictionary<string, string>(1);
                    }
                    break;
                case 2:
                    if (!buffer2.TryTake(out dictionary))
                    {
                        dictionary = new Dictionary<string, string>(2);
                    }
                    break;
                case 8:
                    if (!buffer8.TryTake(out dictionary))
                    {
                        dictionary = new Dictionary<string, string>(8);
                    }
                    break;
                case 16:
                    if (!buffer16.TryTake(out dictionary))
                    {
                        dictionary = new Dictionary<string, string>(16);
                    }
                    break;
                case 32:
                    if (!buffer32.TryTake(out dictionary))
                    {
                        dictionary = new Dictionary<string, string>(32);
                    }
                    break;
                case 64:
                    if (!buffer64.TryTake(out dictionary))
                    {
                        dictionary = new Dictionary<string, string>(64);
                    }
                    break;
            }

            return dictionary;
        }

        public void Return(Dictionary<string, string> dictionary, int size, bool clear = false)
        {
            if (clear)
            {
                dictionary.Clear();
            }
            switch (size)
            {
                case 1:
                    buffer1.Add(dictionary);
                    break;
                case 2:
                    buffer2.Add(dictionary);
                    break;
                case 8:
                    buffer8.Add(dictionary);
                    break;
                case 16:
                    buffer16.Add(dictionary);
                    break;
                case 32:
                    buffer32.Add(dictionary);
                    break;
                case 64:
                    buffer64.Add(dictionary);
                    break;
            }
        }
    }
}