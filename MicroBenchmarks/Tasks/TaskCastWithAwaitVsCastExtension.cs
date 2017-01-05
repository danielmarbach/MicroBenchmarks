using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks
{
    [Config(typeof(Config))]
    public class TaskCastWithAwaitVsCastExtension
    {

        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(new BenchmarkDotNet.Diagnosers.MemoryDiagnoser());
                Add(Job.Default.With(Platform.X86).WithTargetCount(100));
                Add(Job.Default.With(Platform.X64).WithTargetCount(100));
            }
        }


        [Benchmark]
        public async Task TaskCastWithAwait()
        {
            await AwaitCastSimulator.Simulate().ConfigureAwait(false);
        }

        [Benchmark]
        public async Task TaskCastExtension()
        {
            await TaskCastSimulator.Simulate().ConfigureAwait(false);
        }
    }
}