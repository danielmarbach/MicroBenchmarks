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
    public class TaskRunVsTaskFactoryClosureWithMoreStateUsingParameters
    {
        class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(new BenchmarkDotNet.Diagnosers.MemoryDiagnoser());
                Add(StatisticColumn.AllStatistics);
            }
        }

        State state1Field;
        State state2Field;
        State state3Field;
        State state4Field;
        State state5Field;

        [Setup]
        void Setup()
        {
            state1Field = new State();
            state2Field = new State();
            state3Field = new State();
            state4Field = new State();
            state5Field = new State();
        }

        [Benchmark]
        public Task TaskFactoryOneState() => TaskFactoryOne(state1Field);

        Task TaskFactoryOne(State state1)
        {
            return Task.Factory.StartNew(state =>
            {
                var externalState = (State)state;
                GC.KeepAlive(externalState);
            }, state1, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        [Benchmark]
        public Task TaskFactoryTwoState() => TaskFactoryTwo(state1Field, state2Field);

        Task TaskFactoryTwo(State state1, State state2)
        {
            return Task.Factory.StartNew(state =>
            {
                var externalState = (Tuple<State, State>)state;
                GC.KeepAlive(externalState);
            }, Tuple.Create(state1, state2), CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        [Benchmark]
        public Task TaskFactoryThreeState() => TaskFactoryThree(state1Field, state2Field, state3Field);

        Task TaskFactoryThree(State state1, State state2, State state3)
        {
            return Task.Factory.StartNew(state =>
            {
                var externalState = (Tuple<State, State, State>)state;
                GC.KeepAlive(externalState);
            }, Tuple.Create(state1, state2, state3), CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        [Benchmark]
        public Task TaskFactoryFourState() => TaskFactoryFour(state1Field, state2Field, state3Field, state4Field);

        Task TaskFactoryFour(State state1, State state2, State state3, State state4)
        {
            return Task.Factory.StartNew(state =>
            {
                var externalState = (Tuple<State, State, State, State>)state;
                GC.KeepAlive(externalState);
            }, Tuple.Create(state1, state2, state3, state4), CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        [Benchmark]
        public Task TaskFactoryFiveState() => TaskFactoryFive(state1Field, state2Field, state3Field, state4Field, state5Field);

        Task TaskFactoryFive(State state1, State state2, State state3, State state4, State state5)
        {
            return Task.Factory.StartNew(state =>
            {
                var externalState = (Tuple<State, State, State, State, State>)state;
                GC.KeepAlive(externalState);
            }, Tuple.Create(state1, state2, state3, state4, state5), CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        [Benchmark]
        public Task TaskRunOneState() => TaskRunOne(state1Field);

        Task TaskRunOne(State state1)
        {
            return Task.Run(() =>
            {
                GC.KeepAlive(state1);
            });
        }

        [Benchmark]
        public Task TaskRunTwoState() => TaskRunTwo(state1Field, state2Field);

        Task TaskRunTwo(State state1, State state2)
        {
            return Task.Run(() =>
            {
                GC.KeepAlive(state1);
                GC.KeepAlive(state2);
            });
        }

        [Benchmark]
        public Task TaskRunThreeState() => TaskRunThree(state1Field, state2Field, state3Field);

        Task TaskRunThree(State state1, State state2, State state3)
        {
            return Task.Run(() =>
            {
                GC.KeepAlive(state1);
                GC.KeepAlive(state2);
                GC.KeepAlive(state3);
            });
        }

        [Benchmark]
        public Task TaskRunFourState() => TaskRunFour(state1Field, state2Field, state3Field, state4Field);

        Task TaskRunFour(State state1, State state2, State state3, State state4)
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
        public Task TaskRunFiveState() => TaskRunFive(state1Field, state2Field, state3Field, state4Field, state5Field);

        Task TaskRunFive(State state1, State state2, State state3, State state4, State state5)
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

        class State { }
    }
}