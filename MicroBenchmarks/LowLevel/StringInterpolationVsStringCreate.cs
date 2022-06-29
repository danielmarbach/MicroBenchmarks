using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.LowLevel
{
    [Config(typeof(Config))]
    public class StringInterpolationVsStringCreate
    {
        private string value1;
        private string value2;
        private string value3;
        private const char Seperator = '_';

        private class Config : ManualConfig
        {
            public Config()
            {
                AddExporter(MarkdownExporter.GitHub);
                AddDiagnoser(MemoryDiagnoser.Default);
                AddColumn(StatisticColumn.AllStatistics);
                AddJob(Job.Default.WithUnrollFactor(1024));
            }
        }

        [IterationSetup]
        public void IterationSetup()
        {
            value1 = Guid.NewGuid().ToString();
            value2 = Guid.NewGuid().ToString();
            value3 = Guid.NewGuid().ToString();
        }

        [Benchmark(Baseline = true)]
        public string Interpolation()
        {
            return $"{value1}{Seperator}{value2}{Seperator}{value3}";
        }

        [Benchmark]
        public string StringCreate()
        {
            var length = value1.Length + value2.Length + value3.Length + 2;
            return string.Create(length, (value1, value2, value3), static (buffer, state) =>
            {
                var position = 0;
                state.value1.AsSpan().CopyTo(buffer);
                position += state.value1.Length;

                buffer[position++] = Seperator;

                state.value2.AsSpan().CopyTo(buffer[position..]);
                position += state.value2.Length;

                buffer[position++] = Seperator;

                state.value3.AsSpan().CopyTo(buffer[position..]);
            });
        }

        [Benchmark]
        public string StringCreateReverse()
        {
            var length = value1.Length + value2.Length + value3.Length + 2;
            return string.Create(length, (value1, value2, value3, length), static (buffer, state) =>
            {
                var position = state.length;
                position -= state.value3.Length;
                state.value3.AsSpan().CopyTo(buffer[position..]);

                buffer[--position] = Seperator;

                position -= state.value2.Length;
                state.value2.AsSpan().CopyTo(buffer[position..]);

                buffer[--position] = Seperator;

                state.value1.AsSpan().CopyTo(buffer);
            });
        }
    }
}