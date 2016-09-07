using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Exporters;

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
                Add(new MemoryDiagnoser());
            }
        }

        [Params(2, 4, 8, 16, 32, 64, 128, 256, 512, 1024)]
        public int Calls { get; set; }

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

            pipelineBeforeOptimizations = new PipelineBeforeOptimization<IBehaviorContext>(null, new SettingsHolder(),
                pipelineModifications);
            pipelineAfterOptimizations = new PipelineAfterOptimizations<IBehaviorContext>(null, new SettingsHolder(),
                pipelineModifications);
        }

        [Benchmark(Baseline = true)]
        public async Task V6_PipelineBeforeOptimizations()
        {
            for (int i = 0; i < Calls; i++)
            {
                await pipelineBeforeOptimizations.Invoke(null).ConfigureAwait(false);
            }
        }

        [Benchmark]
        public async Task V6_PipelineAfterOptimizations()
        {
            for (int i = 0; i < Calls; i++)
            {
                await pipelineAfterOptimizations.Invoke(null).ConfigureAwait(false);
            }
        }
    }
}