using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Exporters;

namespace MicroBenchmarks.Tasks
{
    [Config(typeof(Config))]
    public class TaskRunVsTaskFactoryClosure
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

        private State willRequireClosure = new State();

        [Benchmark(Baseline = true)]
        public Task TaskFactoryWithoutClosure()
        {
            return Task.Factory.StartNew(state =>
            {
                var externalState = (State)state;
                GC.KeepAlive(externalState);
            }, willRequireClosure, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        [Benchmark]
        public Task TaskRunWithClosure()
        {
            return Task.Run(() =>
            {
                GC.KeepAlive(willRequireClosure);
            });
        }

        class State { }
    }
}