using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.Linq
{
    [Config(typeof(Config))]
    public class ByteVsLongComparison
    {
        private byte[] array;
        private byte[] predefinedArray;
        private long predefinedLong;

        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
            }
        }
        [Params(8)]
        public int Elements { get; set; }

        [GlobalSetup]
        public void SetUp()
        {
            array = predefinedArray = new byte[Elements];
            for (int i = 0; i <  Elements; i++)
            {
                array[i] = (byte)i;
            }

            predefinedLong = System.BitConverter.ToInt64(array, 0);
            Array.Copy(array, predefinedArray, array.Length);
        }

        [Benchmark(Baseline = true)]
        public bool BitConverter()
        {
            return System.BitConverter.ToInt64(array, 0) == predefinedLong;
        }

        [Benchmark]
        public bool StructuralComparison()
        {
            return StructuralComparisons.StructuralEqualityComparer.Equals(array, predefinedArray);
        }
    }
}