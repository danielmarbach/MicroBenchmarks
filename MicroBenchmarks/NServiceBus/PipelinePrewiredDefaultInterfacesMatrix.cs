using System;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;

namespace MicroBenchmarks.NServiceBus;

[MediumRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Alphabetical)]
public class PipelinePrewiredDefaultInterfacesMatrix
{
    Trampoline.BehaviorContext prewiredSuccessContext;
    Trampoline.BehaviorContext prewiredExceptionContext;
    Trampoline.BehaviorContext prewiredSyncExceptionContext;
    Trampoline.BehaviorContext prewiredReplayContext;

    CurrentBehaviorContext defaultInterfacesContext;
    Func<IBehaviorContext, Task> defaultInterfacesSuccessPipeline;
    Func<IBehaviorContext, Task> defaultInterfacesExceptionPipeline;
    Func<IBehaviorContext, Task> defaultInterfacesSyncExceptionPipeline;
    Func<IBehaviorContext, Task> defaultInterfacesReplayPipeline;

    PipelineModifications warmupModifications;
    Consumer consumer;

    [Params(10, 20, 40)]
    public int PipelineDepth { get; set; }

    [Params(3)]
    public int ReplayCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        prewiredSuccessContext = CreatePrewiredSuccessContext(PipelineDepth);
        prewiredExceptionContext = CreatePrewiredExceptionContext(PipelineDepth);
        prewiredSyncExceptionContext = CreatePrewiredSyncExceptionContext(PipelineDepth);
        prewiredReplayContext = CreatePrewiredReplayContext(PipelineDepth, ReplayCount);

        defaultInterfacesContext = new CurrentBehaviorContext();
        defaultInterfacesSuccessPipeline = CreateDefaultInterfacesSuccessPipeline(PipelineDepth);
        defaultInterfacesExceptionPipeline = CreateDefaultInterfacesExceptionPipeline(PipelineDepth);
        defaultInterfacesSyncExceptionPipeline = CreateDefaultInterfacesSyncExceptionPipeline(PipelineDepth);
        defaultInterfacesReplayPipeline = CreateDefaultInterfacesReplayPipeline(PipelineDepth, ReplayCount);

        warmupModifications = new PipelineModifications();
        for (var i = 0; i < PipelineDepth; i++)
        {
            warmupModifications.Additions.Add(RegisterStep.Create(i.ToString(),
                typeof(Behavior1SealedOptimization), i.ToString(), _ => new Behavior1SealedOptimization()));
        }

        consumer = new Consumer();

        Prewired.StageRunners.Start(prewiredSuccessContext).GetAwaiter().GetResult();
        defaultInterfacesSuccessPipeline(defaultInterfacesContext).GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Execution")]
    public Task Prewired_Execution() => Prewired.StageRunners.Start(prewiredSuccessContext);

    [Benchmark, BenchmarkCategory("Execution")]
    public Task DefaultInterfaces_Execution() => defaultInterfacesSuccessPipeline(defaultInterfacesContext);

    [Benchmark(Baseline = true), BenchmarkCategory("Replay")]
    public Task Prewired_Replay() => Prewired.StageRunners.Start(prewiredReplayContext);

    [Benchmark, BenchmarkCategory("Replay")]
    public Task DefaultInterfaces_Replay() => defaultInterfacesReplayPipeline(defaultInterfacesContext);

    [Benchmark(Baseline = true), BenchmarkCategory("Exception")]
    public async Task<Exception?> Prewired_Exception()
    {
        try
        {
            await Prewired.StageRunners.Start(prewiredExceptionContext).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ex;
        }

        return null;
    }

    [Benchmark, BenchmarkCategory("Exception")]
    public async Task<Exception?> DefaultInterfaces_Exception()
    {
        try
        {
            await defaultInterfacesExceptionPipeline(defaultInterfacesContext).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ex;
        }

        return null;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("ExceptionSync")]
    public async Task<Exception?> Prewired_Exception_Sync()
    {
        try
        {
            await Prewired.StageRunners.Start(prewiredSyncExceptionContext).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ex;
        }

        return null;
    }

    [Benchmark, BenchmarkCategory("ExceptionSync")]
    public async Task<Exception?> DefaultInterfaces_Exception_Sync()
    {
        try
        {
            await defaultInterfacesSyncExceptionPipeline(defaultInterfacesContext).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ex;
        }

        return null;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Warmup")]
    public async Task Prewired_Warmup()
    {
        var prewiredContext = CreatePrewiredSuccessContext(PipelineDepth);
        ConsumeCoordinator();
        await Prewired.StageRunners.Start(prewiredContext).ConfigureAwait(false);
    }

    [Benchmark, BenchmarkCategory("Warmup")]
    public async Task DefaultInterfaces_Warmup()
    {
        var pipeline = CreateDefaultInterfacesSuccessPipeline(PipelineDepth);
        ConsumeCoordinator();
        await pipeline(defaultInterfacesContext).ConfigureAwait(false);
    }

    void ConsumeCoordinator()
    {
        var coordinator = new StepRegistrationsCoordinator(warmupModifications.Removals, warmupModifications.Replacements);

        foreach (var registration in warmupModifications.Additions.Where(x => x.IsEnabled(new SettingsHolder())))
        {
            coordinator.Register(registration);
        }

        consumer.Consume(coordinator);
    }

    static Trampoline.BehaviorContext CreatePrewiredSuccessContext(int depth)
    {
        var behaviors = new IBehavior[depth];
        var parts = new Trampoline.PipelinePart[depth];
        for (var i = 0; i < depth; i++)
        {
            behaviors[i] = new Trampoline.BehaviorTrampoline();
            parts[i] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, Trampoline.BehaviorTrampoline>();
        }

        return new Trampoline.BehaviorContext { Behaviors = behaviors, Parts = parts };
    }

    static Trampoline.BehaviorContext CreatePrewiredExceptionContext(int depth)
    {
        var behaviors = new IBehavior[depth];
        var parts = new Trampoline.PipelinePart[depth];
        for (var i = 0; i < depth - 1; i++)
        {
            behaviors[i] = new Trampoline.BehaviorTrampoline();
            parts[i] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, Trampoline.BehaviorTrampoline>();
        }

        var last = depth - 1;
        behaviors[last] = new Trampoline.ThrowingTrampoline();
        parts[last] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, Trampoline.ThrowingTrampoline>();
        return new Trampoline.BehaviorContext { Behaviors = behaviors, Parts = parts };
    }

    static Trampoline.BehaviorContext CreatePrewiredSyncExceptionContext(int depth)
    {
        var behaviors = new IBehavior[depth];
        var parts = new Trampoline.PipelinePart[depth];
        for (var i = 0; i < depth - 1; i++)
        {
            behaviors[i] = new Trampoline.BehaviorTrampoline();
            parts[i] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, Trampoline.BehaviorTrampoline>();
        }

        var last = depth - 1;
        behaviors[last] = new TrampolineSyncThrowingBehavior();
        parts[last] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, TrampolineSyncThrowingBehavior>();
        return new Trampoline.BehaviorContext { Behaviors = behaviors, Parts = parts };
    }

    static Trampoline.BehaviorContext CreatePrewiredReplayContext(int depth, int replayCount)
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

        return new Trampoline.BehaviorContext { Behaviors = behaviors, Parts = parts };
    }

    static Func<IBehaviorContext, Task> CreateDefaultInterfacesSuccessPipeline(int depth)
    {
        var behaviors = new IBehavior[depth];
        for (var i = 0; i < depth; i++)
        {
            behaviors[i] = new Behavior1SealedOptimization();
        }

        return PrewiredDefaultInterfaces.Build(behaviors);
    }

    static Func<IBehaviorContext, Task> CreateDefaultInterfacesExceptionPipeline(int depth)
    {
        var behaviors = new IBehavior[depth];
        for (var i = 0; i < depth - 1; i++)
        {
            behaviors[i] = new Behavior1SealedOptimization();
        }

        behaviors[depth - 1] = new DefaultInterfacesThrowingBehavior();
        return PrewiredDefaultInterfaces.Build(behaviors);
    }

    static Func<IBehaviorContext, Task> CreateDefaultInterfacesSyncExceptionPipeline(int depth)
    {
        var behaviors = new IBehavior[depth];
        for (var i = 0; i < depth - 1; i++)
        {
            behaviors[i] = new Behavior1SealedOptimization();
        }

        behaviors[depth - 1] = new DefaultInterfacesSyncThrowingBehavior();
        return PrewiredDefaultInterfaces.Build(behaviors);
    }

    static Func<IBehaviorContext, Task> CreateDefaultInterfacesReplayPipeline(int depth, int replayCount)
    {
        var behaviors = new IBehavior[depth];
        behaviors[0] = new DefaultInterfacesReplayBehavior(replayCount);
        for (var i = 1; i < depth; i++)
        {
            behaviors[i] = new Behavior1SealedOptimization();
        }

        return PrewiredDefaultInterfaces.Build(behaviors);
    }

    class CurrentBehaviorContext : ContextBag, IBehaviorContext
    {
        public ContextBag Extensions => this;
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
        public Task Invoke(Trampoline.IBehaviorContext context, Func<Trampoline.IBehaviorContext, Task> next) => throw new InvalidOperationException();
    }

    sealed class DefaultInterfacesThrowingBehavior : IBehavior<IBehaviorContext, IBehaviorContext>
    {
        public async Task Invoke(IBehaviorContext context, Func<IBehaviorContext, Task> next)
        {
            await Task.Yield();
            throw new InvalidOperationException();
        }
    }

    sealed class DefaultInterfacesSyncThrowingBehavior : IBehavior<IBehaviorContext, IBehaviorContext>
    {
        public Task Invoke(IBehaviorContext context, Func<IBehaviorContext, Task> next) => throw new InvalidOperationException();
    }

    sealed class DefaultInterfacesReplayBehavior(int replayCount) : IBehavior<IBehaviorContext, IBehaviorContext>
    {
        public async Task Invoke(IBehaviorContext context, Func<IBehaviorContext, Task> next)
        {
            for (var i = 0; i < replayCount; i++)
            {
                await next(context).ConfigureAwait(false);
            }
        }
    }
}
