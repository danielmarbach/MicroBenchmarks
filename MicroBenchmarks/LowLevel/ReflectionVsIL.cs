namespace MicroBenchmarks.LowLevel
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Exporters;

    [Config(typeof(Config))]
    public class ReflectionVsIL
    {
        private FieldInfo messageTemplateBackingField;
        private Action<LogEvent,MessageTemplate> templateSetter;
        private Action<LogEvent,MessageTemplate> delegateSetter;

        [GlobalSetup]
        public void Setup()
        {
            var fields = typeof(LogEvent).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            messageTemplateBackingField = fields.SingleOrDefault(f => f.Name.Contains("<MessageTemplate>"));

            DynamicMethod templateSetterMethod = new DynamicMethod("templateSetter", typeof(void), new[] { typeof(LogEvent), typeof(MessageTemplate) }, typeof(ReflectionVsIL));
            var ilGenerator = templateSetterMethod.GetILGenerator();
            // arg0.<field> = arg1
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stfld, messageTemplateBackingField!);
            ilGenerator.Emit(OpCodes.Ret);
            templateSetter = (Action<LogEvent,MessageTemplate>) templateSetterMethod.CreateDelegate(typeof(Action<LogEvent,MessageTemplate>));;
        }

        private class Config : ManualConfig
        {
            public Config()
            {
                AddExporter(MarkdownExporter.GitHub);
                AddDiagnoser(MemoryDiagnoser.Default);
            }
        }

        [Benchmark(Baseline = true)]
        public LogEvent SetValue()
        {
            var logEvent = new LogEvent();
            messageTemplateBackingField.SetValue(logEvent, new MessageTemplate());
            return logEvent;
        }

        [Benchmark]
        public LogEvent CreateDelegate()
        {
            var logEvent = new LogEvent();
            templateSetter(logEvent, new MessageTemplate());
            return logEvent;
        }

        [Benchmark]
        public LogEvent Emit()
        {
            var logEvent = new LogEvent();
            templateSetter(logEvent, new MessageTemplate());
            return logEvent;
        }

        public class LogEvent
        {
            public MessageTemplate MessageTemplate { get; }
        }
    }

    public class MessageTemplate
    {
    }
}