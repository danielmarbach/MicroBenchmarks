using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;

namespace MicroBenchmarks.Async;

[Config(typeof(Config))]
public class Eliding
{
    private class Config : ManualConfig
    {
        public Config()
        {
            Add(MarkdownExporter.GitHub);
            Add(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default);
            Add(StatisticColumn.AllStatistics);
            Add(Job.Default.With(CsProjCoreToolchain.NetCoreApp20));
        }
    }

    [Benchmark(Baseline = true)]
    public async Task WithStatemachine()
    {
        await Task.Delay(1);
    }

    [Benchmark]
    public Task WithoutStatemachine()
    {
        return Task.Delay(1);
    }
}