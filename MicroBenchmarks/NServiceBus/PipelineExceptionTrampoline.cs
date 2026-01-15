using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace MicroBenchmarks.NServiceBus;

[SimpleJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Alphabetical)]
public class PipelineExceptionTrampoline
{
    private Trampoline.BehaviorContext behaviorContextTrampoline;
    private PipelineModifications currentPipelineModifications;
    private PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext> currentPipeline;
    private BehaviorContext behaviorContextCurrent;
    private Trampoline.PipelinePart[] trampolineParts;

    [Params(10, 20, 40)]
    public int PipelineDepth { get; set; }

    [GlobalSetup]
    public void SetUp()
    {
        behaviorContextCurrent = new BehaviorContext();

        currentPipelineModifications = new PipelineModifications();
        for (int i = 0; i < PipelineDepth; i++)
        {
            currentPipelineModifications.Additions.Add(RegisterStep.Create(i.ToString(),
                typeof(Behavior1SealedOptimization), i.ToString(), b => new Behavior1SealedOptimization()));
        }

        currentPipelineModifications.Additions.Add(RegisterStep.Create("Throwing", typeof(Throwing), "1",
            b => new Throwing()));

        currentPipeline = new PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext>(null, new SettingsHolder(),
            currentPipelineModifications);

        var trampolineBehaviors = new IBehavior[PipelineDepth+1];
        trampolineParts = new Trampoline.PipelinePart[PipelineDepth+1];
        for (var i = 0; i < PipelineDepth; i++)
        {
            trampolineBehaviors[i] = new Trampoline.BehaviorTrampoline();
            trampolineParts[i] = new Trampoline.PipelinePart(Trampoline.Behavior, i);
        }

        trampolineBehaviors[PipelineDepth] = new Throwing();
        trampolineParts[PipelineDepth] = new Trampoline.PipelinePart(Trampoline.Throwing, PipelineDepth);

        behaviorContextTrampoline = new Trampoline.BehaviorContext
        {
            Behaviors =  trampolineBehaviors,
        };

        // warmup and cache
        try
        {
            currentPipeline.Invoke(behaviorContextCurrent).GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
        }

        try
        {
            Trampoline.StageRunners.Start(behaviorContextTrampoline, trampolineParts).GetAwaiter().GetResult();
        }
        catch (Exception e)
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
    public async Task<Exception?> Trampo()
    {
        try
        {
            await Trampoline.StageRunners.Start(behaviorContextTrampoline, trampolineParts);
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