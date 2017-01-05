using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.Tasks
{
    [Config(typeof(Config))]
    public class TaskWithWaitCancellation_Successful
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(new BenchmarkDotNet.Diagnosers.MemoryDiagnoser());
                Add(Job.Default.With(Platform.X64).WithTargetCount(100));
                Add(Job.Default.With(Platform.X86).WithTargetCount(100));
            }
        }

        [Benchmark(Baseline = true)]
        public async Task<int> LinkedTokenSources_Successful()
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(10)))
            {
                return await Successful().WithWaitCancellationLinkedTokenSource(cts.Token).ConfigureAwait(false);
            }
        }

        [Benchmark]
        public async Task<int> TaskCompletionSource_Successful()
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(10)))
            {
                return await Successful().WithWaitCancellationTaskCompletionSource(cts.Token).ConfigureAwait(false);
            }
        }

        static async Task<int> Successful()
        {
            await Task.Delay(2).ConfigureAwait(false);
            return 42;
        }
    }

    [Config(typeof(Config))]
    public class TaskWithWaitCancellation_Canceled
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(new BenchmarkDotNet.Diagnosers.MemoryDiagnoser());
                Add(Job.Default.With(Platform.X64).WithTargetCount(100));
                Add(Job.Default.With(Platform.X86).WithTargetCount(100));
            }
        }

        [Benchmark(Baseline = true)]
        public async Task<int> LinkedTokenSources_Canceled()
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(5)))
            {
                return await Canceled().WithWaitCancellationLinkedTokenSource(cts.Token).ConfigureAwait(false);
            }
        }

        [Benchmark]
        public async Task<int> TaskCompletionSource_Canceled()
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(5)))
            {
                return await Canceled().WithWaitCancellationTaskCompletionSource(cts.Token).ConfigureAwait(false);
            }
        }

        static async Task<int> Canceled()
        {
            await Task.Delay(10).ConfigureAwait(false);
            return 42;
        }
    }

    [Config(typeof(Config))]
    public class TaskWithWaitCancellation_Failure
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(new BenchmarkDotNet.Diagnosers.MemoryDiagnoser());
                Add(Job.Default.With(Platform.X64).WithTargetCount(100));
                Add(Job.Default.With(Platform.X86).WithTargetCount(100));
            }
        }

        [Benchmark(Baseline = true)]
        public async Task<int> LinkedTokenSources_Failure()
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(10)))
            {
                try
                {
                    return await Failure().WithWaitCancellationLinkedTokenSource(cts.Token).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // ignored
                    return 42;
                }
            }
        }

        [Benchmark]
        public async Task<int> TaskCompletionSource_Failure()
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(10)))
            {
                try
                {
                    return await Failure().WithWaitCancellationTaskCompletionSource(cts.Token).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // ignored
                    return 42;
                }
            }
        }

        static async Task<int> Failure()
        {
            await Task.Delay(2).ConfigureAwait(false);
            throw new InvalidOperationException();
        }
    }
}