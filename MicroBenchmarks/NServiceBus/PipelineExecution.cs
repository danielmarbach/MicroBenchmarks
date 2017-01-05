using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.NServiceBus
{
    [Config(typeof(Config))]
    public class PipelineExecution
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(new BenchmarkDotNet.Diagnosers.MemoryDiagnoser());
                Add(Job.Default.With(RunStrategy.ColdStart).WithLaunchCount(1).WithWarmupCount(1).WithTargetCount(1));
                Add(Job.Default.With(RunStrategy.ColdStart).WithLaunchCount(1).WithWarmupCount(1).WithTargetCount(1).With(new GcMode { Server = true }));
            }
        }

        [Params(20000, 40000, 80000, 160000, 320000, 640000, 1280000)]
        public int Calls { get; set; }

        [Params(10, 20, 40)]
        public int PipelineDepth { get; set; }

        private BehaviorContext behaviorContext;
        private PipelineModifications pipelineModificationsBeforeOptimizations;
        private PipelineModifications pipelineModificationsAfterOptimizations;
        private PipelineBeforeOptimization<IBehaviorContext> pipelineBeforeOptimizations;
        private PipelineAfterOptimizations<IBehaviorContext> pipelineAfterOptimizations;

        [Setup]
        public void SetUp()
        {
            behaviorContext = new BehaviorContext();

            pipelineModificationsBeforeOptimizations = new PipelineModifications();
            for (int i = 0; i < PipelineDepth; i++)
            {
                pipelineModificationsBeforeOptimizations.Additions.Add(RegisterStep.Create(i.ToString(),
                    typeof(Behavior1BeforeOptimization), i.ToString(), b => new Behavior1BeforeOptimization()));
            }

            pipelineModificationsAfterOptimizations = new PipelineModifications();
            for (int i = 0; i < PipelineDepth; i++)
            {
                pipelineModificationsAfterOptimizations.Additions.Add(RegisterStep.Create(i.ToString(),
                    typeof(Behavior1AfterOptimization), i.ToString(), b => new Behavior1AfterOptimization()));
            }

            pipelineBeforeOptimizations = new PipelineBeforeOptimization<IBehaviorContext>(null, new SettingsHolder(),
                pipelineModificationsBeforeOptimizations);
            pipelineAfterOptimizations = new PipelineAfterOptimizations<IBehaviorContext>(null, new SettingsHolder(),
                pipelineModificationsAfterOptimizations);

            // warmup and cache
            pipelineBeforeOptimizations.Invoke(behaviorContext).GetAwaiter().GetResult();
            pipelineAfterOptimizations.Invoke(behaviorContext).GetAwaiter().GetResult();
        }

        [Benchmark(Baseline = true)]
        public async Task V6_PipelineBeforeOptimizations()
        {
            for (int i = 0; i < Calls; i++)
            {
                await pipelineBeforeOptimizations.Invoke(behaviorContext).ConfigureAwait(false);
            }
        }

        [Benchmark]
        public async Task V6_PipelineAfterOptimizations()
        {
            for (int i = 0; i < Calls; i++)
            {
                await pipelineAfterOptimizations.Invoke(behaviorContext).ConfigureAwait(false);
            }
        }

        class BehaviorContext : IBehaviorContext {}
    }
}