using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace MicroBenchmarks.NServiceBus;

[SimpleJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Alphabetical)]
public class PipelineExecutionWired
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

        currentPipeline = new PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext>(null, new SettingsHolder(),
            currentPipelineModifications);

        var wiredBehaviors = new IBehavior[PipelineDepth];
        wiredParts = new Trampoline.PipelinePart[PipelineDepth];
        for (var i = 0; i < PipelineDepth; i++)
        {
            wiredBehaviors[i] = new Trampoline.BehaviorTrampoline();
            wiredParts[i] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, Trampoline.BehaviorTrampoline>();
        }

        behaviorContextWired = new Trampoline.BehaviorContext
        {
            Behaviors = wiredBehaviors,
            Parts = wiredParts
        };

        // warmup and cache
        currentPipeline.Invoke(behaviorContextCurrent).GetAwaiter().GetResult();
        Prewired.StageRunners.Start(behaviorContextWired).GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true)]
    public async Task Current()
    {
        await currentPipeline.Invoke(behaviorContextCurrent).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task Wired()
    {
        await Prewired.StageRunners.Start(behaviorContextWired).ConfigureAwait(false);
    }

    class BehaviorContext : ContextBag, IBehaviorContext
    {
        public ContextBag Extensions => this;
    }
}
