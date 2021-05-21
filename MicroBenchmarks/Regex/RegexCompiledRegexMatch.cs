namespace MicroBenchmarks.RegexBenchmarks
{
    using System;
    using System.Text.RegularExpressions;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Exporters;

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
                Add(MarkdownExporter.GitHub);
                Add(MemoryDiagnoser.Default);
            }
        }

        [GlobalSetup]
        public void Setup()
        {
            regex = new Regex(jwtRegex, RegexOptions.Compiled, TimeSpan.FromSeconds(5));
        }

        [Benchmark(Baseline = true)]
        public string RegexMatch()
        {
            return Regex.Replace("Token eyJhbGciOiblahblah.b0dy-.sig_nature arrived", jwtRegex, MaskValue);
        }

        [Benchmark]
        public string Compile()
        {
            return regex.Replace("Token eyJhbGciOiblahblah.b0dy-.sig_nature arrived", MaskValue);
        }
    }
}