using System;
using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.Hashing;

[Config(typeof(Config))]
public class DeterministicGuidImprovements
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(RPlotExporter.Default);
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.Default.WithInvocationCount(960000));
        }
    }

    private string part1;
    private string part2;
    private string part3;

    [Params(1)]
    public int Length { get; set; }

    [IterationSetup]
    public void Setup()
    {
        part1 = string.Join(string.Empty, Enumerable.Range(0, Length).Select(_ => Guid.NewGuid()));
        part2 = string.Join(string.Empty, Enumerable.Range(0, Length).Select(_ => Guid.NewGuid()));
        part3 = string.Join(string.Empty, Enumerable.Range(0, Length).Select(_ => Guid.NewGuid()));
    }

    [Benchmark(Baseline = true)]
    public Guid ComputeAndResize()
    {
        var src = $"{part1}{part2}{part3}";

        var stringBytes = Encoding.UTF8.GetBytes(src);

        using var sha1CryptoServiceProvider = SHA1.Create();
        var hashedBytes = sha1CryptoServiceProvider.ComputeHash(stringBytes);
        Array.Resize(ref hashedBytes, 16);
        return new Guid(hashedBytes);
    }

    [Benchmark]
    public Guid RentAndSpan()
    {
        var src = $"{part1}{part2}{part3}";
        var byteCount = Encoding.UTF8.GetByteCount(src);
        var buffer = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            var numberOfBytesWritten = Encoding.UTF8.GetBytes(src.AsSpan(), buffer);

            using var sha1CryptoServiceProvider = SHA1.Create();
            var guidBytes = sha1CryptoServiceProvider.ComputeHash(buffer, 0, numberOfBytesWritten).AsSpan().Slice(0, 16);
            if (!MemoryMarshal.TryRead<Guid>(guidBytes, out var deterministicGuid))
            {
                deterministicGuid = new Guid(guidBytes.ToArray());
            }
            return deterministicGuid;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    [Benchmark]
    public Guid RentAndSpanTryCompute()
    {
        var src = $"{part1}{part2}{part3}";
        var byteCount = Encoding.UTF8.GetByteCount(src);
        var stringBuffer = ArrayPool<byte>.Shared.Rent(byteCount);
        var hashBuffer = ArrayPool<byte>.Shared.Rent(20);
        var hashBufferReturned = false;
        try
        {
            var numberOfBytesWritten = Encoding.UTF8.GetBytes(src.AsSpan(), stringBuffer);

            using var sha1CryptoServiceProvider = SHA1.Create();
            byte[] hashBufferLocal;
            if (!sha1CryptoServiceProvider.TryComputeHash(stringBuffer.AsSpan().Slice(0, numberOfBytesWritten), hashBuffer, out _))
            {
                ArrayPool<byte>.Shared.Return(hashBuffer, clearArray: false);
                hashBufferReturned = true;
                hashBufferLocal = sha1CryptoServiceProvider.ComputeHash(stringBuffer, 0, numberOfBytesWritten);
            }
            else
            {
                hashBufferLocal = hashBuffer;
            }

            var guidBytes = hashBufferLocal.AsSpan().Slice(0, 16);
            return new Guid(guidBytes);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(stringBuffer, clearArray: true);
            if (!hashBufferReturned)
            {
                ArrayPool<byte>.Shared.Return(hashBuffer, clearArray: false);
            }
        }
    }

    [Benchmark]
    public Guid RentAndSpanTryComputeAndStackalloc()
    {
        var src = $"{part1}{part2}{part3}";
        var byteCount = Encoding.UTF8.GetByteCount(src);
        var stringBuffer = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            var numberOfBytesWritten = Encoding.UTF8.GetBytes(src.AsSpan(), stringBuffer);

            using var sha1CryptoServiceProvider = SHA1.Create();
            Span<byte> hashBuffer = stackalloc byte[20];
            if (!sha1CryptoServiceProvider.TryComputeHash(stringBuffer.AsSpan().Slice(0, numberOfBytesWritten), hashBuffer, out _))
            {
                var hashBufferLocal = sha1CryptoServiceProvider.ComputeHash(stringBuffer, 0, numberOfBytesWritten);
                hashBufferLocal.CopyTo(hashBuffer);
            }

            var guidBytes = hashBuffer.Slice(0, 16);
            return new Guid(guidBytes);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(stringBuffer, clearArray: true);
        }
    }

    [Benchmark]
    [SkipLocalsInit]
    public Guid RentAndSpanTryComputeAndStackallocWithStaticAndNoStringAlloc()
    {
        var length = part1.Length + part2.Length +
                     part3.Length + 2; // two separators

        const int MaxStackLimit = 256;
        const byte SeperatorByte = 95;
        var encoding = Encoding.UTF8;
        var maxByteCount = encoding.GetMaxByteCount(length);

        byte[]? sharedBuffer = null;
        var stringBufferSpan = maxByteCount <= MaxStackLimit ?
            stackalloc byte[MaxStackLimit] :
            sharedBuffer = ArrayPool<byte>.Shared.Rent(maxByteCount);

        try
        {
            var numberOfBytesWritten = encoding.GetBytes(part1.AsSpan(), stringBufferSpan);
            stringBufferSpan[numberOfBytesWritten++] = SeperatorByte;

            numberOfBytesWritten += encoding.GetBytes(part2.AsSpan(), stringBufferSpan[numberOfBytesWritten..]);
            stringBufferSpan[numberOfBytesWritten++] = SeperatorByte;

            numberOfBytesWritten += encoding.GetBytes(part3.AsSpan(), stringBufferSpan[numberOfBytesWritten..]);

            Span<byte> hashBuffer = stackalloc byte[20];
            _ = SHA1.HashData(stringBufferSpan[..numberOfBytesWritten], hashBuffer);
            var guidBytes = hashBuffer[..16];
            return new Guid(guidBytes);
        }
        finally
        {
            if (sharedBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(sharedBuffer, clearArray: true);
            }
        }
    }
}