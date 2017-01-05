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
    public class TaskRunVsTaskFactoryClosureWithMoreState
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

        private State1 state1 = new State1();
        private State2 state2 = new State2();
        private State3 state3 = new State3();
        private State4 state4 = new State4();
        private State5 state5 = new State5();

        [Benchmark()]
        public Task TaskFactoryOneState()
        {
            return Task.Factory.StartNew(state =>
            {
                var externalState = (State1)state;
                GC.KeepAlive(externalState);
            }, state1, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        [Benchmark()]
        public Task TaskFactoryTwoState()
        {
            return Task.Factory.StartNew(state =>
            {
                var externalState = (Tuple<State1, State2>)state;
                GC.KeepAlive(externalState.Item1);
                GC.KeepAlive(externalState.Item2);
            }, Tuple.Create(state1, state2), CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        [Benchmark()]
        public Task TaskFactoryThreeState()
        {
            return Task.Factory.StartNew(state =>
            {
                var externalState = (Tuple<State1, State2, State3>)state;
                GC.KeepAlive(externalState.Item1);
                GC.KeepAlive(externalState.Item2);
                GC.KeepAlive(externalState.Item3);
            }, Tuple.Create(state1, state2, state3), CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        [Benchmark()]
        public Task TaskFactoryFourState()
        {
            return Task.Factory.StartNew(state =>
            {
                var externalState = (Tuple<State1, State2, State3, State4>)state;
                GC.KeepAlive(externalState.Item1);
                GC.KeepAlive(externalState.Item2);
                GC.KeepAlive(externalState.Item3);
                GC.KeepAlive(externalState.Item4);
            }, Tuple.Create(state1, state2, state3, state4), CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        [Benchmark()]
        public Task TaskFactoryFivSetate()
        {
            return Task.Factory.StartNew(state =>
            {
                var externalState = (Tuple<State1, State2, State3, State4, State5>)state;
                GC.KeepAlive(externalState.Item1);
                GC.KeepAlive(externalState.Item2);
                GC.KeepAlive(externalState.Item3);
                GC.KeepAlive(externalState.Item4);
                GC.KeepAlive(externalState.Item5);
            }, Tuple.Create(state1, state2, state3, state4, state5), CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        [Benchmark]
        public Task TaskRunOneState()
        {
            return Task.Run(() =>
            {
                GC.KeepAlive(state1);
            });
        }

        [Benchmark]
        public Task TaskRunTwoState()
        {
            return Task.Run(() =>
            {
                GC.KeepAlive(state1);
                GC.KeepAlive(state2);
            });
        }

        [Benchmark]
        public Task TaskRunThreeState()
        {
            return Task.Run(() =>
            {
                GC.KeepAlive(state1);
                GC.KeepAlive(state2);
                GC.KeepAlive(state3);
            });
        }

        [Benchmark]
        public Task TaskRunFourState()
        {
            return Task.Run(() =>
            {
                GC.KeepAlive(state1);
                GC.KeepAlive(state2);
                GC.KeepAlive(state3);
                GC.KeepAlive(state4);
            });
        }

        [Benchmark]
        public Task TaskRunFiveState()
        {
            return Task.Run(() =>
            {
                GC.KeepAlive(state1);
                GC.KeepAlive(state2);
                GC.KeepAlive(state3);
                GC.KeepAlive(state4);
                GC.KeepAlive(state5);
            });
        }

        class State1 { }
        class State2 { }
        class State3 { }
        class State4 { }
        class State5 { }
    }
}