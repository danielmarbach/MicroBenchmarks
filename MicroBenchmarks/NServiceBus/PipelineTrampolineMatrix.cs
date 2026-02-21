using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;

namespace MicroBenchmarks.NServiceBus;

[SimpleJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Alphabetical)]
public class PipelineTrampolineMatrix
{
    private CurrentBehaviorContext currentContext;
    private PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext> currentSuccessPipeline;
    private PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext> currentSyncExceptionPipeline;
    private PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext> currentExceptionPipeline;
    private PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext> currentReplayPipeline;

    private Trampoline.BehaviorContext trampolineSuccessContext;
    private Trampoline.BehaviorContext trampolineSyncExceptionContext;
    private Trampoline.BehaviorContext trampolineSyncExceptionContinueWithContext;
    private Trampoline.BehaviorContext trampolineExceptionContext;
    private Trampoline.BehaviorContext trampolineExceptionContinueWithContext;
    private Trampoline.BehaviorContext trampolineReplayContext;

    [Params(10, 20, 40)]
    public int PipelineDepth { get; set; }

    [Params(3)]
    public int ReplayCount { get; set; }

    [GlobalSetup]
    public void SetUp()
    {
        currentContext = new CurrentBehaviorContext();

        currentSuccessPipeline = CreateCurrentSuccessPipeline(PipelineDepth);
        currentSyncExceptionPipeline = CreateCurrentSyncExceptionPipeline(PipelineDepth);
        currentExceptionPipeline = CreateCurrentExceptionPipeline(PipelineDepth);
        currentReplayPipeline = CreateCurrentReplayPipeline(PipelineDepth, ReplayCount);

        trampolineSuccessContext = CreateTrampolineSuccessContext(PipelineDepth);
        trampolineSyncExceptionContext = CreateTrampolineSyncExceptionContext(PipelineDepth);
        trampolineSyncExceptionContinueWithContext = CreateTrampolineSyncExceptionContext(PipelineDepth);
        trampolineExceptionContext = CreateTrampolineExceptionContext(PipelineDepth);
        trampolineExceptionContinueWithContext = CreateTrampolineExceptionContext(PipelineDepth);
        trampolineReplayContext = CreateTrampolineReplayContext(PipelineDepth, ReplayCount);

        currentSuccessPipeline.Invoke(currentContext).GetAwaiter().GetResult();
        trampolineTrampolineStart(trampolineSuccessContext).GetAwaiter().GetResult();

        try
        {
            currentSyncExceptionPipeline.Invoke(currentContext).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
        }

        try
        {
            trampolineTrampolineStart(trampolineSyncExceptionContext).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
        }

        try
        {
            trampolineContinueWithStart(trampolineSyncExceptionContinueWithContext).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
        }

        try
        {
            currentExceptionPipeline.Invoke(currentContext).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
        }

        try
        {
            trampolineTrampolineStart(trampolineExceptionContext).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
        }

        try
        {
            trampolineContinueWithStart(trampolineExceptionContinueWithContext).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
        }

        currentReplayPipeline.Invoke(currentContext).GetAwaiter().GetResult();
        trampolineTrampolineStart(trampolineReplayContext).GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Success")]
    public Task Current_Success() => currentSuccessPipeline.Invoke(currentContext);

    [Benchmark]
    [BenchmarkCategory("Success")]
    public Task Trampo_Success() => trampolineTrampolineStart(trampolineSuccessContext);

    [Benchmark]
    [BenchmarkCategory("Success")]
    public Task Trampo_ContinueWith_Success() => trampolineContinueWithStart(trampolineSuccessContext);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ExceptionSync")]
    public async Task<Exception?> Current_Exception_Sync()
    {
        try
        {
            await currentSyncExceptionPipeline.Invoke(currentContext).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ex;
        }

        return null;
    }

    [Benchmark]
    [BenchmarkCategory("ExceptionSync")]
    public async Task<Exception?> Trampo_Exception_Sync()
    {
        try
        {
            await trampolineTrampolineStart(trampolineSyncExceptionContext).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ex;
        }

        return null;
    }

    [Benchmark]
    [BenchmarkCategory("ExceptionSync")]
    public async Task<Exception?> Trampo_Exception_Sync_ContinueWith()
    {
        try
        {
            await trampolineContinueWithStart(trampolineSyncExceptionContinueWithContext).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ex;
        }

        return null;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Exception")]
    public async Task<Exception?> Current_Exception()
    {
        try
        {
            await currentExceptionPipeline.Invoke(currentContext).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ex;
        }

        return null;
    }

    [Benchmark]
    [BenchmarkCategory("Exception")]
    public async Task<Exception?> Trampo_Exception()
    {
        try
        {
            await trampolineTrampolineStart(trampolineExceptionContext).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ex;
        }

        return null;
    }

    [Benchmark]
    [BenchmarkCategory("Exception")]
    public async Task<Exception?> Trampo_Exception_ContinueWith()
    {
        try
        {
            await trampolineContinueWithStart(trampolineExceptionContinueWithContext).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ex;
        }

        return null;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Replay")]
    public Task Current_Replay() => currentReplayPipeline.Invoke(currentContext);

    [Benchmark]
    [BenchmarkCategory("Replay")]
    public Task Trampo_Replay() => trampolineTrampolineStart(trampolineReplayContext);

    static Task trampolineTrampolineStart(Trampoline.BehaviorContext context) => Trampoline.StageRunners.Start(context);
    static Task trampolineContinueWithStart(Trampoline.BehaviorContext context) => TrampolineContinueWith.StageRunners.Start(context);

    static PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext> CreateCurrentSuccessPipeline(int depth)
    {
        var modifications = new PipelineModifications();
        for (var i = 0; i < depth; i++)
        {
            modifications.Additions.Add(RegisterStep.Create($"success-{i}", typeof(Behavior1SealedOptimization), $"success-{i}", _ => new Behavior1SealedOptimization()));
        }

        return new PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext>(null, new SettingsHolder(), modifications);
    }

    static PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext> CreateCurrentExceptionPipeline(int depth)
    {
        var modifications = new PipelineModifications();
        for (var i = 0; i < depth - 1; i++)
        {
            modifications.Additions.Add(RegisterStep.Create($"exception-{i}", typeof(Behavior1SealedOptimization), $"exception-{i}", _ => new Behavior1SealedOptimization()));
        }

        modifications.Additions.Add(RegisterStep.Create("exception-throw", typeof(CurrentThrowingBehavior), "exception-throw", _ => new CurrentThrowingBehavior()));
        return new PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext>(null, new SettingsHolder(), modifications);
    }

    static PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext> CreateCurrentSyncExceptionPipeline(int depth)
    {
        var modifications = new PipelineModifications();
        for (var i = 0; i < depth - 1; i++)
        {
            modifications.Additions.Add(RegisterStep.Create($"sync-exception-{i}", typeof(Behavior1SealedOptimization), $"sync-exception-{i}", _ => new Behavior1SealedOptimization()));
        }

        modifications.Additions.Add(RegisterStep.Create("sync-exception-throw", typeof(CurrentSyncThrowingBehavior), "sync-exception-throw", _ => new CurrentSyncThrowingBehavior()));
        return new PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext>(null, new SettingsHolder(), modifications);
    }

    static PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext> CreateCurrentReplayPipeline(int depth, int replayCount)
    {
        var modifications = new PipelineModifications();
        modifications.Additions.Add(RegisterStep.Create("replay-root", typeof(CurrentReplayBehavior), "replay-root", _ => new CurrentReplayBehavior(replayCount)));

        for (var i = 1; i < depth; i++)
        {
            modifications.Additions.Add(RegisterStep.Create($"replay-{i}", typeof(Behavior1SealedOptimization), $"replay-{i}", _ => new Behavior1SealedOptimization()));
        }

        return new PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext>(null, new SettingsHolder(), modifications);
    }

    static Trampoline.BehaviorContext CreateTrampolineSuccessContext(int depth)
    {
        var behaviors = new IBehavior[depth];
        var parts = new Trampoline.PipelinePart[depth];

        for (var i = 0; i < depth; i++)
        {
            behaviors[i] = new Trampoline.BehaviorTrampoline();
            parts[i] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, Trampoline.BehaviorTrampoline>();
        }

        return new Trampoline.BehaviorContext
        {
            Behaviors = behaviors,
            Parts = parts
        };
    }

    static Trampoline.BehaviorContext CreateTrampolineExceptionContext(int depth)
    {
        var behaviors = new IBehavior[depth];
        var parts = new Trampoline.PipelinePart[depth];

        for (var i = 0; i < depth - 1; i++)
        {
            behaviors[i] = new Trampoline.BehaviorTrampoline();
            parts[i] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, Trampoline.BehaviorTrampoline>();
        }

        var lastIndex = depth - 1;
        behaviors[lastIndex] = new Trampoline.ThrowingTrampoline();
        parts[lastIndex] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, Trampoline.ThrowingTrampoline>();

        return new Trampoline.BehaviorContext
        {
            Behaviors = behaviors,
            Parts = parts
        };
    }

    static Trampoline.BehaviorContext CreateTrampolineSyncExceptionContext(int depth)
    {
        var behaviors = new IBehavior[depth];
        var parts = new Trampoline.PipelinePart[depth];

        for (var i = 0; i < depth - 1; i++)
        {
            behaviors[i] = new Trampoline.BehaviorTrampoline();
            parts[i] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, Trampoline.BehaviorTrampoline>();
        }

        var lastIndex = depth - 1;
        behaviors[lastIndex] = new TrampolineSyncThrowingBehavior();
        parts[lastIndex] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, TrampolineSyncThrowingBehavior>();

        return new Trampoline.BehaviorContext
        {
            Behaviors = behaviors,
            Parts = parts
        };
    }

    static Trampoline.BehaviorContext CreateTrampolineReplayContext(int depth, int replayCount)
    {
        var behaviors = new IBehavior[depth];
        var parts = new Trampoline.PipelinePart[depth];

        behaviors[0] = new TrampolineReplayBehavior(replayCount);
        parts[0] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, TrampolineReplayBehavior>();

        for (var i = 1; i < depth; i++)
        {
            behaviors[i] = new Trampoline.BehaviorTrampoline();
            parts[i] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, Trampoline.BehaviorTrampoline>();
        }

        return new Trampoline.BehaviorContext
        {
            Behaviors = behaviors,
            Parts = parts
        };
    }

    class CurrentBehaviorContext : ContextBag, IBehaviorContext
    {
        public ContextBag Extensions => this;
    }

    sealed class CurrentThrowingBehavior : IBehavior<IBehaviorContext, IBehaviorContext>
    {
        public async Task Invoke(IBehaviorContext context, Func<IBehaviorContext, Task> next)
        {
            await Task.Yield();
            throw new InvalidOperationException();
        }
    }

    sealed class CurrentSyncThrowingBehavior : IBehavior<IBehaviorContext, IBehaviorContext>
    {
        public Task Invoke(IBehaviorContext context, Func<IBehaviorContext, Task> next)
        {
            throw new InvalidOperationException();
        }
    }

    sealed class CurrentReplayBehavior(int replayCount) : IBehavior<IBehaviorContext, IBehaviorContext>
    {
        public async Task Invoke(IBehaviorContext context, Func<IBehaviorContext, Task> next)
        {
            for (var i = 0; i < replayCount; i++)
            {
                await next(context).ConfigureAwait(false);
            }
        }
    }

    sealed class TrampolineReplayBehavior(int replayCount) : Trampoline.IBehavior<Trampoline.IBehaviorContext, Trampoline.IBehaviorContext>
    {
        public async Task Invoke(Trampoline.IBehaviorContext context, Func<Trampoline.IBehaviorContext, Task> next)
        {
            for (var i = 0; i < replayCount; i++)
            {
                await next(context).ConfigureAwait(false);
            }
        }
    }

    sealed class TrampolineSyncThrowingBehavior : Trampoline.IBehavior<Trampoline.IBehaviorContext, Trampoline.IBehaviorContext>
    {
        public Task Invoke(Trampoline.IBehaviorContext context, Func<Trampoline.IBehaviorContext, Task> next)
        {
            throw new InvalidOperationException();
        }
    }
}
