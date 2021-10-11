using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using MicroBenchmarks.RabbitMQ;

namespace MicroBenchmarks.Events
{
    [Config(typeof(Config))]
    public class NullableEvents
    {
        private readonly Consumer consumer = new();
        
        private class Config : ManualConfig
        {
            public Config()
            {
                AddExporter(MarkdownExporter.GitHub);
                AddDiagnoser(MemoryDiagnoser.Default);
                AddColumn(StatisticColumn.AllStatistics);
            }
        }

        [Benchmark(Baseline = true)]
        public void NullableEventInvoke()
        {
            NullableEvent?.Invoke(consumer, EventArgs.Empty);
        }
        
        [Benchmark]
        public void NullableEvent_WithOneRegisteredDelegate_Invoke()
        {
            NullableEvent += static (sender, args) => ((Consumer)sender!).Consume(args);
            
            NullableEvent?.Invoke(consumer, EventArgs.Empty);
        }

        [Benchmark]
        public void EventWithDefaultDelegateInvoke()
        {
            EventWithDefaultDelegate(consumer, EventArgs.Empty);
        }
        
        [Benchmark]
        public void EventWithDefaultDelegate_WithOneRegisteredDelegate_Invoke()
        {
            EventWithDefaultDelegate += static (sender, args) => ((Consumer)sender!).Consume(args);
            
            EventWithDefaultDelegate(consumer, EventArgs.Empty);
        }

        public event EventHandler? NullableEvent;
        public event EventHandler EventWithDefaultDelegate = delegate { };
    }
}