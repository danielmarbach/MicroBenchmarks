using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

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
                Add(new BenchmarkDotNet.Diagnosers.MemoryDiagnoser());
                Add(Job.Default);
                Add(Job.Default.With(new GcMode { Server = true }));
            }
        }

        private BehaviorContext behaviorContext;
        private PipelineModifications pipelineModificationsBeforeOptimizations;
        private PipelineModifications pipelineModificationsAfterOptimizations;
        private PipelineBeforeOptimization<IBehaviorContext> pipelineBeforeOptimizations;
        private PipelineAfterOptimizations<IBehaviorContext> pipelineAfterOptimizations;

        [Params(10, 20, 40)]
        public int PipelineDepth { get; set; }

        [Setup]
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

            pipelineModificationsAfterOptimizations = new PipelineModifications();
            for (int i = 0; i < PipelineDepth; i++)
            {
                pipelineModificationsAfterOptimizations.Additions.Add(RegisterStep.Create(i.ToString(), typeof(Behavior1AfterOptimization), i.ToString(), b => new Behavior1AfterOptimization()));
            }

            pipelineModificationsAfterOptimizations.Additions.Add(RegisterStep.Create(stepdId.ToString(), typeof(Throwing), "1", b => new Throwing()));

            pipelineBeforeOptimizations = new PipelineBeforeOptimization<IBehaviorContext>(null, new SettingsHolder(),
                pipelineModificationsBeforeOptimizations);
            pipelineAfterOptimizations = new PipelineAfterOptimizations<IBehaviorContext>(null, new SettingsHolder(),
                pipelineModificationsAfterOptimizations);

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
                pipelineAfterOptimizations.Invoke(behaviorContext).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
            }
        }

        [Benchmark(Baseline = true)]
        public async Task V6_PipelineBeforeOptimizations()
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
        public async Task V6_PipelineAfterOptimizations()
        {
            try
            {
                await pipelineAfterOptimizations.Invoke(behaviorContext).ConfigureAwait(false);
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
        class BehaviorContext : IBehaviorContext { }
    }
}