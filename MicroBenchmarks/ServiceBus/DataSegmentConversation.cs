using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;

namespace MicroBenchmarks.ServiceBus;

[Config(typeof(Config))]
public class DataSegmentConversation
{
    private AmqpMessage amqpMessage;
    private Consumer consumer;
    private Data[] data;

    [Params(0, 1, 2, 4, 8, 16)] public int NumberOfSegments { get; set; }

    [IterationSetup]
    public void Setup()
    {
        data = Enumerable.Range(0, NumberOfSegments)
            .Select(i => new Data { Value = new ArraySegment<byte>(Encoding.UTF8.GetBytes($"Hello World{i}, Hello World{i}, Hello World{i}, Hello World{i}")) })
            .ToArray();
        amqpMessage = AmqpMessage.Create(data);

        consumer = new Consumer();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        amqpMessage.Dispose();
    }

    [Benchmark(Baseline = true)]
    public ReadOnlyMemory<byte> Before()
    {
        var copier = new EagerCopyingMessageBodyBefore(data);
        return copier.WrittenMemory;
    }

    [Benchmark]
    public ReadOnlyMemory<byte> After()
    {
        var copier = new EagerCopyingMessageBodyAfter(data);
        return copier.WrittenMemory;
    }

    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.Default.WithInvocationCount(800000));
        }
    }

    private sealed class EagerCopyingMessageBodyBefore
    {
        private IList<ReadOnlyMemory<byte>> _segments;
        private ArrayBufferWriter<byte> _writer;

        internal EagerCopyingMessageBodyBefore(IEnumerable<Data> dataSegments)
        {
            foreach (var segment in dataSegments) Append(segment);
        }

        public ReadOnlyMemory<byte> WrittenMemory => _writer?.WrittenMemory ?? ReadOnlyMemory<byte>.Empty;

        public IEnumerator<ReadOnlyMemory<byte>> GetEnumerator()
        {
            return _segments.GetEnumerator();
        }

        private void Append(Data segment)
        {
            // fields are lazy initialized to not occupy unnecessary memory when there are no data segments
            _writer ??= new ArrayBufferWriter<byte>();
            _segments ??= new List<ReadOnlyMemory<byte>>();

            var dataToAppend = segment.Value switch
            {
                byte[] byteArray => byteArray,
                ArraySegment<byte> arraySegment => arraySegment,
                _ => ReadOnlyMemory<byte>.Empty
            };

            var memory = _writer.GetMemory(dataToAppend.Length);
            dataToAppend.CopyTo(memory);
            _writer.Advance(dataToAppend.Length);
            _segments.Add(memory.Slice(0, dataToAppend.Length));
        }
    }

    private sealed class EagerCopyingMessageBodyAfter
    {
        private IList<ReadOnlyMemory<byte>> _segments;
        private ArrayBufferWriter<byte> _writer;

        internal EagerCopyingMessageBodyAfter(IEnumerable<Data> dataSegments)
        {
            Append(dataSegments);
        }

        public ReadOnlyMemory<byte> WrittenMemory => _writer?.WrittenMemory ?? ReadOnlyMemory<byte>.Empty;

        public IEnumerator<ReadOnlyMemory<byte>> GetEnumerator()
        {
            return _segments.GetEnumerator();
        }

        private void Append(IEnumerable<Data> dataSegments)
        {
            var length = 0;
            var numberOfSegments = 0;
            List<ReadOnlyMemory<byte>> segments = null;
            foreach (var segment in dataSegments)
            {
                segments ??= dataSegments is IReadOnlyCollection<Data> readOnlyList
                    ? new List<ReadOnlyMemory<byte>>(readOnlyList.Count)
                    : new List<ReadOnlyMemory<byte>>();
                var dataToAppend = segment.Value switch
                {
                    byte[] byteArray => byteArray,
                    ArraySegment<byte> arraySegment => arraySegment,
                    _ => ReadOnlyMemory<byte>.Empty
                };
                length += dataToAppend.Length;
                numberOfSegments++;
                segments.Add(dataToAppend);
            }

            if (segments == null) return;

            // fields are lazy initialized to not occupy unnecessary memory when there are no data segments
            _writer = length > 0 ? new ArrayBufferWriter<byte>(length) : new ArrayBufferWriter<byte>();
            _segments = segments;

            for (var i = 0; i < numberOfSegments; i++)
            {
                var dataToAppend = segments[i];
                var memory = _writer.GetMemory(dataToAppend.Length);
                dataToAppend.CopyTo(memory);
                _writer.Advance(dataToAppend.Length);
                segments[i] = memory.Slice(0, dataToAppend.Length);
            }
        }
    }
}