using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;

namespace MicroBenchmarks.Tasks;

[Config(typeof(Config))]
public class TaskCastWithAwaitVsCastExtension
{

    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
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