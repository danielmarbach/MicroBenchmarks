using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;

namespace MicroBenchmarks.LowLevel;

[Config(typeof(Config))]
public class StringContainsVsIndexOf
{
    private string MatchingString = "ThisIsAVeryLongStringContainingTheThingWeLookFor__impl";
        
    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddColumn(StatisticColumn.AllStatistics);
        }
    }
        
    [Benchmark(Baseline = true)]
    public bool Contains()
    {
        return MatchingString.Contains("__impl");
    }
        
    [Benchmark]
    public bool IndexOf()
    {
        return MatchingString.AsSpan().IndexOf("__impl".AsSpan()) != -1;
    }
}