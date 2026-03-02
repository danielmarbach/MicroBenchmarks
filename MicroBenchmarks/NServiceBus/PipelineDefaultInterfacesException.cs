using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace MicroBenchmarks.NServiceBus;

[SimpleJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Alphabetical)]
public class PipelineDefaultInterfacesException
{
    private Trampoline.BehaviorContext behaviorContextPrewired;
    private Func<IBehaviorContext, Task> defaultInterfacesPipeline;
    private BehaviorContext behaviorContextDefaultInterfaces;

    [Params(10, 20, 40)]
    public int PipelineDepth { get; set; }

    [GlobalSetup]
    public void SetUp()
    {
        var prewiredBehaviors = new IBehavior[PipelineDepth + 1];
        var prewiredParts = new Trampoline.PipelinePart[PipelineDepth + 1];
        for (var i = 0; i < PipelineDepth; i++)
        {
            prewiredBehaviors[i] = new Trampoline.BehaviorTrampoline();
            prewiredParts[i] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, Trampoline.BehaviorTrampoline>();
        }

        prewiredBehaviors[PipelineDepth] = new Trampoline.ThrowingTrampoline();
        prewiredParts[PipelineDepth] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, Trampoline.ThrowingTrampoline>();

        behaviorContextPrewired = new Trampoline.BehaviorContext
        {
            Behaviors = prewiredBehaviors,
            Parts = prewiredParts
        };

        var defaultInterfacesBehaviors = new IBehavior[PipelineDepth + 1];
        for (var i = 0; i < PipelineDepth; i++)
        {
            defaultInterfacesBehaviors[i] = new Behavior1SealedOptimization();
        }

        defaultInterfacesBehaviors[PipelineDepth] = new ThrowingBehavior();
        defaultInterfacesPipeline = PrewiredDefaultInterfaces.Build(defaultInterfacesBehaviors);
        behaviorContextDefaultInterfaces = new BehaviorContext();

        try
        {
            Prewired.StageRunners.Start(behaviorContextPrewired).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
        }

        try
        {
            defaultInterfacesPipeline(behaviorContextDefaultInterfaces).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
        }
    }

    [Benchmark(Baseline = true)]
    public async Task<Exception?> Prewired_Path()
    {
        try
        {
            await Prewired.StageRunners.Start(behaviorContextPrewired).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ex;
        }

        return null;
    }

    [Benchmark]
    public async Task<Exception?> DefaultInterfaces_Path()
    {
        try
        {
            await defaultInterfacesPipeline(behaviorContextDefaultInterfaces).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ex;
        }

        return null;
    }

    sealed class ThrowingBehavior : IBehavior<IBehaviorContext, IBehaviorContext>
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
