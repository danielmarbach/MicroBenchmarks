using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace MicroBenchmarks.RabbitMQ;

[SimpleJob]
[MemoryDiagnoser]
public class RoutingKey
{
    [Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(Arguments))]
    public string Before(int delay, string address) => CalculateRoutingKeyNew(10, "some-address", out _);
    
    [Benchmark]
    [ArgumentsSource(nameof(Arguments))]
    public string After(int delay, string address) => CalculateRoutingKeyNewV2(10, "some-address", out _);
    
    public IEnumerable<object[]> Arguments()
    {
        yield return [0, "some-address"];
        yield return [10, "some-address"];
        yield return [MaxDelayInSeconds - 1, "almost-max"];
        yield return [MaxDelayInSeconds, "max"];
    }
    
    public static string CalculateRoutingKeyOld(int delayInSeconds, string address, out int startingDelayLevel)
    {
        if (delayInSeconds < 0)
        {
            delayInSeconds = 0;
        }

        var bitArray = new BitArray(new[] { delayInSeconds });
        var sb = new StringBuilder();
        startingDelayLevel = 0;

        for (var level = MaxLevel; level >= 0; level--)
        {
            if (startingDelayLevel == 0 && bitArray[level])
            {
                startingDelayLevel = level;
            }

            sb.Append(bitArray[level] ? "1." : "0.");
        }

        sb.Append(address);

        return sb.ToString();
    }
    
    public static unsafe string CalculateRoutingKeyNew(int delayInSeconds, string address, out int startingDelayLevel)
    {
        if (delayInSeconds < 0)
        {
            delayInSeconds = 0;
        }

        startingDelayLevel = 0;

        fixed (int* startingDelayLevelPtr = &startingDelayLevel)
        {
            var addr = (IntPtr)startingDelayLevelPtr;

            return string.Create((2 * MaxLevel) + 2 + address.Length, (address, delayInSeconds, addr), Action);

            static void Action(Span<char> span, (string address, int, IntPtr) state)
            {
                var (address, delayInSeconds, startingDelayLevelPtr) = state;

                var startingDelayLevel = 0;
                var mask = BitVector32.CreateMask();

                var bitVector = new BitVector32(delayInSeconds);

                var index = 0;
                for (var level = MaxLevel; level >= 0; level--)
                {
                    var flag = bitVector[mask << level];
                    if (startingDelayLevel == 0 && flag)
                    {
                        startingDelayLevel = level;
                    }

                    span[index++] = flag ? '1' : '0';
                    span[index++] = '.';
                }

                address.AsSpan().CopyTo(span[index..]);

                Unsafe.Write(startingDelayLevelPtr.ToPointer(), startingDelayLevel);
            }
        }
    }

    public static unsafe string CalculateRoutingKeyNewV2(int delayInSeconds, string address, out int startingDelayLevel)
    {
        if (delayInSeconds < 0)
        {
            delayInSeconds = 0;
        }

        startingDelayLevel = 0;

        fixed (int* startingDelayLevelPtr = &startingDelayLevel)
        {
            var addr = (IntPtr)startingDelayLevelPtr;

            return string.Create((2 * MaxLevel) + 2 + address.Length, (address, delayInSeconds, addr), Action);

            static void Action(Span<char> span, (string address, int, IntPtr) state)
            {
                var (address, delayInSeconds, startingDelayLevelPtr) = state;

                var startingDelayLevel = 0;

                var index = 0;
                for (var level = MaxLevel; level >= 0; level--)
                {
                    bool bitSet = ((delayInSeconds >> level) & 1) != 0;
                    if (startingDelayLevel == 0 && bitSet)
                    {
                        startingDelayLevel = level;
                    }

                    span[index++] = bitSet ? '1' : '0';
                    span[index++] = '.';
                }

                address.AsSpan().CopyTo(span[index..]);

                Unsafe.Write(startingDelayLevelPtr.ToPointer(), startingDelayLevel);
            }
        }
    }
    
    const int maxNumberOfBitsToUse = 28;

    public const int MaxLevel = maxNumberOfBitsToUse - 1;
    public const int MaxDelayInSeconds = (1 << maxNumberOfBitsToUse) - 1;
}