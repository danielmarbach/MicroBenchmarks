using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace MicroBenchmarks.NServiceBus;

[MemoryDiagnoser]
public class DeterministicGuidBenchmarks
{
    public IEnumerable<object> Inputs()
    {
        yield return new object[] { "Instance", "Host" };
        yield return new object[] { "InstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstance", "InstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstanceInstance" };
    }
    
    [Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(Inputs))]
    public Guid Original(string input1, string input2) => DeterministicGuid_Original.Create(input1, input2);

    [Benchmark]
    [ArgumentsSource(nameof(Inputs))]
    public Guid V2(string input1, string input2) => DeterministicGuidV2.Create(input1, input2);

    [Benchmark]
    [ArgumentsSource(nameof(Inputs))]
    public Guid V3(string input1, string input2) => DeterministicGuidV3.Create(input1, input2);
    static class DeterministicGuid_Original
    {
        public static Guid Create(params object[] data)
        {
            // use MD5 hash to get a 16-byte hash of the string
            using (var provider = MD5.Create())
            {
                var inputBytes = Encoding.Default.GetBytes(string.Concat(data));
                var hashBytes = provider.ComputeHash(inputBytes);
                // generate a guid from the hash:
                return new Guid(hashBytes);
            }
        }
    }
  
    static class DeterministicGuidV2
    {
        public static Guid Create(string data1, string data2) => Create($"{data1}{data2}");

        public static Guid Create(string data)
        {
            // use MD5 hash to get a 16-byte hash of the string
            var inputBytes = Encoding.Default.GetBytes(data);

            Span<byte> hashBytes = stackalloc byte[16];

            _ = MD5.HashData(inputBytes, hashBytes);

            // generate a guid from the hash:
            return new Guid(hashBytes);
        }
    }
    
    static class DeterministicGuidV3
    {
        public static Guid Create(string data1, string data2) => Create($"{data1}{data2}");

        [SkipLocalsInit]
        public static Guid Create(string data)
        {
            const int MaxStackLimit = 256;
            var encoding = Encoding.UTF8;
            var maxByteCount = encoding.GetMaxByteCount(data.Length);

            byte[]? sharedBuffer = null;
            var stringBufferSpan = maxByteCount <= MaxStackLimit ?
                stackalloc byte[MaxStackLimit] :
                sharedBuffer = ArrayPool<byte>.Shared.Rent(maxByteCount);

            try
            {
                var numberOfBytesWritten = encoding.GetBytes(data, stringBufferSpan);
                Span<byte> hashBytes = stackalloc byte[16];

                _ = MD5.HashData(stringBufferSpan[..numberOfBytesWritten], hashBytes);
                
                // generate a guid from the hash:
                return new Guid(hashBytes);
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
}