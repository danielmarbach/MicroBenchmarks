using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.RabbitMQ;

[Config(typeof(Config))]
public class WorkerComparison
{
    private WorkPoolTaskCompletionSource tcsWorker;
    private List<Work> work;
    private WorkPoolSemaphoreSlim semaphoreWorker;

    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.ShortRun);
        }
    }

    [Params(1000, 10000, 100000, 1000000, 10000000)]
    public int Elements { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        this.tcsWorker = new WorkPoolTaskCompletionSource();
        this.tcsWorker.Start();

        this.semaphoreWorker = new WorkPoolSemaphoreSlim();
        this.semaphoreWorker.Start();

        this.work = Enumerable.Range(0, Elements).Select(i => new Work()).ToList();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        this.tcsWorker.Stop();
        this.semaphoreWorker.Stop();
    }

    [Benchmark]
    public void SemaphoreSlim()
    {
        foreach (var w in work)
        {
            semaphoreWorker.Enqueue(w);
        }
    }

    [Benchmark(Baseline = true)]
    public void TaskCompletionSource()
    {
        foreach (var w in work)
        {
            tcsWorker.Enqueue(w);
        }
    }
}

class Work
{
    public Task Execute()
    {
        return Task.CompletedTask;
    }
}

class WorkPoolTaskCompletionSource
{
    readonly ConcurrentQueue<Work> _workQueue;
    readonly CancellationTokenSource _tokenSource;
    CancellationTokenRegistration _tokenRegistration;

    TaskCompletionSource<bool> _syncSource =
        new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

    private Task _task;

    public WorkPoolTaskCompletionSource()
    {
        _workQueue = new ConcurrentQueue<Work>();
        _tokenSource = new CancellationTokenSource();
        _tokenRegistration = _tokenSource.Token.Register(() => _syncSource.TrySetCanceled());
    }

    public void Start()
    {
        _task = Task.Run(Loop, CancellationToken.None);
    }

    public void Enqueue(Work work)
    {
        _workQueue.Enqueue(work);
        _syncSource.TrySetResult(true);
    }

    async Task Loop()
    {
        while (_tokenSource.IsCancellationRequested == false)
        {
            try
            {
                await _syncSource.Task.ConfigureAwait(false);
                _syncSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            catch (TaskCanceledException)
            {
                // Swallowing the task cancellation in case we are stopping work.
            }

            if (!_tokenSource.IsCancellationRequested && _workQueue.TryDequeue(out Work work))
            {
                await work.Execute().ConfigureAwait(false);
            }
        }
    }

    public void Stop()
    {
        _tokenSource.Cancel();
        _tokenRegistration.Dispose();
    }

}

class WorkPoolSemaphoreSlim
{
    readonly ConcurrentQueue<Work> _workQueue;
    readonly CancellationTokenSource _tokenSource;
    CancellationTokenRegistration _tokenRegistration;

    SemaphoreSlim _semaphore = new SemaphoreSlim(0);

    private Task _task;

    public WorkPoolSemaphoreSlim()
    {
        _workQueue = new ConcurrentQueue<Work>();
        _tokenSource = new CancellationTokenSource();
    }

    public void Start()
    {
        _task = Task.Run(Loop, CancellationToken.None);
    }

    public void Enqueue(Work work)
    {
        _workQueue.Enqueue(work);
        _semaphore.Release();
    }

    async Task Loop()
    {
        while (_tokenSource.IsCancellationRequested == false)
        {
            try
            {
                await _semaphore.WaitAsync(_tokenSource.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // Swallowing the task cancellation in case we are stopping work.
            }

            if (!_tokenSource.IsCancellationRequested && _workQueue.TryDequeue(out Work work))
            {
                await work.Execute().ConfigureAwait(false);
            }
        }
    }

    public void Stop()
    {
        _tokenSource.Cancel();
        _tokenRegistration.Dispose();
    }

}