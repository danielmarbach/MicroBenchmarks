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
                Add(MarkdownExporter.GitHub);
                Add(MemoryDiagnoser.Default);
                Add(StatisticColumn.AllStatistics);
                Add(Job.Default.With(Platform.X64));
                Add(Job.Default.With(Platform.X86));
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