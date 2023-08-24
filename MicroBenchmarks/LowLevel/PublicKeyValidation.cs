using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;

namespace MicroBenchmarks.LowLevel;

[SimpleJob]
[MemoryDiagnoser]
public class PublicKeyValidation
{
    private byte[] PublicKeyToken;

    [GlobalSetup]
    public void Setup()
    {
        PublicKeyToken = typeof(string).Assembly.GetName().GetPublicKeyToken()!;
    }

    [Benchmark(Baseline = true)]
    public bool Before()
    {
        return IsRuntimeAssembly(GetPublicKeyToken(PublicKeyToken));
    }

    [Benchmark]
    public bool After()
    {
        return IsRuntimeAssemblyAfter(GetPublicKeyTokenAfter(PublicKeyToken));
    }
    
    static byte[] GetPublicKeyToken(byte[] publicKey)
    {
        using (var sha1 = SHA1.Create())
        {
            var hash = sha1.ComputeHash(publicKey);
            var publicKeyToken = new byte[8];

            for (var i = 0; i < 8; i++)
            {
                publicKeyToken[i] = hash[hash.Length - (i + 1)];
            }

            return publicKeyToken;
        }
    }

    public static bool IsRuntimeAssembly(byte[] publicKeyToken)
    {
        var tokenString = BitConverter.ToString(publicKeyToken).Replace("-", string.Empty).ToLowerInvariant();

        return tokenString switch
        {
            // Microsoft tokens
            "b77a5c561934e089" or "7cec85d7bea7798e" or "b03f5f7f11d50a3a" or "31bf3856ad364e35" or "cc7b13ffcd2ddd51" or "adb9793829ddae60" or "7e34167dcc6d6d8c" or "23ec7fc2d6eaa4a5" => true,
            _ => false,
        };
    }
    
    static string GetPublicKeyTokenAfter(byte[] publicKey)
    {
        using var sha1 = SHA1.Create();
        Span<byte> publicKeyToken = stackalloc byte[20];
        // returns false when the destination doesn't have enough space
        _ = sha1.TryComputeHash(publicKey, publicKeyToken, out _);
        Span<byte> lastEightBytes = publicKeyToken.Slice(publicKeyToken.Length - 8, 8);
        lastEightBytes.Reverse();
        return Convert.ToHexString(lastEightBytes);
    }

    public static bool IsRuntimeAssemblyAfter(string tokenString) =>
        tokenString switch
        {
            // Microsoft tokens
            "B77A5C561934E089" or "7CEC85D7BEA7798E" or "B03F5F7F11D50A3A" or "31BF3856AD364E35" or "CC7B13FFCD2DDD51" or "ADB9793829DDAE60" or "7E34167DCC6D6D8C" or "23EC7FC2D6EAA4A5" => true,
            _ => false,
        };
}