using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Exporters;

namespace MicroBenchmarks.NServiceBus
{
    [Config(typeof(Config))]
    public class MessageHandlerRegistrySize
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
        public MessageHandlerRegistryBeforeOptimizations V6_RegistryBeforeOptimizations()
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
            return registry;
        }

        [Benchmark]
        public MessageHandlerRegistryAfterOptimizations V6_RegistryAfterOptimizations()
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
            return registry;
        }
    }
}