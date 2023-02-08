using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.DictionaryTable;

[Config(typeof(Config))]
public class DictionaryTableMappingExecutionWithSimpleState
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.LongRun);
        }
    }

    [Params(1, 20, 100,1000)]
    public int Calls { get; set; }

    [Params(false, true)]
    public bool Warmup { get; set; }

    [GlobalSetup]
    public void SetUp()
    {
        Data = new SimpleStateSagaData
        {
            NullableDouble = 4.5d,
            ByteArray = new byte[] {1},
            NullableBool = true,
            NullableGuid = new Guid("3C623C1F-80AB-4036-86CA-C2020FAE2EFE"),
            NullableLong = 10,
            NullableInt = 10,
        };

        if (!Warmup)
        {
            return;
        }
        GC.KeepAlive(DictionaryTableEntityExtensions.ToEntity(typeof(SimpleStateSagaData), DictionaryTableEntityExtensions.ToDictionaryTableEntity(Data, new DictionaryTableEntity())));
        GC.KeepAlive(DictionaryTableEntityExtensionsNew.ToSagaDataNew<SimpleStateSagaData>(DictionaryTableEntityExtensionsNew.ToDictionaryTableEntity(Data, new DictionaryTableEntity())));
    }

    public SimpleStateSagaData Data { get; set; }

    [Benchmark(Baseline = true)]
    public void MappingBefore()
    {
        for (var i = 0; i < Calls; i++)
        {
            GC.KeepAlive(DictionaryTableEntityExtensions.ToEntity(typeof(SimpleStateSagaData), DictionaryTableEntityExtensions.ToDictionaryTableEntity(Data, new DictionaryTableEntity())));
        }
    }

    [Benchmark]
    public void MappingAfter()
    {
        for (var i = 0; i < Calls; i++)
        {
            GC.KeepAlive(DictionaryTableEntityExtensionsNew.ToSagaDataNew<SimpleStateSagaData>(DictionaryTableEntityExtensionsNew.ToDictionaryTableEntity(Data, new DictionaryTableEntity())));
        }
    }

    public class SimpleStateSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
        public Guid SomeId { get; set; }

        public double? NullableDouble { get; set; }

        public bool? NullableBool { get; set; }
        public int? NullableInt { get; set; }
        public Guid? NullableGuid { get; set; }
        public long? NullableLong { get; set; }
        public byte[] ByteArray { get; set; }

    }
}