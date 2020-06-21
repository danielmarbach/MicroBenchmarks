using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;

namespace MicroBenchmarks.Tasks
{
    [Config(typeof(Config))]
    public class TaskVSValueTask
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

        [Benchmark]
        public Task TaskCompleted()
        {
            return Task.CompletedTask;
        }

        [Benchmark]
        public async Task TaskYield()
        {
            await Task.Yield();
        }

        [Benchmark]
        public ValueTask ValueTask()
        {
            return default;
        }

        [Benchmark]
        public async ValueTask ValueTaskYield()
        {
            await Task.Yield();
        }
    }
}