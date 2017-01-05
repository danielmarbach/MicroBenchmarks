using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks
{
    [Config(typeof(Config))]
    public class BatchedVsImmediateSimulated
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(new BenchmarkDotNet.Diagnosers.MemoryDiagnoser());
                Add(Job.Default.With(Platform.X64).WithTargetCount(1).WithWarmupCount(1));
            }
        }

        [Params(2, 4, 8, 16, 32, 64)]
        public int Concurrency { get; set; }


        [Benchmark]
        public async Task ConcurrentBatched()
        {
            var concurrentStack = new ConcurrentStack<MyMessage>();
            var context = new Context();

            var batches = new Task[Concurrency];
            for (var i = 0; i < Concurrency; i++)
            {
                batches[i] = context.Send(new MyMessage { Counter = i }, concurrentStack);
            }
            await Task.WhenAll(batches).ConfigureAwait(false);


            var actualSends = new List<Task>(Concurrency);
            foreach (var message in concurrentStack)
            {
                actualSends.Add(context.ActualSend(message));
            }
            await Task.WhenAll(actualSends).ConfigureAwait(false);
        }

        [Benchmark]
        public async Task SequentialBatched()
        {
            var concurrentStack = new ConcurrentStack<MyMessage>();
            var context = new Context();

            for (var i = 0; i < Concurrency; i++)
            {
                await context.Send(new MyMessage { Counter = i }, concurrentStack).ConfigureAwait(false);
            }

            var actualSends = new List<Task>(Concurrency);
            foreach (var message in concurrentStack)
            {
                actualSends.Add(context.ActualSend(message));
            }
            await Task.WhenAll(actualSends).ConfigureAwait(false);
        }

        class MyMessage
        {
            public int Counter { get; set; }
        }

        class Context
        {
            public Task Send(MyMessage message, ConcurrentStack<MyMessage> stack)
            {
                stack.Push(message);
                return Task.CompletedTask; // assumes nothing else is happening
            }

            public async Task ActualSend(MyMessage message)
            {
                using (var streamWriter = File.CreateText(Path.GetFileName(Path.GetTempFileName())))
                {
                    await streamWriter.WriteLineAsync(message.Counter.ToString()).ConfigureAwait(false);
                }
            }
        }
    }
}