using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;

namespace MicroBenchmarks.Strings;

[Config(typeof(Config))]
public class StringCreateVsBuilder
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
        }
    }

    [Benchmark(Baseline = true)]
    public string StringBuilder()
    {
        return StringBuilderApproach("eyJhbGciOiblahblah.b0dy-.sig_nature.fifo");
    }

    [Benchmark]
    public string StringCreate()
    {
        return StringCreateApproach("eyJhbGciOiblahblah.b0dy-.sig_nature.fifo");
    }

    static string StringBuilderApproach(string input)
    {
        var skipCharacters = input.EndsWith(".fifo") ? 5 : 0;
        var charactersToProcess = input.Length - skipCharacters;
        var stringBuilder = new StringBuilder(input);
        for (var i = 0; i < charactersToProcess; ++i)
        {
            var c = stringBuilder[i];
            if (!char.IsLetterOrDigit(c)
                && c != '-'
                && c != '_')
            {
                stringBuilder[i] = '-';
            }
        }

        return stringBuilder.ToString();
    }

    static string StringCreateApproach(string input)
    {
        var skipCharacters = input.EndsWith(".fifo") ? 5 : 0;
        var charactersToProcess = input.Length - skipCharacters;
        return string.Create(input.Length, (input, charactersToProcess), static (chars, state) =>
        {
            var (queueName, charactersToProcess) = state;
            var queueNameSpan = queueName.AsSpan();
            for (int i = 0; i < chars.Length; i++)
            {
                var c = queueNameSpan[i];
                if (!char.IsLetterOrDigit(c)
                    && c != '-'
                    && c != '_'
                    && i < charactersToProcess)
                {
                    chars[i] = '-';
                }
                else
                {
                    chars[i] = c;
                }
            }
        });
    }
}