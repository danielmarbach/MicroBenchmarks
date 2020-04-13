using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.Linq
{
    [Config(typeof(Config))]
    public class LinqGuidConversion
    {
        private List<Guid> list;

        [Params(1, 16, 32, 64)]
        public int Elements { get; set; }

        [GlobalSetup]
        public void SetUp()
        {
            list = Enumerable.Range(0, Elements).Select(i => Guid.NewGuid()).ToList();
        }

        [Benchmark(Baseline = true)]
        public List<ArraySegment<byte>> Before()
        {
            return ConvertLockTokensToDeliveryTags(list);
        }

        [Benchmark]
        public List<ArraySegment<byte>> After()
        {
            return ConvertLockTokensToDeliveryTagsWithoutLinq(list);
        }

        private static List<ArraySegment<byte>> ConvertLockTokensToDeliveryTags(IEnumerable<Guid> lockTokens)
        {
            return lockTokens.Select(lockToken => new ArraySegment<byte>(lockToken.ToByteArray())).ToList();
        }

        private static List<ArraySegment<byte>> ConvertLockTokensToDeliveryTagsWithoutLinq(IEnumerable<Guid> lockTokens)
        {
            var lockTokenSegments = new List<ArraySegment<byte>>(lockTokens.Count());
            foreach (var token in lockTokens) lockTokenSegments.Add(new ArraySegment<byte>(token.ToByteArray()));
            return lockTokenSegments;
        }

        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(MemoryDiagnoser.Default);
                Add(Job.ShortRun);
            }
        }


    }
}