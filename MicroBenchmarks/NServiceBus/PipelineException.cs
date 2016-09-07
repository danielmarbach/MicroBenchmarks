using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;

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
            }
        }

        private PipelineModifications pipelineModifications;
        private PipelineBeforeOptimization<IBehaviorContext> pipelineBeforeOptimizations;
        private PipelineAfterOptimizations<IBehaviorContext> pipelineAfterOptimizations;

        [Setup]
        public void SetUp()
        {
            pipelineModifications = new PipelineModifications();
            pipelineModifications.Additions.Add(RegisterStep.Create("1", typeof(Behavior1), "1", b => new Behavior1()));
            pipelineModifications.Additions.Add(RegisterStep.Create("2", typeof(Behavior1), "1", b => new Behavior1()));
            pipelineModifications.Additions.Add(RegisterStep.Create("3", typeof(Behavior1), "1", b => new Behavior1()));
            pipelineModifications.Additions.Add(RegisterStep.Create("4", typeof(Behavior1), "1", b => new Behavior1()));
            pipelineModifications.Additions.Add(RegisterStep.Create("5", typeof(Behavior1), "1", b => new Behavior1()));
            pipelineModifications.Additions.Add(RegisterStep.Create("6", typeof(Behavior1), "1", b => new Behavior1()));
            pipelineModifications.Additions.Add(RegisterStep.Create("7", typeof(Behavior1), "1", b => new Behavior1()));
            pipelineModifications.Additions.Add(RegisterStep.Create("8", typeof(Behavior1), "1", b => new Behavior1()));
            pipelineModifications.Additions.Add(RegisterStep.Create("9", typeof(Behavior1), "1", b => new Behavior1()));
            pipelineModifications.Additions.Add(RegisterStep.Create("10", typeof(Behavior1), "1", b => new Behavior1()));
            pipelineModifications.Additions.Add(RegisterStep.Create("11", typeof(Behavior1), "1", b => new Behavior1()));
            pipelineModifications.Additions.Add(RegisterStep.Create("12", typeof(Behavior1), "1", b => new Behavior1()));
            pipelineModifications.Additions.Add(RegisterStep.Create("13", typeof(Behavior1), "1", b => new Behavior1()));
            pipelineModifications.Additions.Add(RegisterStep.Create("14", typeof(Behavior1), "1", b => new Behavior1()));
            pipelineModifications.Additions.Add(RegisterStep.Create("15", typeof(Behavior1), "1", b => new Behavior1()));
            pipelineModifications.Additions.Add(RegisterStep.Create("16", typeof(Behavior1), "1", b => new Behavior1()));
            pipelineModifications.Additions.Add(RegisterStep.Create("17", typeof(Behavior1), "1", b => new Behavior1()));
            pipelineModifications.Additions.Add(RegisterStep.Create("18", typeof(Behavior1), "1", b => new Behavior1()));
            pipelineModifications.Additions.Add(RegisterStep.Create("19", typeof(Behavior1), "1", b => new Behavior1()));
            pipelineModifications.Additions.Add(RegisterStep.Create("20", typeof(Behavior1), "1", b => new Behavior1()));
            pipelineModifications.Additions.Add(RegisterStep.Create("21", typeof(Throwing), "1", b => new Throwing()));

            pipelineBeforeOptimizations = new PipelineBeforeOptimization<IBehaviorContext>(null, new SettingsHolder(),
                pipelineModifications);
            pipelineAfterOptimizations = new PipelineAfterOptimizations<IBehaviorContext>(null, new SettingsHolder(),
                pipelineModifications);
        }

        [Benchmark(Baseline = true)]
        public async Task V6_PipelineBeforeOptimizations()
        {
            try
            {
                await pipelineBeforeOptimizations.Invoke(null).ConfigureAwait(false);
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
                await pipelineAfterOptimizations.Invoke(null).ConfigureAwait(false);
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
    }
}