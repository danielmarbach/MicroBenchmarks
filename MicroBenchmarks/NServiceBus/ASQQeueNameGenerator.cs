using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Exporters;

namespace MicroBenchmarks.NServiceBus
{
    [Config(typeof(Config))]
    public class ASQQeueNameGenerator
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(new BenchmarkDotNet.Diagnosers.MemoryDiagnoser());
                Add(StatisticColumn.AllStatistics);
            }
        }

        static string[] QueueNames = new string[]
        {
            "Test1234Queue",
            "Test.Queue",
            "TestQueueTestQueueTestQueueTestQueueTestQueueTestQueueTestQueue",
            "Test1234QueueTest1234Queue",
        };

        static Random random = new Random();

        [Params(2, 4, 8, 16, 32, 64, 128, 256, 512, 1024)]
        public int Calls { get; set; }

        [Benchmark(Baseline = true)]
        public QueueAddressGenerator BeforeOptimizations()
        {
            var generator = new QueueAddressGenerator();

            for (int i = 0; i < Calls; i++)
            {
                GC.KeepAlive(generator.GetQueueName(QueueNames[random.Next(0, QueueNames.Length - 1)]));
            }

            return generator;
        }

        [Benchmark]
        public QueueAddressGeneratorCompiledRegex CompiledRegex()
        {
            var generator = new QueueAddressGeneratorCompiledRegex();

            for (int i = 0; i < Calls; i++)
            {
                GC.KeepAlive(generator.GetQueueName(QueueNames[random.Next(0, QueueNames.Length - 1)]));
            }

            return generator;
        }

        [Benchmark]
        public QueueAddressGeneratorCached Cached()
        {
            var generator = new QueueAddressGeneratorCached();

            for (int i = 0; i < Calls; i++)
            {
                GC.KeepAlive(generator.GetQueueName(QueueNames[random.Next(0, QueueNames.Length - 1)]));
            }

            return generator;
        }

        public class QueueAddressGenerator
        {
            public string GetQueueName(string address)
            {
                var name = SanitizeQueueName(address.ToLowerInvariant());
                var input = address.Replace('.', '-').ToLowerInvariant(); // this string was used in the past to calculate guid, should stay backward compat

                if (name.Length > 63)
                {
                    var shortenedName = shortener(input);
                    name = name.Substring(0, 63 - shortenedName.Length - 1).Trim('-') + "-" + shortenedName;
                }

                return name;
            }

            static string SanitizeQueueName(string queueName)
            {
                //rules for naming queues can be found at http://msdn.microsoft.com/en-us/library/windowsazure/dd179349.aspx"
                var invalidCharacters = new Regex(@"[^a-zA-Z0-9\-]");
                var sanitized = invalidCharacters.Replace(queueName, "-"); // this can lead to multiple - occurrences in a row
                var multipleDashes = new Regex(@"\-+");
                sanitized = multipleDashes.Replace(sanitized, "-");
                return sanitized;
            }

            Func<string, string> shortener = input => input;
        }

        public class QueueAddressGeneratorCompiledRegex
        {
            public string GetQueueName(string address)
            {
                var name = SanitizeQueueName(address.ToLowerInvariant());
                var input = address.Replace('.', '-').ToLowerInvariant(); // this string was used in the past to calculate guid, should stay backward compat

                if (name.Length > 63)
                {
                    var shortenedName = shortener(input);
                    name = name.Substring(0, 63 - shortenedName.Length - 1).Trim('-') + "-" + shortenedName;
                }

                return name;
            }

            static string SanitizeQueueName(string queueName)
            {
                //rules for naming queues can be found at http://msdn.microsoft.com/en-us/library/windowsazure/dd179349.aspx"
                var sanitized = invalidCharacters.Replace(queueName, "-"); // this can lead to multiple - occurrences in a row
                sanitized = multipleDashes.Replace(sanitized, "-");
                return sanitized;
            }

            Func<string, string> shortener = input => input;

            static Regex invalidCharacters = new Regex(@"[^a-zA-Z0-9\-]", RegexOptions.Compiled);
            static Regex multipleDashes = new Regex(@"\-+", RegexOptions.Compiled);
        }

        public class QueueAddressGeneratorCached
        {
            public string GetQueueName(string address)
            {
                var queueName = address.ToLowerInvariant();

                return sanitizedQueueNames.GetOrAdd(queueName, name => ShortenQueueNameIfNecessary(name, SanitizeQueueName(name)));
            }

            string ShortenQueueNameIfNecessary(string address, string queueName)
            {
                if (queueName.Length <= 63)
                {
                    return queueName;
                }

                var input = address.Replace('.', '-').ToLowerInvariant(); // this string was used in the past to calculate guid, should stay backward compat
                var shortenedName = shortener(input);
                queueName = $"{queueName.Substring(0, 63 - shortenedName.Length - 1).Trim('-')}-{shortenedName}";
                return queueName;
            }

            static string SanitizeQueueName(string queueName)
            {
                //rules for naming queues can be found at http://msdn.microsoft.com/en-us/library/windowsazure/dd179349.aspx"
                var sanitized = invalidCharacters.Replace(queueName, "-"); // this can lead to multiple - occurrences in a row
                sanitized = multipleDashes.Replace(sanitized, "-");
                return sanitized;
            }

            Func<string, string> shortener;
            ConcurrentDictionary<string, string> sanitizedQueueNames = new ConcurrentDictionary<string, string>();

            static Regex invalidCharacters = new Regex(@"[^a-zA-Z0-9\-]", RegexOptions.Compiled);
            static Regex multipleDashes = new Regex(@"\-+", RegexOptions.Compiled);
        }
    }
}