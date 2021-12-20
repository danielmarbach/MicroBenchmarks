using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;

namespace MicroBenchmarks.NServiceBus;

[Config(typeof(Config))]
public class PatternMatchVsForEach
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddColumn(StatisticColumn.AllStatistics);
        }
    }

    private object neverMatches;
    private OrderAccepted orderAccepted;
    private OrderDeclined orderDeclined;
    private OrderApproved orderApproved;
    private ExtractorUsingPatternMatching extractorBase;

    private ExtractorUsingForEach extractorForeach;

    // here to make things consumed
    private bool[] results = new[] { false, false, false, false };

    [GlobalSetup]
    public void Setup()
    {
        neverMatches = new object();
        orderAccepted = new OrderAccepted();
        orderDeclined = new OrderDeclined();
        orderApproved = new OrderApproved();

        extractorBase = new ExtractorUsingPatternMatching();
        extractorForeach = new ExtractorUsingForEach();
    }

    [Benchmark(Baseline = true)]
    public bool[] PatternMatch()
    {
        results[0] = extractorBase.TryExtractFromMessage(neverMatches, out _, out _);
        results[1] = extractorBase.TryExtractFromMessage(orderAccepted, out _, out _);
        results[2] = extractorBase.TryExtractFromMessage(orderDeclined, out _, out _);
        results[3] = extractorBase.TryExtractFromMessage(orderApproved, out _, out _);
        return results;
    }

    [Benchmark]
    public bool[] ForEach()
    {
        results[0] = extractorForeach.TryExtractFromMessage(neverMatches, out _, out _);
        results[1] = extractorForeach.TryExtractFromMessage(orderAccepted, out _, out _);
        results[2] = extractorForeach.TryExtractFromMessage(orderDeclined, out _, out _);
        results[3] = extractorForeach.TryExtractFromMessage(orderApproved, out _, out _);
        return results;
    }

    public interface IProvideOrderId
    {
        Guid OrderId { get; }
    }

    class OrderApproved : IProvideOrderId
    {
        public Guid OrderId { get; }
    }
    
    class OrderAccepted : IProvideOrderId
    {
        public Guid OrderId { get; }
    }

    class OrderDeclined
    {
        public Guid OrderId { get; }
    }

    class ExtractorUsingPatternMatching : PartitionKeyExtractorBase
    {
        public ExtractorUsingPatternMatching()
        {
            ExtractFromMessageDirect<OrderApproved>(x => x.OrderId.ToString());
            ExtractFromMessageDirect<IProvideOrderId>(x => x.OrderId.ToString());
            ExtractFromMessageDirect<OrderDeclined>(x => x.OrderId.ToString());
        }

        protected override bool TryExtractFromMessageCore(object message, out string? partitionKey,
            out string? containerInformation)
        {
            var orderapproved = message as OrderApproved;
            if (orderapproved != null)
            {
                return Invoke<OrderApproved>(orderapproved, out partitionKey, out containerInformation);
            }
            
            var iprovideorderid = message as IProvideOrderId;
            if (iprovideorderid != null)
            {
                return Invoke<IProvideOrderId>(iprovideorderid, out partitionKey, out containerInformation);
            }

            var orderdeclined = message as OrderDeclined;
            if (orderdeclined != null)
            {
                return Invoke<OrderDeclined>(orderdeclined, out partitionKey, out containerInformation);
            }

            partitionKey = null;
            containerInformation = null;
            return false;
        }
    }

    class ExtractorUsingForEach : PartitionKeyExtractorBase
    {
        public ExtractorUsingForEach()
        {
            ExtractFromMessage<OrderApproved>(x => x.OrderId.ToString());
            ExtractFromMessage<IProvideOrderId>(x => x.OrderId.ToString());
            ExtractFromMessage<OrderDeclined>(x => x.OrderId.ToString());
        }
    }

    public abstract class PartitionKeyExtractorBase
    {
        readonly Dictionary<Type, IExtractPartitionKeyFromMessage> partitionKeyFromMessageExtractorsByTypeName =
            new Dictionary<Type, IExtractPartitionKeyFromMessage>();

        private readonly List<IExtractPartitionKeyFromMessage> partitionKeyFromMessageExtractors =
            new List<IExtractPartitionKeyFromMessage>();

        public bool TryExtractFromMessage(object message, out string? partitionKey,
            out string? containerInformation)
            => TryExtractFromMessageCore(message, out partitionKey, out containerInformation);

        protected virtual bool TryExtractFromMessageCore(object message, out string? partitionKey,
            out string? containerInformation)
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var index = 0; index < partitionKeyFromMessageExtractors.Count; index++)
            {
                var extractor = partitionKeyFromMessageExtractors[index];
                if (extractor.TryExtract(message, out partitionKey, out containerInformation))
                {
                    return true;
                }
            }

            partitionKey = null;
            containerInformation = null;
            return false;
        }

        protected void ExtractFromMessage<TMessage>(Func<TMessage, string> extractor,
            string? containerInformation = default)
        {
            partitionKeyFromMessageExtractorsByTypeName.Add(typeof(TMessage),  null);
            partitionKeyFromMessageExtractors.Add(new ExtractPartitionKeyFromMessage<TMessage, Func<TMessage, string>>(
                (msg, invoker) => invoker(msg), containerInformation, extractor));
        }

        protected void ExtractFromMessage<TMessage, TState>(Func<TMessage, TState, string> extractor,
            string? containerInformation = default, TState state = default)
            => partitionKeyFromMessageExtractorsByTypeName.Add(typeof(TMessage),
                new ExtractPartitionKeyFromMessage<TMessage, TState>(extractor, containerInformation, state));

        protected void ExtractFromMessageDirect<TMessage>(Func<TMessage, string> extractor,
            string? containerInformation = default)
            => partitionKeyFromMessageExtractorsByTypeName.Add(typeof(TMessage),
                new ExtractPartitionKeyFromMessageDirect<TMessage, Func<TMessage, string>>(
                    (msg, invoker) => invoker(msg), containerInformation, extractor));

        protected void ExtractFromMessageDirect<TMessage, TState>(Func<TMessage, TState, string> extractor,
            string? containerInformation = default, TState state = default)
            => partitionKeyFromMessageExtractorsByTypeName.Add(typeof(TMessage),
                new ExtractPartitionKeyFromMessageDirect<TMessage, TState>(extractor, containerInformation, state));

        protected bool Invoke<TMessage>(object message, out string? partitionKey, out string? containerInformation)
        {
            return partitionKeyFromMessageExtractorsByTypeName[typeof(TMessage)]
                .TryExtract(message, out partitionKey, out containerInformation);
        }

        interface IExtractPartitionKeyFromMessage
        {
            bool TryExtract(object message, out string? partitionKey, out string? containerInformation);
        }

        sealed class ExtractPartitionKeyFromMessage<TMessage, TState> : IExtractPartitionKeyFromMessage
        {
            readonly Func<TMessage, TState, string> extractor;
            readonly string? container;
            readonly TState state;

            public ExtractPartitionKeyFromMessage(Func<TMessage, TState, string> extractor, string? container,
                TState state = default)
            {
                this.state = state;
                this.container = container;
                this.extractor = extractor;
            }

            public bool TryExtract(object message, out string? partitionKey,
                out string? containerInformation)
            {
                if (message is TMessage typedMessage)
                {
                    partitionKey = extractor(typedMessage, state);
                    containerInformation = container;
                    return true;
                }
                partitionKey = null;
                containerInformation = null;
                return false;
            }
        }

        sealed class ExtractPartitionKeyFromMessageDirect<TMessage, TState> : IExtractPartitionKeyFromMessage
        {
            readonly Func<TMessage, TState, string> extractor;
            readonly string? container;
            readonly TState state;

            public ExtractPartitionKeyFromMessageDirect(Func<TMessage, TState, string> extractor, string? container,
                TState state = default)
            {
                this.state = state;
                this.container = container;
                this.extractor = extractor;
            }

            public bool TryExtract(object message, out string? partitionKey,
                out string? containerInformation)
            {
                partitionKey = extractor((TMessage) message, state);
                containerInformation = container;
                return true;
            }
        }
    }
}