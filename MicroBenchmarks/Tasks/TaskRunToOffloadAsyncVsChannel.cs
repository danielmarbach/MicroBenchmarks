namespace MicroBenchmarks.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Columns;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Exporters;

    [Config(typeof(Config))]
    public class TaskRunToOffloadAsyncVsChannel
    {
        private List<Task> tasks;
        private BackgroundWorkQueue worker;

        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(MemoryDiagnoser.Default);
                Add(StatisticColumn.AllStatistics);
                AddDiagnoser(ThreadingDiagnoser.Default);
            }
        }

        [Params(1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024)]
        public int Operations { get; set; }

        [IterationSetup]
        public void IterationSetup()
        {
            tasks = new List<Task>(Operations);
            worker = new BackgroundWorkQueue(Operations);
        }

        [Benchmark(Baseline = true)]
        public Task TaskRun()
        {
            for (int i = 0; i < Operations; i++)
            {
                // use state machine here to make it a more fair comparison
                tasks.Add(Task.Run(static async () => await Task.Delay(1)));
            }

            return Task.WhenAll(tasks);
        }

        [Benchmark]
        public Task Worker()
        {
            for (int i = 0; i < Operations; i++)
            {
                worker.Enqueue(static () => Task.Delay(1));
            }

            return worker.Finished.Task;
        }

        public sealed class BackgroundWorkQueue : IDisposable
        {
            private readonly Channel<Work> workChannel;
            private readonly Task worker;
            private readonly CancellationTokenSource cancellationTokenSource;
            private readonly CancellationToken cancellationToken;
            private bool disposed;
            private static int currentItems;

            public BackgroundWorkQueue(int numberOfItems)
            {
                workChannel = Channel.CreateUnbounded<Work>(
                    new UnboundedChannelOptions
                    {
                        SingleReader = true,
                        SingleWriter = false,
                        AllowSynchronousContinuations = true,
                    });

                cancellationTokenSource = new CancellationTokenSource();
                cancellationToken = cancellationTokenSource.Token;
                Finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                currentItems = numberOfItems;
                worker = LoopAsync();
            }

            public TaskCompletionSource Finished { get; }

            public void Enqueue(Func<Task> work)
            {
                // returns falls when channel is completed
                if (workChannel.Writer.TryWrite(new Work(work, Finished)))
                {
                }
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                // we are not interested in the outcome of the worker task
                workChannel.Writer.Complete();
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                disposed = true;
            }

            async Task LoopAsync()
            {
                try
                {
                    while (await workChannel.Reader.WaitToReadAsync(cancellationToken))
                    {
                        while (workChannel.Reader.TryRead(out var work))
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            _ = work.ExecuteAsync();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // ignored for graceful shutdown
                }
            }

            private readonly struct Work
            {
                private readonly Func<Task> action;
                private readonly TaskCompletionSource countdownEvent1;

                public Work(Func<Task> action, TaskCompletionSource countdownEvent)
                {
                    countdownEvent1 = countdownEvent;
                    this.action = action;
                }

                public async Task ExecuteAsync()
                {
                    await action();
                    if (Interlocked.Decrement(ref currentItems) <= 0)
                    {
                        countdownEvent1.TrySetResult();
                    }
                }
            }
        }
    }
}