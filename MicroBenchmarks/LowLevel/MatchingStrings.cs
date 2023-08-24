using System;
using BenchmarkDotNet.Attributes;

namespace MicroBenchmarks.LowLevel;

[SimpleJob]
[MemoryDiagnoser]
public class MatchingStrings
{
    [Params("SomeAssembly.Dll", "SomeAssembly.Exe", "SomeAssembly")]
    public string Input { get; set; }

    [Benchmark(Baseline = true)]
    public bool MatchOriginal() => IsMatch(Input, Input);
    
    [Benchmark]
    public bool MatchOptimized() => IsMatch2(Input, Input);
    
    static bool IsMatch2(string expression1, string expression2)
        => string.Equals(RemoveExtensionIfNecessary(expression1),RemoveExtensionIfNecessary(expression2), StringComparison.OrdinalIgnoreCase);
    
    static bool IsMatch(string expression1, string expression2)
        => DistillLowerAssemblyName(expression1) == DistillLowerAssemblyName(expression2);
    
    static string DistillLowerAssemblyName(string assemblyOrFileName)
    {
        var lowerAssemblyName = assemblyOrFileName.ToLowerInvariant();
        if (lowerAssemblyName.EndsWith(".dll") || lowerAssemblyName.EndsWith(".exe"))
        {
            lowerAssemblyName = lowerAssemblyName.Substring(0, lowerAssemblyName.Length - 4);
        }
        return lowerAssemblyName;
    }
    
    static string RemoveExtensionIfNecessary(string assemblyOrFileName)
    {
        if (assemblyOrFileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || assemblyOrFileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return assemblyOrFileName[..^4];
        }
        return assemblyOrFileName;
    }
}