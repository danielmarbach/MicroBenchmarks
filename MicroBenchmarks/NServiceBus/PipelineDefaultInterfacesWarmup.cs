using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;

namespace MicroBenchmarks.NServiceBus;

[SimpleJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Alphabetical)]
public class PipelineDefaultInterfacesWarmup
{
    private PipelineModifications pipelineModifications;
    private Consumer consumer;
    private BehaviorContext behaviorContextDefaultInterfaces;

    [Params(10, 20, 40)]
    public int PipelineDepth { get; set; }

    [GlobalSetup]
    public void SetUp()
    {
        consumer = new Consumer();
        behaviorContextDefaultInterfaces = new BehaviorContext();

        pipelineModifications = new PipelineModifications();
        for (var i = 0; i < PipelineDepth; i++)
        {
            pipelineModifications.Additions.Add(RegisterStep.Create(i.ToString(),
                typeof(Behavior1SealedOptimization), i.ToString(), _ => new Behavior1SealedOptimization()));
        }
    }

    [Benchmark(Baseline = true)]
    public async Task Prewired_Warmup()
    {
        var prewiredBehaviors = new IBehavior[PipelineDepth];
        var prewiredParts = new Trampoline.PipelinePart[PipelineDepth];
        for (var i = 0; i < PipelineDepth; i++)
        {
            prewiredBehaviors[i] = new Trampoline.BehaviorTrampoline();
            prewiredParts[i] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, Trampoline.BehaviorTrampoline>();
        }

        var behaviorContextPrewired = new Trampoline.BehaviorContext
        {
            Behaviors = prewiredBehaviors,
            Parts = prewiredParts
        };

        ConsumeCoordinator();
        await Prewired.StageRunners.Start(behaviorContextPrewired).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task DefaultInterfaces_Warmup()
    {
        var defaultInterfacesBehaviors = new IBehavior[PipelineDepth];
        for (var i = 0; i < PipelineDepth; i++)
        {
            defaultInterfacesBehaviors[i] = new Behavior1SealedOptimization();
        }

        var pipeline = PrewiredDefaultInterfaces.Build(defaultInterfacesBehaviors);

        ConsumeCoordinator();
        await pipeline(behaviorContextDefaultInterfaces).ConfigureAwait(false);
    }

    void ConsumeCoordinator()
    {
        var coordinator = new StepRegistrationsCoordinator(pipelineModifications.Removals, pipelineModifications.Replacements);

        foreach (var registration in pipelineModifications.Additions.Where(x => x.IsEnabled(new SettingsHolder())))
        {
            coordinator.Register(registration);
        }

        consumer.Consume(coordinator);
    }

    class BehaviorContext : ContextBag, IBehaviorContext
    {
        public ContextBag Extensions => this;
    }
}
