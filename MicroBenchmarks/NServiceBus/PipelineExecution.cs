using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
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
                Add(Job.ShortRun);
                Add(Job.ShortRun.With(new GcMode { Server = true }));
            }
        }

        [Params(20000, 40000, 80000, 160000, 320000)]
        public int Calls { get; set; }

        [Params(10, 20, 40)]
        public int PipelineDepth { get; set; }

        private BehaviorContext behaviorContext;
        private PipelineModifications pipelineModificationsBeforeOptimizations;
        private PipelineModifications pipelineModificationsAfterOptimizations;
        private PipelineBeforeOptimization<IBehaviorContext> pipelineBeforeOptimizations;
        private PipelineAfterOptimizations<IBehaviorContext> pipelineAfterOptimizations;
        private PipelineFastExpressionCompiler<IBehaviorContext> pipelineAfterOptimizationsFastExpressionCompiler;

        [GlobalSetup]
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
            pipelineAfterOptimizationsFastExpressionCompiler = new PipelineFastExpressionCompiler<IBehaviorContext>(null, new SettingsHolder(),
                pipelineModificationsAfterOptimizations);

            // warmup and cache
            pipelineBeforeOptimizations.Invoke(behaviorContext).GetAwaiter().GetResult();
            pipelineAfterOptimizations.Invoke(behaviorContext).GetAwaiter().GetResult();
            pipelineAfterOptimizationsFastExpressionCompiler.Invoke(behaviorContext).GetAwaiter().GetResult();
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

        [Benchmark]
        public async Task V6_PipelineAfterOptimizationsFastExpressionCompiler()
        {
            for (int i = 0; i < Calls; i++)
            {
                await pipelineAfterOptimizationsFastExpressionCompiler.Invoke(behaviorContext).ConfigureAwait(false);
            }
        }

        class BehaviorContext : IBehaviorContext {}
    }
}