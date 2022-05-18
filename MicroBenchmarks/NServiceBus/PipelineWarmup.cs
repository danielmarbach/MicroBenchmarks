using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using NServiceBus.Pipeline;

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
                Add(MemoryDiagnoser.Default);
                Add(Job.ShortRun);
                Add(Job.ShortRun.With(new GcMode { Server = true }));
            }
        }

        private PipelineModifications pipelineModificationsBeforeOptimizations;
        private PipelineModifications pipelineModificationsAfterOptimizationsWithUnsafe;

        [Params(10, 20, 40)]
        public int PipelineDepth { get; set; }

        [GlobalSetup]
        public void SetUp()
        {
            pipelineModificationsBeforeOptimizations = new PipelineModifications();
            for (int i = 0; i < PipelineDepth; i++)
            {
                pipelineModificationsBeforeOptimizations.Additions.Add(RegisterStep.Create(i.ToString(), typeof(Behavior1BeforeOptimization), i.ToString(), b => new Behavior1BeforeOptimization()));
            }

            pipelineModificationsAfterOptimizationsWithUnsafe = new PipelineModifications();
            for (int i = 0; i < PipelineDepth; i++)
            {
                pipelineModificationsAfterOptimizationsWithUnsafe.Additions.Add(RegisterStep.Create(i.ToString(), typeof(Behavior1AfterOptimization), i.ToString(), b => new Behavior1AfterOptimization()));
            }
        }

        [Benchmark(Baseline = true)]
        public PipelineAfterOptimizations<IBehaviorContext> V8_PipelineBeforeOptimizations()
        {
            var pipeline = new PipelineAfterOptimizations<IBehaviorContext>(null, new SettingsHolder(),
                pipelineModificationsBeforeOptimizations);
            return pipeline;
        }

        [Benchmark]
        public PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext> V8_PipelineAfterOptimizationsWithUnsafe()
        {
            var pipeline = new PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext>(null, new SettingsHolder(),
                pipelineModificationsAfterOptimizationsWithUnsafe);
            return pipeline;
        }
    }
}