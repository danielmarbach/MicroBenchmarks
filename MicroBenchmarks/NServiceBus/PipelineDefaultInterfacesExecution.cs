using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace MicroBenchmarks.NServiceBus;

[SimpleJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Alphabetical)]
public class PipelineDefaultInterfacesExecution
{
    private Trampoline.BehaviorContext behaviorContextPrewired;
    private Func<IBehaviorContext, Task> defaultInterfacesPipeline;
    private BehaviorContext behaviorContextDefaultInterfaces;

    [Params(10, 20, 40)]
    public int PipelineDepth { get; set; }

    [GlobalSetup]
    public void SetUp()
    {
        var prewiredBehaviors = new IBehavior[PipelineDepth];
        var prewiredParts = new Trampoline.PipelinePart[PipelineDepth];
        for (var i = 0; i < PipelineDepth; i++)
        {
            prewiredBehaviors[i] = new Trampoline.BehaviorTrampoline();
            prewiredParts[i] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, Trampoline.BehaviorTrampoline>();
        }

        behaviorContextPrewired = new Trampoline.BehaviorContext
        {
            Behaviors = prewiredBehaviors,
            Parts = prewiredParts
        };

        var defaultInterfacesBehaviors = new IBehavior[PipelineDepth];
        for (var i = 0; i < PipelineDepth; i++)
        {
            defaultInterfacesBehaviors[i] = new Behavior1SealedOptimization();
        }

        defaultInterfacesPipeline = PrewiredDefaultInterfaces.Build(defaultInterfacesBehaviors);
        behaviorContextDefaultInterfaces = new BehaviorContext();

        Prewired.StageRunners.Start(behaviorContextPrewired).GetAwaiter().GetResult();
        defaultInterfacesPipeline(behaviorContextDefaultInterfaces).GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true)]
    public async Task Prewired_Path()
    {
        await Prewired.StageRunners.Start(behaviorContextPrewired).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task DefaultInterfaces_Path()
    {
        await defaultInterfacesPipeline(behaviorContextDefaultInterfaces).ConfigureAwait(false);
    }

    class BehaviorContext : ContextBag, IBehaviorContext
    {
        public ContextBag Extensions => this;
    }
}
