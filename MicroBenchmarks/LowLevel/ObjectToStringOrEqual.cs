using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.LowLevel
{
    [Config(typeof(Config))]
    public class ObjectToStringOrEqual
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddExporter(MarkdownExporter.GitHub);
                AddDiagnoser(MemoryDiagnoser.Default);
                AddColumn(StatisticColumn.AllStatistics);
            }
        }

        private object someStringDisguisedAsObject = "somestring";

        [Benchmark(Baseline = true)]
        public bool ObjectToString()
        {
            return someStringDisguisedAsObject.ToString() == "somestring";
        }

        [Benchmark()]
        public bool ObjectEquals()
        {
            return someStringDisguisedAsObject.Equals("somestring");
        }
    }
}