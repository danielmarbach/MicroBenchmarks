using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;
using Microsoft.Azure.Amqp.Framing;

namespace MicroBenchmarks.ServiceBus
{
    using System;
    using System.IO;
    using System.Text;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Exporters;
    using BenchmarkDotNet.Jobs;
    using Microsoft.Azure.Amqp;

    [Config(typeof(Config))]
    public class ArrayCopyVsArrayWriter
    {
        private Data[] data;
        private AmqpMessage amqpMessage;
        private Consumer consumer;

        private class Config : ManualConfig
        {
            public Config()
            {
                AddExporter(MarkdownExporter.GitHub);
                AddDiagnoser(MemoryDiagnoser.Default);
                AddJob(Job.Default.WithInvocationCount(800000));
            }
        }

        [IterationSetup]
        public void Setup()
        {
            data = new[]
            {
                new Data {Value = new ArraySegment<byte>(Encoding.UTF8.GetBytes("Hello"))},
                new Data {Value = new ArraySegment<byte>(Encoding.UTF8.GetBytes("World"))}
            };
            amqpMessage = AmqpMessage.Create(data);

            consumer = new Consumer();
        }

        [IterationCleanup]
        public void Cleanup()
        {
            amqpMessage.Dispose();
        }

        [Benchmark(Baseline = true)]
        public void ArrayCopy()
        {
            GetDataViaDataBody(amqpMessage).Consume(consumer);
        }

        [Benchmark]
        public void BySegment()
        {
            GetDataViaDataBodyArrayWriter(amqpMessage).Consume(consumer);
        }

        private static ReadOnlyMemory<byte> GetByteArrayCopy(Data data)
        {
            switch (data.Value)
            {
                case byte[] byteArray:
                    return byteArray;
                case ArraySegment<byte> arraySegment:
                    var bytes = new byte[arraySegment.Count];
                    Array.ConstrainedCopy(
                        sourceArray: arraySegment.Array,
                        sourceIndex: arraySegment.Offset,
                        destinationArray: bytes,
                        destinationIndex: 0,
                        length: arraySegment.Count);
                    return bytes;
                default:
                    return null;
            }
        }

        public static IList<ReadOnlyMemory<byte>> GetDataViaDataBody(AmqpMessage message)
        {
            IList<ReadOnlyMemory<byte>> dataList = new List<ReadOnlyMemory<byte>>();
            foreach (Data data in (message.DataBody ?? Enumerable.Empty<Data>()))
            {
                dataList.Add(BinaryData.FromBytes(GetByteArrayCopy(data)));
            }
            return dataList;
        }

        private static ReadOnlyMemory<byte> GetByteArrayBySegment(Data data)
        {
            return data.Value switch
            {
                byte[] byteArray => byteArray,
                ArraySegment<byte> arraySegment => arraySegment,
                _ => null
            };
        }

        private static IEnumerable<ReadOnlyMemory<byte>> GetDataViaDataBodyArrayWriter(AmqpMessage message)
        {
            List<ReadOnlyMemory<byte>> dataList = null;
            foreach (Data data in message.DataBody ?? Enumerable.Empty<Data>())
            {
                dataList ??= new List<ReadOnlyMemory<byte>>();
                dataList.Add(GetByteArrayBySegment(data));
            }
            return dataList ?? Enumerable.Empty<ReadOnlyMemory<byte>>();
        }
    }
}