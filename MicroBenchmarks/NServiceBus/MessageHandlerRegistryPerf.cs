using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Exporters;

namespace MicroBenchmarks.NServiceBus
{
    [Config(typeof(Config))]
    public class MessageHandlerRegistryPerf
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(new BenchmarkDotNet.Diagnosers.MemoryDiagnoser());
                Add(StatisticColumn.AllStatistics);
            }
        }

        [Params(2, 4, 8, 16, 32, 64, 128, 256, 512, 1024)]
        public int Calls { get; set; }

        [Setup]
        public void SetUp()
        {
            registryBefore = SetupRegistryBeforeOptimizations();
            registryAfter = SetupRegistryAfterOptimizations();
        }

        private static MessageHandlerRegistryBeforeOptimizations SetupRegistryBeforeOptimizations()
        {
            var conventions = new MessageHandlerRegistryBeforeOptimizations.Conventions();
            conventions.AddSystemMessagesConventions(t => t == typeof(MessageHandlerRegistryBeforeOptimizations.MyMessage));

            var registry = new MessageHandlerRegistryBeforeOptimizations(conventions);
            registry.RegisterHandler(typeof(MessageHandlerRegistryBeforeOptimizations.Handler1));
            registry.RegisterHandler(typeof(MessageHandlerRegistryBeforeOptimizations.Handler2));
            registry.RegisterHandler(typeof(MessageHandlerRegistryBeforeOptimizations.Handler3));
            registry.RegisterHandler(typeof(MessageHandlerRegistryBeforeOptimizations.Handler4));
            registry.RegisterHandler(typeof(MessageHandlerRegistryBeforeOptimizations.Handler5));

            var handlers = registry.GetHandlersFor(typeof(MessageHandlerRegistryBeforeOptimizations.MyMessage));
            foreach (var messageHandler in handlers)
            {
            }
            return registry;
        }

        static MessageHandlerRegistryAfterOptimizations SetupRegistryAfterOptimizations()
        {
            var conventions = new MessageHandlerRegistryAfterOptimizations.Conventions();
            conventions.AddSystemMessagesConventions(t => t == typeof(MessageHandlerRegistryAfterOptimizations.MyMessage));

            var registry = new MessageHandlerRegistryAfterOptimizations(conventions);
            registry.RegisterHandler(typeof(MessageHandlerRegistryAfterOptimizations.Handler1));
            registry.RegisterHandler(typeof(MessageHandlerRegistryAfterOptimizations.Handler2));
            registry.RegisterHandler(typeof(MessageHandlerRegistryAfterOptimizations.Handler3));
            registry.RegisterHandler(typeof(MessageHandlerRegistryAfterOptimizations.Handler4));
            registry.RegisterHandler(typeof(MessageHandlerRegistryAfterOptimizations.Handler5));

            var handlers = registry.GetHandlersFor(typeof(MessageHandlerRegistryAfterOptimizations.MyMessage));
            foreach (var messageHandler in handlers)
            {
            }
            return registry;
        }

        private MessageHandlerRegistryBeforeOptimizations registryBefore;
        private MessageHandlerRegistryAfterOptimizations registryAfter;

        [Benchmark(Baseline = true)]
        public void V6_RegistryBeforeOptimizations()
        {
            for (int i = 0; i < Calls; i++)
            {
                var handlers = registryBefore.GetHandlersFor(typeof(MessageHandlerRegistryBeforeOptimizations.MyMessage));
                foreach (var messageHandler in handlers)
                {
                }
            }
        }

        [Benchmark]
        public void V6_RegistryAfterOptimizations()
        {
            for (int i = 0; i < Calls; i++)
            {
                var handlers = registryAfter.GetHandlersFor(typeof(MessageHandlerRegistryAfterOptimizations.MyMessage));
                foreach (var messageHandler in handlers)
                {
                }
            }

        }
    }
}