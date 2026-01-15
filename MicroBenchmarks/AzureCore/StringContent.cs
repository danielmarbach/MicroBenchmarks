using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MicroBenchmarks.AzureCore;

using System;
using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

[Config(typeof(Config))]
public class ReadonlySequence
{
    private ReadOnlySequence<byte> readOnlySequence;

    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.Default);
        }
    }

    [Params(5, 10, 20)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var arrayOne = Enumerable.Range(0, Size).Select(i => (byte)i).ToArray();
        var arrayTwo = Enumerable.Range(Size+1, 2*Size).Select(i => (byte)i).ToArray();
        var arrayThree = Enumerable.Range(2*Size+1, 3*Size).Select(i => (byte)i).ToArray();

        var first = new MemorySegment<byte>(arrayOne);
        var last = first.Append(arrayTwo).Append(arrayThree);

        readOnlySequence = new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    [Benchmark(Baseline = true)]
    public Stream Before()
    {
        var stream = new MemoryStream();
        using var content = new ReadOnlySequenceContent(readOnlySequence);
        content.WriteTo(stream, CancellationToken.None);
        stream.Flush();
        return stream;
    }

    [Benchmark]
    public Stream After()
    {
        var stream = new MemoryStream();
        using var content = new ReadOnlySequenceContentAfter(readOnlySequence);
        content.WriteTo(stream, CancellationToken.None);
        stream.Flush();
        return stream;
    }
}

internal class MemorySegment<T> : ReadOnlySequenceSegment<T>
{
    public MemorySegment(ReadOnlyMemory<T> memory)
    {
        Memory = memory;
    }

    public MemorySegment<T> Append(ReadOnlyMemory<T> memory)
    {
        var segment = new MemorySegment<T>(memory)
        {
            RunningIndex = RunningIndex + Memory.Length
        };

        Next = segment;

        return segment;
    }
}

sealed class ReadOnlySequenceContent : RequestContent
{
    private readonly ReadOnlySequence<byte> _bytes;

    public ReadOnlySequenceContent(ReadOnlySequence<byte> bytes)
        => _bytes = bytes;

    public override void Dispose() { }

    public override void WriteTo(Stream stream, CancellationToken cancellation)
    {
        byte[] buffer = _bytes.ToArray();
        stream.Write(buffer, 0, buffer.Length);
    }

    public override bool TryComputeLength(out long length)
    {
        length = _bytes.Length;
        return true;
    }

    public override Task WriteToAsync(Stream stream, CancellationToken cancellation)
    {
        return Task.CompletedTask;
    }
}

sealed class ReadOnlySequenceContentAfter : RequestContent
{
    private readonly ReadOnlySequence<byte> _bytes;

    public ReadOnlySequenceContentAfter(ReadOnlySequence<byte> bytes)
        => _bytes = bytes;

    public override void Dispose() { }

    public override void WriteTo(Stream stream, CancellationToken cancellation)
    {
        foreach (var memory in _bytes)
        {
            stream.Write(memory.Span);
        }
    }

    public override bool TryComputeLength(out long length)
    {
        length = _bytes.Length;
        return true;
    }

    public override Task WriteToAsync(Stream stream, CancellationToken cancellation)
    {
        return Task.CompletedTask;
    }
}

[Config(typeof(Config))]
public class StringContent
{
    private string inputString;

    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.Default);
        }
    }

    [Params(8, 12, 24, 32, 64, 128, 255, 512)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        inputString = new string('a', Size);
    }

    [Benchmark(Baseline = true)]
    public Stream Before()
    {
        var stream = new MemoryStream();
        using var content = new ArrayContent(RequestContent.s_UTF8NoBomEncoding.GetBytes(inputString), 0, inputString.Length);
        content.WriteTo(stream, CancellationToken.None);
        stream.Flush();
        return stream;
    }

    [Benchmark]
    public Stream After()
    {
        var stream = new MemoryStream();
        using var content = new StringContentAfter(inputString, RequestContent.s_UTF8NoBomEncoding);
        content.WriteTo(stream, CancellationToken.None);
        stream.Flush();
        return stream;
    }
}

public abstract class RequestContent : IDisposable
{

    public static readonly Encoding s_UTF8NoBomEncoding = new UTF8Encoding(false);

    public abstract Task WriteToAsync(Stream stream, CancellationToken cancellation);

    public abstract void WriteTo(Stream stream, CancellationToken cancellation);

    public abstract bool TryComputeLength(out long length);

    public abstract void Dispose();
}

sealed class ArrayContent : RequestContent
{
    private readonly byte[] _bytes;

    private readonly int _contentStart;

    private readonly int _contentLength;

    public ArrayContent(byte[] bytes, int index, int length)
    {
        _bytes = bytes;
        _contentStart = index;
        _contentLength = length;
    }

    public override void Dispose() { }

    public override void WriteTo(Stream stream, CancellationToken cancellation)
    {
        stream.Write(_bytes, _contentStart, _contentLength);
    }

    public override bool TryComputeLength(out long length)
    {
        length = _contentLength;
        return true;
    }

    public override async Task WriteToAsync(Stream stream, CancellationToken cancellation)
    {
#pragma warning disable CA1835 // WriteAsync(Memory<>) overload is not available in all targets
        await stream.WriteAsync(_bytes, _contentStart, _contentLength, cancellation).ConfigureAwait(false);
#pragma warning restore // WriteAsync(Memory<>) overload is not available in all targets
    }
}


sealed class StringContentAfter : RequestContent
{
    private readonly byte[] _buffer;
    private readonly int _actualByteCount;

    public StringContentAfter(string value, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var byteCount = encoding.GetMaxByteCount(value.Length);
        _buffer = ArrayPool<byte>.Shared.Rent(byteCount);
        _actualByteCount = encoding.GetBytes(value, _buffer);
    }

    public override async Task WriteToAsync(Stream stream, CancellationToken cancellation)
    {
        await stream.WriteAsync(_buffer.AsMemory(0, _actualByteCount), cancellation).ConfigureAwait(false);
    }

    public override void WriteTo(Stream stream, CancellationToken cancellation)
    {
        stream.Write(_buffer.AsSpan(0, _actualByteCount));
    }

    public override bool TryComputeLength(out long length)
    {
        length = _actualByteCount;
        return true;
    }

    public override void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_buffer, clearArray: true);
    }
}