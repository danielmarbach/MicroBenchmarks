using System;
using System.Collections.Generic;
using System.Data.HashFunction;
using System.Data.HashFunction.FarmHash;
using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using FastHashes;

namespace MicroBenchmarks.Hashing
{
    [Config(typeof(Config))]
    public class Hashing
    {
        private List<int> list;
        private IList<int> ilist;
        private static SHA1CryptoServiceProvider sha1CryptoServiceProvider;
        private static SHA1 sha1CryptoServiceProviderNg;
        private static FarmHash128 farmHash128Provider;
        public static readonly IFarmHashFingerprint128 FarmHashFingerprint128 = FarmHashFingerprint128Factory.Instance.Create();

        private class Config : ManualConfig
        {
            public Config()
            {
                AddExporter(CsvMeasurementsExporter.Default);
                AddExporter(RPlotExporter.Default);
                AddExporter(MarkdownExporter.GitHub);
                AddDiagnoser(MemoryDiagnoser.Default);
                AddColumn(StatisticColumn.AllStatistics);
            }
        }

        [Params(1, 1000)]
        public int NumberOfGuids { get; set; }


        [GlobalSetup]
        public void SetUp()
        {
            sha1CryptoServiceProvider = new SHA1CryptoServiceProvider();
            sha1CryptoServiceProviderNg = SHA1.Create();
            farmHash128Provider = new FarmHash128(ulong.MinValue);
        }

        [Benchmark(Baseline = true)]
        public void DeterministcGuidRegular()
        {
            for (var i = 0; i < NumberOfGuids; i++)
            {
                DeterministicGuid($"{typeof(Hashing).FullName}_PropertyName_Value");
            }
        }

        [Benchmark()]
        public void DeterministcGuidReuse()
        {
            for (var i = 0; i < NumberOfGuids; i++)
            {
                DeterministicGuidReuse($"{typeof(Hashing).FullName}_PropertyName_Value");
            }
        }

        [Benchmark()]
        public void DeterministcGuidNg()
        {
            for (var i = 0; i < NumberOfGuids; i++)
            {
                DeterministicGuidNg($"{typeof(Hashing).FullName}_PropertyName_Value");
            }
        }

        [Benchmark]
        public void DeterministcGuidNgReuse()
        {
            for (var i = 0; i < NumberOfGuids; i++)
            {
                DeterministicGuidNgReuse($"{typeof(Hashing).FullName}_PropertyName_Value");
            }
        }

        [Benchmark()]
        public void DeterministcGuidFarmHash()
        {
            for (var i = 0; i < NumberOfGuids; i++)
            {
                DeterministicFarmHash($"{typeof(Hashing).FullName}_PropertyName_Value");
            }
        }

        [Benchmark]
        public void DeterministcGuidFarmHashReuse()
        {
            for (var i = 0; i < NumberOfGuids; i++)
            {
                DeterministicFarmHashReuse($"{typeof(Hashing).FullName}_PropertyName_Value");
            }
        }

        [Benchmark]
        public void DeterministcGuidFarmHashFingerPrintReuse()
        {
            for (var i = 0; i < NumberOfGuids; i++)
            {
                DeterministicFarmHashFingerprintReuse($"{typeof(Hashing).FullName}_PropertyName_Value");
            }
        }

        static Guid DeterministicGuid(string src)
        {
            var stringBytes = Encoding.UTF8.GetBytes(src);

            using (var provider = new SHA1CryptoServiceProvider())
            {
                var hashedBytes = provider.ComputeHash(stringBytes);
                Array.Resize(ref hashedBytes, 16);
                return new Guid(hashedBytes);
            }
        }

        static Guid DeterministicGuidReuse(string src)
        {
            var stringBytes = Encoding.UTF8.GetBytes(src);

            var hashedBytes = sha1CryptoServiceProvider.ComputeHash(stringBytes);
            Array.Resize(ref hashedBytes, 16);
            return new Guid(hashedBytes);
        }

        static Guid DeterministicGuidNg(string src)
        {
            var stringBytes = Encoding.UTF8.GetBytes(src);

            using (var provider = SHA1.Create())
            {
                var hashedBytes = provider.ComputeHash(stringBytes);
                Array.Resize(ref hashedBytes, 16);
                return new Guid(hashedBytes);
            }
        }

        static Guid DeterministicGuidNgReuse(string src)
        {
            var stringBytes = Encoding.UTF8.GetBytes(src);

            var hashedBytes = sha1CryptoServiceProviderNg.ComputeHash(stringBytes);
            Array.Resize(ref hashedBytes, 16);
            return new Guid(hashedBytes);
        }

        static Guid DeterministicFarmHash(string src)
        {
            var stringBytes = Encoding.UTF8.GetBytes(src);
            var provider = new FarmHash128(ulong.MinValue);
            return new Guid(provider.ComputeHash(stringBytes));
        }

        static Guid DeterministicFarmHashReuse(string src)
        {
            var stringBytes = Encoding.UTF8.GetBytes(src);

            return new Guid(farmHash128Provider.ComputeHash(stringBytes));
        }

        static Guid DeterministicFarmHashFingerprintReuse(string src)
        {
            var stringBytes = Encoding.UTF8.GetBytes(src);

            return new Guid(FarmHashFingerprint128.ComputeHash(stringBytes).Hash);
        }
    }
}