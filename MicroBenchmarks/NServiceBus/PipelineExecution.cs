using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.NServiceBus;

[Config(typeof(Config))]
public class PipelineExecution
{
    private class Config : ManualConfig
    {
        public Config()
        {
            Add(MarkdownExporter.GitHub);
            Add(MemoryDiagnoser.Default);
            Add(Job.Default);
        }
    }

    [Params(1)]
    public int Calls { get; set; }

    [Params(10, 20, 40)]
    public int PipelineDepth { get; set; }

    private BehaviorContext behaviorContext;
    private PipelineModifications pipelineModificationsBeforeOptimizations;
    private PipelineModifications pipelineModificationsAfterOptimizationsWithUnsafe;
    private PipelineAfterOptimizations<IBehaviorContext> pipelineBeforeOptimizations;
    private PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext> pipelineAfterOptimizationsWithUnsafe;

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

        pipelineModificationsAfterOptimizationsWithUnsafe = new PipelineModifications();
        for (int i = 0; i < PipelineDepth; i++)
        {
            pipelineModificationsAfterOptimizationsWithUnsafe.Additions.Add(RegisterStep.Create(i.ToString(),
                typeof(Behavior1AfterOptimization), i.ToString(), b => new Behavior1AfterOptimization()));
        }

        pipelineBeforeOptimizations = new PipelineAfterOptimizations<IBehaviorContext>(null, new SettingsHolder(),
            pipelineModificationsBeforeOptimizations);
        pipelineAfterOptimizationsWithUnsafe = new PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext>(null, new SettingsHolder(),
            pipelineModificationsAfterOptimizationsWithUnsafe);
            
        // warmup and cache
        pipelineBeforeOptimizations.Invoke(behaviorContext).GetAwaiter().GetResult();
        pipelineAfterOptimizationsWithUnsafe.Invoke(behaviorContext).GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true)]
    public async Task V8_PipelineBeforeOptimizations()
    {
        for (int i = 0; i < Calls; i++)
        {
            await pipelineBeforeOptimizations.Invoke(behaviorContext).ConfigureAwait(false);
        }
    }
        

    [Benchmark]
    public async Task V8_PipelineAfterOptimizationsWithUnsafe()
    {
        for (int i = 0; i < Calls; i++)
        {
            await pipelineAfterOptimizationsWithUnsafe.Invoke(behaviorContext).ConfigureAwait(false);
        }
    }

    class BehaviorContext : ContextBag, IBehaviorContext
    {
        public ContextBag Extensions => this;
    }
}