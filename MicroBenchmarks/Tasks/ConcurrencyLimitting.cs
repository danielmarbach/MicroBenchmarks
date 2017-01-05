using System.IO;
using System.Threading;
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
    public class ConcurrencyLimitting
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


        [Benchmark]
        public async Task Packets()
        {
            var context = new Context();
            for (var i = 0; i < 100; i++)
            {
                var tasks = new Task[100];
                for (var j = 0; j < 100; j++)
                {
                    var options = new SendOptions();
                    tasks[j] = context.Send(new MyMessage { Counter = i }, options);
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        [Benchmark]
        public async Task Unbounded()
        {
            var context = new Context();
            var tasks = new Task[10000];

            for (var i = 0; i < 10000; i++)
            {
                var options = new SendOptions();
                tasks[i] = context.Send(new MyMessage { Counter = i }, options);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        [Benchmark]
        public async Task SemaphoreWaitInside()
        {
            var context = new Context();
            var semaphore = new SemaphoreSlim(100);

            var tasks = new Task[10000];
            for (var i = 0; i < 10000; i++)
            {
                tasks[i] = SendWaitInside(i, context, semaphore);
            }
            await  Task.WhenAll(tasks);
        }

        static async Task SendWaitInside(int counter, Context context, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var options = new SendOptions();
                await context.Send(new MyMessage { Counter = counter }, options).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        }

        [Benchmark]
        public async Task SemaphoreWaitOutside()
        {
            var context = new Context();
            var semaphore = new SemaphoreSlim(100);

            var tasks = new Task[10000];
            for (var i = 0; i < 10000; i++)
            {
                await semaphore.WaitAsync().ConfigureAwait(false);

                tasks[i] = SendWaitOutside(i, context, semaphore);
            }
            await Task.WhenAll(tasks);
        }

        static async Task SendWaitOutside(int counter, Context context, SemaphoreSlim semaphore)
        {
            try
            {
                var options = new SendOptions();
                await context.Send(new MyMessage { Counter = counter }, options).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        }

        class SendOptions { }

        class MyMessage
        {
            public int Counter { get; set; }
        }

        class Context
        {
            public async Task Send(MyMessage message, SendOptions options)
            {
                using (var streamWriter = File.CreateText(Path.GetFileName(Path.GetTempFileName())))
                {
                    await streamWriter.WriteLineAsync(message.Counter.ToString()).ConfigureAwait(false);
                }
            }
        }
    }
}