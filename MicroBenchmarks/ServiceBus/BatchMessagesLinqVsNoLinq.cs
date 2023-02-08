using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.ServiceBus;

[Config(typeof(Config))]
public class BatchMessagesLinqVsNoLinq
{
    private IEnumerable<object> data;
    private Consumer consumer;

    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.Default.WithInvocationCount(640000));
        }
    }

    [Params(0, 2, 4, 8, 16)]
    public int Elements { get; set; }

    [IterationSetup]
    public void Setup()
    {
        consumer = new Consumer();
        data = Enumerable.Range(0, Elements)
            .Select(i => new object());
    }

    [Benchmark(Baseline = true)]
    public object Linq()
    {
        return BuildAmqpBatchFromMessageLinq(data, true);
    }

    object BuildAmqpBatchFromMessageLinq(IEnumerable<object> source, bool forceBatch)
    {
        IDisposable firstAmqpMessage = null;
        object firstMessage = null;

        return BuildAmqpBatchFromMessagesLinq(
            source.Select(sbMessage =>
            {
                if (firstAmqpMessage == null)
                {
                    firstAmqpMessage = new SomeDisposable();
                    firstMessage = sbMessage;
                    return firstAmqpMessage;
                }
                else
                {
                    return new SomeDisposable();
                }
            }).ToList(), firstMessage, forceBatch);
    }

    object BuildAmqpBatchFromMessagesLinq(
        IList<IDisposable> batchMessages,
        object firstMessage,
        bool forceBatch)
    {
        object batchEnvelope;

        if (batchMessages.Count == 1 && !forceBatch)
        {
            batchEnvelope = batchMessages[0];
        }
        else
        {
            batchEnvelope = batchMessages.Select(m =>
            {
                using (m)
                {
                    consumer.Consume<object>(m);
                    return new object();
                }
            }).ToArray();
        }

        return batchEnvelope;
    }

    class SomeDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    [Benchmark]
    public object NoLinq()
    {
        return BuildAmqpBatchFromMessageNoLinq(data, true);
    }

    object BuildAmqpBatchFromMessageNoLinq(IEnumerable<object> source, bool forceBatch)
    {
        IDisposable firstAmqpMessage = null;
        object firstMessage = null;
        List<IDisposable> amqpMessagesForBatching = new List<IDisposable>();
        try
        {
            foreach (var sbMessage in source)
            {
                if (firstAmqpMessage == null)
                {
                    firstAmqpMessage = new SomeDisposable();
                    firstMessage = sbMessage;
                    amqpMessagesForBatching.Add(firstAmqpMessage);
                }
                else
                {
                    amqpMessagesForBatching.Add(new SomeDisposable());
                }
            }

            var amqpMessageBatch = BuildAmqpBatchFromMessagesNoLinq(amqpMessagesForBatching, firstMessage, forceBatch);
            return amqpMessageBatch;
        }
        finally
        {
            foreach (var disposable in amqpMessagesForBatching)
            {
                disposable.Dispose();
            }
        }
    }

    object BuildAmqpBatchFromMessagesNoLinq(
        IReadOnlyList<IDisposable> batchMessages,
        object firstMessage,
        bool forceBatch)
    {
        object batchEnvelope;

        if (batchMessages.Count == 1 && !forceBatch)
        {
            batchEnvelope = batchMessages[0];
        }
        else
        {
            var batched = new List<object>(batchMessages.Count);
            foreach (var message in batchMessages)
            {
                consumer.Consume(message);
                batched.Add(new object());
            }
            batchEnvelope = batched;
        }

        return batchEnvelope;
    }
}