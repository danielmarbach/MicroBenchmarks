using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.NServiceBus
{
    [Config(typeof(Config))]
    public class PipelineWarmup
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(new BenchmarkDotNet.Diagnosers.MemoryDiagnoser());
                Add(Job.Default);
                Add(Job.Default.With(new GcMode { Server = true }));
            }
        }

        private PipelineModifications pipelineModificationsBeforeOptimizations;
        private PipelineModifications pipelineModificationsAfterOptimizations;

        [Params(10, 20, 40)]
        public int PipelineDepth { get; set; }

        [Setup]
        public void SetUp()
        {
            pipelineModificationsBeforeOptimizations = new PipelineModifications();
            for (int i = 0; i < PipelineDepth; i++)
            {
                pipelineModificationsBeforeOptimizations.Additions.Add(RegisterStep.Create(i.ToString(), typeof(Behavior1BeforeOptimization), i.ToString(), b => new Behavior1BeforeOptimization()));
            }

            pipelineModificationsAfterOptimizations = new PipelineModifications();
            for (int i = 0; i < PipelineDepth; i++)
            {
                pipelineModificationsAfterOptimizations.Additions.Add(RegisterStep.Create(i.ToString(), typeof(Behavior1AfterOptimization), i.ToString(), b => new Behavior1AfterOptimization()));
            }
        }

        [Benchmark(Baseline = true)]
        public PipelineBeforeOptimization<IBehaviorContext> V6_PipelineBeforeOptimizations()
        {
            var pipeline = new PipelineBeforeOptimization<IBehaviorContext>(null, new SettingsHolder(),
                pipelineModificationsBeforeOptimizations);
            return pipeline;
        }

        [Benchmark]
        public PipelineAfterOptimizations<IBehaviorContext> V6_PipelineAfterOptimizations()
        {
            var pipeline = new PipelineAfterOptimizations<IBehaviorContext>(null, new SettingsHolder(),
                pipelineModificationsAfterOptimizations);
            return pipeline;
        }
    }
}