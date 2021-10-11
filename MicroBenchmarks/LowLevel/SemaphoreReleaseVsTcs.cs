using System.Threading;
using System.Threading.Tasks;
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
    public class SemaphoreReleaseVsTcs
    {
        private volatile TaskCompletionSource<bool> tcs;
        private SemaphoreSlim semaphore;
        private CancellationTokenSource cts;
        private Task createContention;

        private class Config : ManualConfig
        {
            public Config()
            {
                AddExporter(MarkdownExporter.GitHub);
                AddDiagnoser(MemoryDiagnoser.Default);
            }
        }

        [GlobalSetup]
        public void Setup()
        {
            this.tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            this.semaphore = new SemaphoreSlim(0);
            this.cts = new CancellationTokenSource();
            this.createContention = Task.Run(() =>
            {
                while (cts.IsCancellationRequested)
                {
                    this.tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                }
            });
        }

        [GlobalCleanup]
        public Task Cleanup()
        {
            cts.Cancel();
            return this.createContention;
        }

        [Benchmark(Baseline = true)]
        public void TaskCompletionSource()
        {
            this.tcs.TrySetResult(true);
        }

        [Benchmark]
        public void SemaphoreSlim()
        {
            semaphore.Release();
        }
    }
}