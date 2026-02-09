using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;

namespace MicroBenchmarks.NServiceBus;

[SimpleJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Alphabetical)]
// This benchmark is not entirely fair since the trampoline pipeline does not have the overhead of the initial pipeline warmup with expression trees and currently doesn't do coordination that's why it brings in fake coordination
public class PipelineWarmupTrampoline
{
    private PipelineModifications currentPipelineModifications;
    private BehaviorContext behaviorContextCurrent;
    private Consumer consumer;

    [Params(10, 20, 40)]
    public int PipelineDepth { get; set; }

    [GlobalSetup]
    public void SetUp()
    {
        consumer = new Consumer();
        behaviorContextCurrent = new BehaviorContext();

        currentPipelineModifications = new PipelineModifications();
        for (int i = 0; i < PipelineDepth; i++)
        {
            currentPipelineModifications.Additions.Add(RegisterStep.Create(i.ToString(),
                typeof(Behavior1SealedOptimization), i.ToString(), b => new Behavior1SealedOptimization()));
        }
    }

    [Benchmark(Baseline = true)]
    public async Task Current()
    {
        var currentPipeline = new PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext>(null, new SettingsHolder(),
            currentPipelineModifications);
        await currentPipeline.Invoke(behaviorContextCurrent).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task Trampo()
    {
        var trampolineBehaviors = new IBehavior[PipelineDepth];
        var trampolineParts = new Trampoline.PipelinePart[PipelineDepth];
        for (var i = 0; i < PipelineDepth; i++)
        {
            trampolineBehaviors[i] = new Trampoline.BehaviorTrampoline();
            trampolineParts[i] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, Trampoline.BehaviorTrampoline>();
        }

        var behaviorContextTrampoline = new Trampoline.BehaviorContext
        {
            Behaviors =  trampolineBehaviors,
            Parts =  trampolineParts
        };

        var coordinator = new StepRegistrationsCoordinator(currentPipelineModifications.Removals,
            currentPipelineModifications.Replacements);

        foreach (var rego in currentPipelineModifications.Additions.Where(x => x.IsEnabled(new SettingsHolder())))
        {
            coordinator.Register(rego);
        }

        consumer.Consume(coordinator);

        await Trampoline.StageRunners.Start(behaviorContextTrampoline);
    }

    class BehaviorContext : ContextBag, IBehaviorContext
    {
        public ContextBag Extensions => this;
    }
}