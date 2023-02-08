using BenchmarkDotNet.Attributes;

namespace MicroBenchmarks.ServiceBus;

using System;
using System.IO;
using System.Text;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using Microsoft.Azure.Amqp;

[Config(typeof(Config))]
public class BufferListStreamDirectly
{
    private BufferListStream stream;
    private AmqpMessage amqpMessage;

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
        stream = new BufferListStream(new[] {new ArraySegment<byte>(Encoding.UTF8.GetBytes("Hello"))});
        amqpMessage = AmqpMessage.Create(stream, false);
    }

    [IterationCleanup]
    public void Cleanup()
    {
        amqpMessage.Dispose();
    }

    private const int StreamBufferSizeInBytes = 512;

    [Benchmark(Baseline = true)]
    public ArraySegment<byte> ConvertViaMemoryStream()
    {
        using var messageStream = amqpMessage.ToStream();
        using var memStream = new MemoryStream(StreamBufferSizeInBytes);
        messageStream.CopyTo(memStream, StreamBufferSizeInBytes);
        return new ArraySegment<byte>(memStream.ToArray());
    }

    [Benchmark]
    public ArraySegment<byte> PatternMatch()
    {
        using var messageStream = amqpMessage.ToStream();
        return messageStream switch
        {
            BufferListStream bufferListStream => bufferListStream.ReadBytes((int) stream.Length),
            _ => throw new InvalidOperationException()
        };
    }
}