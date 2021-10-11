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
    public class LinqGuidConversionContains
    {
        private static readonly HashSet<Guid> _requestResponseLockedMessages = new HashSet<Guid>
        {
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()
        };

        private List<string> list;

        [Params(1, 2, 4, 8, 16)] public int Elements { get; set; }

        [GlobalSetup]
        public void SetUp()
        {
            list = Enumerable.Range(0, Elements).Select(i => Guid.NewGuid().ToString()).ToList();
        }

        private static Guid[] Old(IEnumerable<string> lockTokens)
        {
            var lockTokenGuids = lockTokens.Select(token => new Guid(token)).ToArray();
            if (lockTokenGuids.Any(lockToken => _requestResponseLockedMessages.Contains(lockToken)))
                return lockTokenGuids;

            return lockTokenGuids;
        }

        private static Guid[] New(IEnumerable<string> lockTokens)
        {
            var requestResponse = false;
            var lockTokenGuids = lockTokens.Select(token => new Guid(token)).ToArray();
            foreach (var tokenGuid in lockTokenGuids)
            {
                if (_requestResponseLockedMessages.Contains(tokenGuid))
                {
                    requestResponse = true;
                    break;
                }
            }

            if (requestResponse)
            {
                return lockTokenGuids;
            }

            return lockTokenGuids;
        }

        [Benchmark(Baseline = true)]
        public Guid[] Before()
        {
            return Old(list);
        }

        [Benchmark]
        public Guid[] After()
        {
            return New(list);
        }

        private class Config : ManualConfig
        {
            public Config()
            {
                AddExporter(MarkdownExporter.GitHub);
                AddDiagnoser(MemoryDiagnoser.Default);
                AddJob(Job.MediumRun);
            }
        }
    }
}