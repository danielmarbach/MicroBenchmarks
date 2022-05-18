using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using NServiceBus.Pipeline;

namespace MicroBenchmarks.NServiceBus
{
    [Config(typeof(Config))]
    public class PipelineException
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

        private BehaviorContext behaviorContext;
        private PipelineModifications pipelineModificationsBeforeOptimizations;
        private PipelineModifications pipelineModificationsAfterOptimizationsWithUnsafe;
        private PipelineAfterOptimizations<IBehaviorContext> pipelineBeforeOptimizations;
        private PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext> pipelineAfterOptimizationsWithUnsafe;

        [Params(10, 20, 40)]
        public int PipelineDepth { get; set; }

        [GlobalSetup]
        public void SetUp()
        {
            behaviorContext = new BehaviorContext();

            pipelineModificationsBeforeOptimizations = new PipelineModifications();
            for (int i = 0; i < PipelineDepth; i++)
            {
                pipelineModificationsBeforeOptimizations.Additions.Add(RegisterStep.Create(i.ToString(), typeof(Behavior1BeforeOptimization), i.ToString(), b => new Behavior1BeforeOptimization()));
            }
            var stepdId = PipelineDepth + 1;
            pipelineModificationsBeforeOptimizations.Additions.Add(RegisterStep.Create(stepdId.ToString(), typeof(Throwing), "1", b => new Throwing()));

            pipelineModificationsAfterOptimizationsWithUnsafe = new PipelineModifications();
            for (int i = 0; i < PipelineDepth; i++)
            {
                pipelineModificationsAfterOptimizationsWithUnsafe.Additions.Add(RegisterStep.Create(i.ToString(), typeof(Behavior1AfterOptimization), i.ToString(), b => new Behavior1AfterOptimization()));
            }
            pipelineModificationsAfterOptimizationsWithUnsafe.Additions.Add(RegisterStep.Create(stepdId.ToString(), typeof(Throwing), "1", b => new Throwing()));

            pipelineBeforeOptimizations = new PipelineAfterOptimizations<IBehaviorContext>(null, new SettingsHolder(),
                pipelineModificationsBeforeOptimizations);
            pipelineAfterOptimizationsWithUnsafe = new PipelineAfterOptimizationsUnsafeAndMemoryMarshal<IBehaviorContext>(null, new SettingsHolder(),
                pipelineModificationsAfterOptimizationsWithUnsafe);

            // warmup and cache
            try
            {
                pipelineBeforeOptimizations.Invoke(behaviorContext).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
            }
            try
            {
                pipelineAfterOptimizationsWithUnsafe.Invoke(behaviorContext).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
            }
        }

        [Benchmark(Baseline = true)]
        public async Task V8_PipelineBeforeOptimizations()
        {
            try
            {
                await pipelineBeforeOptimizations.Invoke(behaviorContext).ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {

            }
        }

        [Benchmark]
        public async Task V8_PipelineAfterOptimizationsWithUnsafe()
        {
            try
            {
                await pipelineAfterOptimizationsWithUnsafe.Invoke(behaviorContext).ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
            }
        }

        class Throwing : Behavior<IBehaviorContext>
        {
            public override Task Invoke(IBehaviorContext context, Func<Task> next)
            {
                throw new InvalidOperationException();
            }
        }
        class BehaviorContext : ContextBag, IBehaviorContext
        {
            public ContextBag Extensions => this;
        }
    }
}
