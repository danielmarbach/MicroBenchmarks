using System.Buffers;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using Microsoft.IO;
using Newtonsoft.Json;

namespace MicroBenchmarks.Json;

[Config(typeof(Config))]
public class JsonPooling
{
    private SomeObjectToSerialize objectToSerialize;
    private JsonSerializer serializer;
    private static RecyclableMemoryStreamManager manager = new RecyclableMemoryStreamManager();
    private Consumer consumer;

    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.ShortRun);
        }
    }
    
    [Params(64, 128, 256)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        objectToSerialize = new SomeObjectToSerialize
        {
            Foo = new string('f', Size),
            Bar = new string('b', Size)
        };
        serializer = JsonSerializer.CreateDefault();
        consumer = new Consumer();
    }

    class SomeObjectToSerialize
    {
        public string Foo { get; set; }
        public string Bar { get; set; }
    }

    [Benchmark(Baseline = true)]
    public void Encoding()
    {
        var buffer = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(objectToSerialize));
        using var stream = new MemoryStream(buffer);
        consumer.Consume(stream);
    }

    [Benchmark]
    public void EncodingWithPooling()
    {
        var buffer = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(objectToSerialize));
        using var stream = manager.GetStream();
        stream.Write(buffer);
        consumer.Consume(stream);
    }

    [Benchmark]
    public void StreamPooling()
    {
        using var stream = manager.GetStream();
        using var streamWriter = new StreamWriter(stream, default, bufferSize: 1024, leaveOpen: true);
        using var writer = new JsonTextWriter(streamWriter);
        serializer.Serialize(writer, objectToSerialize);
        consumer.Consume(stream);
    }

    [Benchmark]
    public void JsonCharPooling()
    {
        using var stream = new MemoryStream();
        using var streamWriter = new StreamWriter(stream, default, bufferSize: 1024, leaveOpen: true);
        using var writer = new JsonTextWriter(streamWriter);
        writer.ArrayPool = JsonArrayPool.Instance;
        serializer.Serialize(writer, objectToSerialize);
        consumer.Consume(stream);
    }

    [Benchmark]
    public void StreamAndJsonCharPooling()
    {
        using var stream = manager.GetStream();
        using var streamWriter = new StreamWriter(stream, default, bufferSize: 1024, leaveOpen: true);
        using var writer = new JsonTextWriter(streamWriter);
        writer.ArrayPool = JsonArrayPool.Instance;
        serializer.Serialize(writer, objectToSerialize);
        consumer.Consume(stream);
    }
    
    class JsonArrayPool : IArrayPool<char>
    {
        public static readonly JsonArrayPool Instance = new JsonArrayPool();

        public char[] Rent(int minimumLength) =>
            ArrayPool<char>.Shared.Rent(minimumLength);

        public void Return(char[] array) =>
            ArrayPool<char>.Shared.Return(array);
    }
}