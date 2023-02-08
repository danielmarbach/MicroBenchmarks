using System;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;

namespace MicroBenchmarks.RegularExpressions;

[Config(typeof(Config))]
public class RegexCompiledRegexMatch
{
    private readonly string jwtRegex = @"eyJhbGciOi[\w\-\.]+";
    private Regex regex;
    private const string MaskValue = "***MASKED***";

    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
        }
    }

    [GlobalSetup]
    public void Setup()
    {
        regex = new System.Text.RegularExpressions.Regex(jwtRegex, RegexOptions.Compiled, TimeSpan.FromSeconds(5));
    }

    [Benchmark(Baseline = true)]
    public string RegexMatch()
    {
        return System.Text.RegularExpressions.Regex.Replace("Token eyJhbGciOiblahblah.b0dy-.sig_nature arrived", jwtRegex, MaskValue);
    }

    [Benchmark]
    public string Compile()
    {
        return regex.Replace("Token eyJhbGciOiblahblah.b0dy-.sig_nature arrived", MaskValue);
    }
}