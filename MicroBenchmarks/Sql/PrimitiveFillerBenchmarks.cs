using BenchmarkDotNet.Attributes;

namespace MicroBenchmarks.Sql;

using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;

[Config(typeof(Config))]
public class PrimitiveFillerBenchmarks
{
    private Consumer consumer;
    private DataRecord record;

    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddColumn(StatisticColumn.AllStatistics);
        }
    }

    [GlobalSetup]
    public void Setup()
    {
        consumer = new Consumer();
        record = new DataRecord();

        // warmup for static constructor
        consumer.Consume(PrimitiveFillerBefore.GetReaderValue<int>(record, 0));
        // to be fair
        consumer.Consume(PrimiteTypeFillerAfter.GetReaderValue<int>(record, 0));
    }

    [Benchmark(Baseline = true)]
    public void Before()
    {
        consumer.Consume(PrimitiveFillerBefore.GetReaderValue<int>(record, 0));
        consumer.Consume(PrimitiveFillerBefore.GetReaderValue<char>(record, 0));
        consumer.Consume<decimal>(PrimitiveFillerBefore.GetReaderValue<decimal>(record, 0));
        consumer.Consume<DateTime>(PrimitiveFillerBefore.GetReaderValue<DateTime>(record, 0));
        consumer.Consume(PrimitiveFillerBefore.GetReaderValue<bool>(record, 0));
        consumer.Consume(PrimitiveFillerBefore.GetReaderValue<int>(record, 0));
        consumer.Consume(PrimitiveFillerBefore.GetReaderValue<long>(record, 0));
        consumer.Consume(PrimitiveFillerBefore.GetReaderValue<byte>(record, 0));
    }

    [Benchmark()]
    public void After()
    {
        consumer.Consume(PrimiteTypeFillerAfter.GetReaderValue<int>(record, 0));
        consumer.Consume(PrimiteTypeFillerAfter.GetReaderValue<char>(record, 0));
        consumer.Consume<decimal>(PrimiteTypeFillerAfter.GetReaderValue<decimal>(record, 0));
        consumer.Consume<DateTime>(PrimiteTypeFillerAfter.GetReaderValue<DateTime>(record, 0));
        consumer.Consume(PrimiteTypeFillerAfter.GetReaderValue<bool>(record, 0));
        consumer.Consume(PrimiteTypeFillerAfter.GetReaderValue<int>(record, 0));
        consumer.Consume(PrimiteTypeFillerAfter.GetReaderValue<long>(record, 0));
        consumer.Consume(PrimiteTypeFillerAfter.GetReaderValue<byte>(record, 0));
    }
}

static class PrimiteTypeFillerAfter
{
    public static TType GetReaderValue<TType>(IDataRecord reader, int index)
    {
        var type = typeof(TType);

        switch (type)
        {
            case { } when type == typeof(int):
                var intValue = GetInt32(reader, index);
                return Unsafe.As<int, TType>(ref intValue);
            case { } when type == typeof(long):
                var longValue = GetInt64(reader, index);
                return Unsafe.As<long, TType>(ref longValue);
            case { } when type == typeof(char):
                var charValue = GetChar(reader, index);
                return Unsafe.As<char, TType>(ref charValue);
            case { } when type == typeof(double):
                var doubleValue = GetDouble(reader, index);
                return Unsafe.As<double, TType>(ref doubleValue);
            case { } when type == typeof(decimal):
                var decimalValue = GetDecimal(reader, index);
                return Unsafe.As<decimal, TType>(ref decimalValue);
            case { } when type == typeof(DateTime):
                var dateTimeValue = GetDateTime(reader, index);
                return Unsafe.As<DateTime, TType>(ref dateTimeValue);
            case { } when type == typeof(bool):
                var booleanValue = GetBoolean(reader, index);
                return Unsafe.As<bool, TType>(ref booleanValue);
            case { } when type == typeof(byte):
                var byteValue = GetByte(reader, index);
                return Unsafe.As<byte, TType>(ref byteValue);
            default:
                throw new InvalidOperationException();
        }
    }

    static decimal GetDecimal(IDataRecord record, int index)
    {
        return record.GetDecimal(index);
    }

    static bool GetBoolean(IDataRecord record, int index)
    {
        return record.GetBoolean(index);
    }

    static char GetChar(IDataRecord record, int index)
    {
        return record.GetChar(index);
    }

    static DateTime GetDateTime(IDataRecord record, int index)
    {
        return record.GetDateTime(index);
    }

    static double GetDouble(IDataRecord record, int index)
    {
        return record.GetDouble(index);
    }

    static byte GetByte(IDataRecord record, int index)
    {
        return record.GetByte(index);
    }

    static int GetInt32(IDataRecord record, int index)
    {
        return record.GetInt32(index);
    }

    static long GetInt64(IDataRecord record, int index)
    {
        return record.GetInt64(index);
    }
}

static class PrimitiveFillerBefore
{
    private static readonly Dictionary<Type, Func<IDataRecord, int, object>> fillersByType = new Dictionary<Type, Func<IDataRecord, int, object>>();

    static PrimitiveFillerBefore()
    {
        AddFiller(GetChar);
        AddFiller(GetDouble);
        AddFiller(GetDecimal);
        AddFiller(GetDateTime);
        AddFiller(GetBoolean);
        AddFiller(GetInt32);
        AddFiller(GetInt64);
        AddFiller(GetByte);
    }

    private static void AddFiller<TPrimitiveType>(Func<IDataRecord, int, TPrimitiveType> f)
    {
        fillersByType.Add(typeof(TPrimitiveType), (reader, index) => f(reader, index)!);
    }

    public static TType GetReaderValue<TType>(IDataRecord reader, int index)
    {
        if (!fillersByType.TryGetValue(typeof(TType), out var fillMethod))
        {
            throw new InvalidOperationException();
        }

        return (TType)fillMethod(reader, index);
    }

    static decimal GetDecimal(IDataRecord record, int index)
    {
        return record.GetDecimal(index);
    }

    static bool GetBoolean(IDataRecord record, int index)
    {
        return record.GetBoolean(index);
    }

    static char GetChar(IDataRecord record, int index)
    {
        return record.GetChar(index);
    }

    static DateTime GetDateTime(IDataRecord record, int index)
    {
        return record.GetDateTime(index);
    }

    static double GetDouble(IDataRecord record, int index)
    {
        return record.GetDouble(index);
    }

    static byte GetByte(IDataRecord record, int index)
    {
        return record.GetByte(index);
    }

    static int GetInt32(IDataRecord record, int index)
    {
        return record.GetInt32(index);
    }

    static long GetInt64(IDataRecord record, int index)
    {
        return record.GetInt64(index);
    }
}

class DataRecord : IDataRecord
{
    public bool GetBoolean(int i)
    {
        return true;
    }

    public byte GetByte(int i)
    {
        return 124;
    }

    public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
    {
        return 1;
    }

    public char GetChar(int i)
    {
        return 'c';
    }

    public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
    {
        throw new NotImplementedException();
    }

    public IDataReader GetData(int i)
    {
        throw new NotImplementedException();
    }

    public string GetDataTypeName(int i)
    {
        throw new NotImplementedException();
    }

    public DateTime GetDateTime(int i)
    {
        return DateTime.Now;
    }

    public decimal GetDecimal(int i)
    {
        return 12.34m;
    }

    public double GetDouble(int i)
    {
        return 12.34d;
    }

    public Type GetFieldType(int i)
    {
        throw new NotImplementedException();
    }

    public float GetFloat(int i)
    {
        throw new NotImplementedException();
    }

    public Guid GetGuid(int i)
    {
        throw new NotImplementedException();
    }

    public short GetInt16(int i)
    {
        throw new NotImplementedException();
    }

    public int GetInt32(int i)
    {
        return 43;
    }

    public long GetInt64(int i)
    {
        return 42;
    }

    public string GetName(int i)
    {
        throw new NotImplementedException();
    }

    public int GetOrdinal(string name)
    {
        throw new NotImplementedException();
    }

    public string GetString(int i)
    {
        throw new NotImplementedException();
    }

    public object GetValue(int i)
    {
        throw new NotImplementedException();
    }

    public int GetValues(object[] values)
    {
        throw new NotImplementedException();
    }

    public bool IsDBNull(int i)
    {
        throw new NotImplementedException();
    }

    public int FieldCount { get; }

    public object this[int i] => throw new NotImplementedException();

    public object this[string name] => throw new NotImplementedException();
}