using System;
using System.Buffers.Binary;
#if NETFRAMEWORK
using System.Runtime.InteropServices;
#endif
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.Hashing;

[Config(typeof(Config))]
public class CombGuidImprovements
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(RPlotExporter.Default);
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.Default.WithRuntime(ClrRuntime.Net48).WithInvocationCount(1920000));
            AddJob(Job.Default.WithRuntime(CoreRuntime.Core60).WithInvocationCount(1920000));
        }
    }

    [Benchmark(Baseline = true)]
    public Guid Before()
    {
        return GenerateOld();
    }

    [Benchmark]
    public Guid After()
    {
        return GenerateImproved();
    }

    private static Guid GenerateOld()
    {
        var guidArray = Guid.NewGuid().ToByteArray();

        var now = DateTime.UtcNow; // Internal use, no need for DateTimeOffset

        // Get the days and milliseconds which will be used to build the byte string
        var days = new TimeSpan(now.Ticks - BaseDateTicks);
        var timeOfDay = now.TimeOfDay;

        // Convert to a byte array
        // Note that SQL Server is accurate to 1/300th of a millisecond so we divide by 3.333333
        var daysArray = BitConverter.GetBytes(days.Days);
        var millisecondArray = BitConverter.GetBytes((long)(timeOfDay.TotalMilliseconds / 3.333333));

        // Reverse the bytes to match SQL Servers ordering
        Array.Reverse(daysArray);
        Array.Reverse(millisecondArray);

        // Copy the bytes into the guid
        Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
        Array.Copy(millisecondArray, millisecondArray.Length - 4, guidArray, guidArray.Length - 4, 4);

        return new Guid(guidArray);
    }

    public static Guid GenerateImproved() => GenerateImproved(Guid.NewGuid(), DateTime.UtcNow);

    public static Guid GenerateImproved(Guid guid, DateTime nowInput)
    {
        var newGuid = guid;
        Span<byte> guidArray = stackalloc byte[16];
#if NETCOREAPP
        if (!newGuid.TryWriteBytes(guidArray))
#elif NETFRAMEWORK
                if (!MemoryMarshal.TryWrite(guidArray, ref newGuid))
#endif
        {

            guidArray = newGuid.ToByteArray();
        }

        var now = nowInput; // Internal use, no need for DateTimeOffset

        // Get the days and milliseconds which will be used to build the byte string
        var days = new TimeSpan(now.Ticks - BaseDateTicksConst);
        var timeOfDay = now.TimeOfDay;

        // Convert to a byte array
        Span<byte> scratchArray = stackalloc byte[sizeof(long)] { 0, 0, 0, 0, 0, 0, 0, 0};
        // Reverse the bytes to match SQL Servers ordering
        if (BitConverter.IsLittleEndian)
        {
            BinaryPrimitives.WriteInt32BigEndian(scratchArray, days.Days);
        }
        else
        {
            BinaryPrimitives.WriteInt32LittleEndian(scratchArray, days.Days);
        }
        // Copy the bytes into the guid
        scratchArray.Slice(2, 2).CopyTo(guidArray.Slice(10, 2));

        // Reverse the bytes to match SQL Servers ordering
        // Note that SQL Server is accurate to 1/300th of a millisecond so we divide by 3.333333
        if (BitConverter.IsLittleEndian)
        {
            BinaryPrimitives.WriteInt64BigEndian(scratchArray, (long)(timeOfDay.TotalMilliseconds / 3.333333));
        }
        else
        {
            BinaryPrimitives.WriteInt64LittleEndian(scratchArray, (long)(timeOfDay.TotalMilliseconds / 3.333333));
        }
        // Copy the bytes into the guid
        scratchArray.Slice(3, 4).CopyTo(guidArray.Slice(12, 4));

#if NETCOREAPP
        return new Guid(guidArray);
#elif NETFRAMEWORK
                if (!MemoryMarshal.TryRead(guidArray, out Guid readGuid))
                {
                    readGuid = new Guid(guidArray.ToArray());
                }

                return readGuid;
#endif
    }

    static readonly long BaseDateTicks = new DateTime(1900, 1, 1).Ticks;

    private const long BaseDateTicksConst = 599266080000000000;
}