using System.Buffers;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using Newtonsoft.Json;

namespace MicroBenchmarks.Json;

[Config(typeof(Config))]
public class JsonPooling
{
    private SomeObjectToSerialize objectToSerialize;
    private JsonSerializer serializer;

    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
        }
    }
    
    [Params(1, 128, 256, 512, 1024, 4096, 8192)]
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
    }

    class SomeObjectToSerialize
    {
        public string Foo { get; set; }
        public string Bar { get; set; }
    }

    [Benchmark(Baseline = true)]
    public MemoryStream Encoding()
    {
        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(objectToSerialize)));
    }

    [Benchmark]
    public MemoryStream Pooling()
    {
        var stream = new MemoryStream();
        using var streamWriter = new StreamWriter(stream, default, bufferSize: -1, leaveOpen: true);
        using var writer = new JsonTextWriter(streamWriter);
        writer.ArrayPool = JsonArrayPool.Instance;
        serializer.Serialize(writer, objectToSerialize);
        return stream;
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