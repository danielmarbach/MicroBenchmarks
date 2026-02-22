using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace MicroBenchmarks.NServiceBus;

[SimpleJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Alphabetical)]
public class PipelineExceptionWired
{
    private Trampoline.BehaviorContext behaviorContextWired;
    private PipelineModifications currentPipelineModifications;
    private PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext> currentPipeline;
    private BehaviorContext behaviorContextCurrent;
    private Trampoline.PipelinePart[] wiredParts;

    [Params(10, 20, 40)]
    public int PipelineDepth { get; set; }

    [GlobalSetup]
    public void SetUp()
    {
        behaviorContextCurrent = new BehaviorContext();

        currentPipelineModifications = new PipelineModifications();
        for (var i = 0; i < PipelineDepth; i++)
        {
            currentPipelineModifications.Additions.Add(RegisterStep.Create(i.ToString(),
                typeof(Behavior1SealedOptimization), i.ToString(), _ => new Behavior1SealedOptimization()));
        }

        currentPipelineModifications.Additions.Add(RegisterStep.Create("Throwing", typeof(Throwing), "1",
            _ => new Throwing()));

        currentPipeline = new PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext>(null, new SettingsHolder(),
            currentPipelineModifications);

        var wiredBehaviors = new IBehavior[PipelineDepth + 1];
        wiredParts = new Trampoline.PipelinePart[PipelineDepth + 1];
        for (var i = 0; i < PipelineDepth; i++)
        {
            wiredBehaviors[i] = new Trampoline.BehaviorTrampoline();
            wiredParts[i] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, Trampoline.BehaviorTrampoline>();
        }

        wiredBehaviors[PipelineDepth] = new Trampoline.ThrowingTrampoline();
        wiredParts[PipelineDepth] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, Trampoline.ThrowingTrampoline>();

        behaviorContextWired = new Trampoline.BehaviorContext
        {
            Behaviors = wiredBehaviors,
            Parts = wiredParts
        };

        // warmup and cache
        try
        {
            currentPipeline.Invoke(behaviorContextCurrent).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
        }

        try
        {
            Prewired.StageRunners.Start(behaviorContextWired).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
        }
    }

    [Benchmark(Baseline = true)]
    public async Task<Exception?> Current()
    {
        try
        {
            await currentPipeline.Invoke(behaviorContextCurrent).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            return e;
        }

        return null;
    }

    [Benchmark]
    public async Task<Exception?> Wired()
    {
        try
        {
            await Prewired.StageRunners.Start(behaviorContextWired).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            return e;
        }

        return null;
    }

    public sealed class Throwing : IBehavior<IBehaviorContext, IBehaviorContext>
    {
        public async Task Invoke(IBehaviorContext context, Func<IBehaviorContext, Task> next)
        {
            await Task.Yield();
            throw new InvalidOperationException();
        }
    }

    class BehaviorContext : ContextBag, IBehaviorContext
    {
        public ContextBag Extensions => this;
    }
}
