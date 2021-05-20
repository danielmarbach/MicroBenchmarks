namespace MicroBenchmarks.LowLevel
{
    using System.Runtime.CompilerServices;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Columns;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Exporters;

    [Config(typeof(Config))]
    public class UnsafeOrCast
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(MemoryDiagnoser.Default);
                Add(StatisticColumn.AllStatistics);
                //Add(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(printSource: true, exportDiff: true)));
            }
        }

        [Benchmark(Baseline = true)]
        public int CastMethod()
        {
            return CastUtil.GetValue<int>();
        }

        [Benchmark()]
        public int UnsafeMethod()
        {
            return UnsafeUtil.GetValue<int>();
        }

        static class CastUtil {
            public static TValue GetValue<TValue>() {
                var type = typeof(TValue);

                switch(type) {
                    case {} when type == typeof(int):
                        var intValue = GetInt();
                        return (TValue)(object)intValue;
                    case {} when type == typeof(double):
                        var doubleValue = GetDouble();
                        return (TValue)(object)doubleValue;
                }
                return default;
            }

            public static int GetInt() => 4;
            public static double GetDouble() => 4;
        }

        static class UnsafeUtil {
            public static TValue GetValue<TValue>() {
                var type = typeof(TValue);

                switch(type) {
                    case {} when type == typeof(int):
                        var intValue = GetInt();
                        return Unsafe.As<int, TValue>(ref intValue);
                    case {} when type == typeof(double):
                        var doubleValue = GetDouble();
                        return Unsafe.As<double, TValue>(ref doubleValue);
                }
                return default;
            }

            public static int GetInt() => 4;
            public static double GetDouble() => 4;
        }
    }
}