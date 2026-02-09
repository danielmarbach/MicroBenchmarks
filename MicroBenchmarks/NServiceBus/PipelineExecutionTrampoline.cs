using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace MicroBenchmarks.NServiceBus;

[SimpleJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Alphabetical)]
public class PipelineExecutionTrampoline
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

        currentPipeline = new PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext>(null, new SettingsHolder(),
            currentPipelineModifications);

        var trampolineBehaviors = new IBehavior[PipelineDepth];
        trampolineParts = new Trampoline.PipelinePart[PipelineDepth];
        for (var i = 0; i < PipelineDepth; i++)
        {
            trampolineBehaviors[i] = new Trampoline.BehaviorTrampoline();
            trampolineParts[i] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, Trampoline.BehaviorTrampoline>();
        }

        behaviorContextTrampoline = new Trampoline.BehaviorContext
        {
            Behaviors =  trampolineBehaviors,
            Parts = trampolineParts,
        };

        // warmup and cache
        currentPipeline.Invoke(behaviorContextCurrent).GetAwaiter().GetResult();
        NServiceBus.Trampoline.StageRunners.Start(behaviorContextTrampoline).GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true)]
    public async Task Current()
    {
        await currentPipeline.Invoke(behaviorContextCurrent).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task Trampo()
    {
        await Trampoline.StageRunners.Start(behaviorContextTrampoline);
    }

    class BehaviorContext : ContextBag, IBehaviorContext
    {
        public ContextBag Extensions => this;
    }
}