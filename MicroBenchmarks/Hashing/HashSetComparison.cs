using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.Hashing;

[Config(typeof(Config))]
public class HashSetComparison
{
    private HashSet<int> hashSet;
    private int start;

    class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default.WithUnrollFactor(64));
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        hashSet = new HashSet<int>(1000);
        for (int i = 0; i < 1000; i++)
        {
            hashSet.Add(i);
        }

        start = hashSet.Count + 1;
    }

    [Benchmark(Baseline = true)]
    public void ContainsAndAdd()
    {
        start++;
        if (!hashSet.Contains(start))
        {
            hashSet.Add(start);
        }
    }

    [Benchmark]
    public void AddOnly()
    {
        start++;
        hashSet.Add(start);
    }
}