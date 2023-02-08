using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.NServiceBus;

[Config(typeof(Config))]
public class PipelineExecutionSealed
{
    private class Config : ManualConfig
    {
        public Config()
        {
            Add(MarkdownExporter.GitHub);
            Add(MemoryDiagnoser.Default);
            Add(Job.LongRun);
        }
    }

    [Params(20000, 40000, 80000)]
    public int Calls { get; set; }

    [Params(10, 20, 40)]
    public int PipelineDepth { get; set; }

    private BehaviorContext behaviorContext;
    private PipelineModifications pipelineModificationsBeforeOptimizations;
    private PipelineModifications pipelineModificationsAfterOptimizations;
    private PipelineAfterOptimizationsUnsafe<IBehaviorContext> pipelineBeforeOptimizations;
    private PipelineAfterOptimizationsUnsafe<IBehaviorContext> pipelineAfterOptimizations;

    [GlobalSetup]
    public void SetUp()
    {
        behaviorContext = new BehaviorContext();

        pipelineModificationsBeforeOptimizations = new PipelineModifications();
        for (int i = 0; i < PipelineDepth; i++)
        {
            pipelineModificationsBeforeOptimizations.Additions.Add(RegisterStep.Create(i.ToString(),
                typeof(Behavior1AfterOptimization), i.ToString(), b => new Behavior1AfterOptimization()));
        }

        pipelineModificationsAfterOptimizations = new PipelineModifications();
        for (int i = 0; i < PipelineDepth; i++)
        {
            pipelineModificationsAfterOptimizations.Additions.Add(RegisterStep.Create(i.ToString(),
                typeof(Behavior1SealedOptimization), i.ToString(), b => new Behavior1SealedOptimization()));
        }
            
        pipelineBeforeOptimizations = new PipelineAfterOptimizationsUnsafe<IBehaviorContext>(null, new SettingsHolder(),
            pipelineModificationsBeforeOptimizations);
        pipelineAfterOptimizations = new PipelineAfterOptimizationsUnsafe<IBehaviorContext>(null, new SettingsHolder(),
            pipelineModificationsAfterOptimizations);
            
        // warmup and cache
        pipelineBeforeOptimizations.Invoke(behaviorContext).GetAwaiter().GetResult();
        pipelineAfterOptimizations.Invoke(behaviorContext).GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true)]
    public async Task V8_PipelineWithoutSealedBehaviors()
    {
        for (int i = 0; i < Calls; i++)
        {
            await pipelineBeforeOptimizations.Invoke(behaviorContext).ConfigureAwait(false);
        }
    }
        
    [Benchmark]
    public async Task V8_PipelineWithSealedBehaviors()
    {
        for (int i = 0; i < Calls; i++)
        {
            await pipelineAfterOptimizations.Invoke(behaviorContext).ConfigureAwait(false);
        }
    }
        
    class BehaviorContext : ContextBag, IBehaviorContext
    {
        public ContextBag Extensions => this;
    }
}