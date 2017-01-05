using System;
using System.Collections.Generic;
using System.IO;
using System.Messaging;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.NServiceBus
{
    [Config(typeof(Config))]
    public class MsmqBodyStream
    {
        private MessageQueue queue;

        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(new BenchmarkDotNet.Diagnosers.MemoryDiagnoser());
                Add(Job.Default.With(RunStrategy.ColdStart).With(Platform.X64).WithLaunchCount(1).WithWarmupCount(1).WithTargetCount(1));
                Add(Job.Default.With(RunStrategy.ColdStart).With(Platform.X86).WithLaunchCount(1).WithWarmupCount(1).WithTargetCount(1));
            }
        }

        [Params(256, 512, 1024)]
        public int BodySize { get; set; }

        [Params(1000, 10000, 20000)]
        public int NumberOfMessages { get; set; }

        [Setup]
        public void SetUp()
        {
            var random = new Random();

            var queueName = $".\\Private$\\{nameof(MsmqBodyStream).ToLowerInvariant()}";
            if (!MessageQueue.Exists(queueName))
            {
                queue = MessageQueue.Create(queueName, false);
            }
            else
            {
                queue = new MessageQueue(queueName, QueueAccessMode.SendAndReceive);
            }

            queue.Purge();

            for (int i = 0; i < NumberOfMessages; i++)
            {
                var content = new byte[BodySize];
                random.NextBytes(content);

                using (var body = new MemoryStream(content))
                {
                    queue.Send(new Message { BodyStream = body }, MessageQueueTransactionType.None);
                }
            }
        }

        [Benchmark(Baseline = true)]
        public async Task<List<byte[]>> V6_BodyStreamAccess()
        {
            var bodies = new List<byte[]>(NumberOfMessages);
            var enumerator = queue.GetMessageEnumerator2();
            while (enumerator.MoveNext())
            {
                var message = queue.ReceiveById(enumerator.Current.Id);
                var content = await ReadStream(message.BodyStream).ConfigureAwait(false);
                bodies.Add(content);
                message.Dispose();
            }
            return bodies;
        }

        [Benchmark]
        public List<ArraySegment<byte>> V6_BodyDirectAccess()
        {
            var bodies = new List<ArraySegment<byte>>(NumberOfMessages);
            var enumerator = queue.GetMessageEnumerator2();
            while (enumerator.MoveNext())
            {
                var message = queue.ReceiveById(enumerator.Current.Id);
                var content = MsmqUtilities.GetBodyAsArraySegment(message);
                bodies.Add(content);
                message.Dispose();
            }
            return bodies;
        }

        static async Task<byte[]> ReadStream(Stream bodyStream)
        {
            bodyStream.Seek(0, SeekOrigin.Begin);
            var length = (int)bodyStream.Length;
            var body = new byte[length];
            await bodyStream.ReadAsync(body, 0, length).ConfigureAwait(false);
            return body;
        }
    }

    static class MsmqUtilities
    {
        static MsmqUtilities()
        {
            var getRawBytesSegment = new DynamicMethod(nameof(GetAsArraySegment), typeof(ArraySegment<byte>),
                new[] {typeof(MemoryStream)}, true);

            var il = getRawBytesSegment.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld,
                typeof(MemoryStream).GetField("_buffer", BindingFlags.NonPublic | BindingFlags.Instance));
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld,
                typeof(MemoryStream).GetField("_length", BindingFlags.NonPublic | BindingFlags.Instance));
            il.Emit(OpCodes.Newobj,
                typeof(ArraySegment<byte>).GetConstructor(new[] {typeof(byte[]), typeof(int), typeof(int)}));
            il.Emit(OpCodes.Ret);

            GetAsArraySegment =
                (Func<MemoryStream, ArraySegment<byte>>)
                getRawBytesSegment.CreateDelegate(typeof(Func<MemoryStream, ArraySegment<byte>>));
        }

        public static readonly Func<MemoryStream, ArraySegment<byte>> GetAsArraySegment;

        public static ArraySegment<byte> GetBodyAsArraySegment(Message m)
        {
            return GetAsArraySegment((MemoryStream) m.BodyStream);
        }
    }
}