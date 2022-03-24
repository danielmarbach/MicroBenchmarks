using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.NServiceBus
{
    [Config(typeof(Config))]
    public class DeserializeMessageConnector
    {
        private Consumer consumer;
        private MessageMetadataRegistry registry;
        private Dictionary<string, string>[] headers;

        private class Config : ManualConfig
        {
            public Config()
            {
                AddExporter(MarkdownExporter.GitHub);
                AddDiagnoser(MemoryDiagnoser.Default);
                AddJob(Job.ShortRun);
            }
        }

        [Params(1, 200, 400, 800)]
        public int Calls { get; set; }

        [GlobalSetup]
        public void SetUp()
        {
            consumer = new Consumer();
            registry = new MessageMetadataRegistry();
            headers = new Dictionary<string, string>[120];
            for (var i = 0; i < 120; i++)
            {
                headers[i] = new Dictionary<string, string>
                {
                    {
                        Headers.EnclosedMessageTypes,
                        $"Shipping.OrderAccepted{i}, Shared{i}, Version=1.0.0.0, Culture=neutral, PublicKeyToken=XYZ;Shipping.IOrderAccepted{i}, Shared{i}, Version=1.0.0.0, Culture=neutral, PublicKeyToken=XYZ;Shipping.IOrderStatusChanged{i}, Shared{i}, Version=1.0.0.0, Culture=neutral, PublicKeyToken=XYZ"
                    }
                };
            }
        }

        [Benchmark(Baseline = true)]
        public void Before()
        {
            for (var i = 0; i < Calls; i++)
            {
                var types = ConnectorSimulator.Before(headers[i % 120], false, registry);
                consumer.Consume(types);
            }
        }
        
        [Benchmark]
        public void After()
        {
            for (var i = 0; i < Calls; i++)
            {
                var types = ConnectorSimulator.After(headers[i % 120], false, registry);
                consumer.Consume(types);
            }
        }
    }
    
    [Config(typeof(Config))]
    public class DeserializeMessageConnectorConcurrent
    {
        private Consumer[] consumers;
        private MessageMetadataRegistry registry;
        private Dictionary<string, string>[] headers;

        private class Config : ManualConfig
        {
            public Config()
            {
                AddExporter(MarkdownExporter.GitHub);
                AddDiagnoser(MemoryDiagnoser.Default);
                AddJob(Job.ShortRun);
            }
        }

        [Params(10, 20, 40)]
        public int Concurrency { get; set; }

        [GlobalSetup]
        public void SetUp()
        {
            consumers = new Consumer[Concurrency];
            for (int i = 0; i < Concurrency; i++)
            {
                consumers[i] = new Consumer();
            }
            registry = new MessageMetadataRegistry();
            headers = new Dictionary<string, string>[120];
            for (var i = 0; i < 120; i++)
            {
                headers[i] = new Dictionary<string, string>
                {
                    {
                        Headers.EnclosedMessageTypes,
                        $"Shipping.OrderAccepted{i}, Shared{i}, Version=1.0.0.0, Culture=neutral, PublicKeyToken=XYZ;Shipping.IOrderAccepted{i}, Shared{i}, Version=1.0.0.0, Culture=neutral, PublicKeyToken=XYZ;Shipping.IOrderStatusChanged{i}, Shared{i}, Version=1.0.0.0, Culture=neutral, PublicKeyToken=XYZ"
                    }
                };
            }
        }

        [Benchmark(Baseline = true)]
        public async Task Before()
        {
            var tasks = new List<Task>(Concurrency);
            for (var i = 0; i < Concurrency; i++)
            {
                tasks.Add(Task.Factory.StartNew(static state =>
                {
                    var (headers, registry, consumer) = (ValueTuple<Dictionary<string, string>[], MessageMetadataRegistry, Consumer>) state;
                    for (var j = 0; j < 500; j++)
                    {
                        var types = ConnectorSimulator.Before(headers[j % 120], false, registry);
                        consumer.Consume(types);
                    }
                }, (headers, registry, consumers[i])));
            }

            await Task.WhenAll(tasks);
        }
        
        [Benchmark]
        public async Task After()
        {
            var tasks = new List<Task>(Concurrency);
            for (var i = 0; i < Concurrency; i++)
            {
                tasks.Add(Task.Factory.StartNew(static state =>
                {
                    var (headers, registry, consumer) = (ValueTuple<Dictionary<string, string>[], MessageMetadataRegistry, Consumer>) state;
                    for (var j = 0; j < 500; j++)
                    {
                        var types = ConnectorSimulator.After(headers[j % 120], false, registry);
                        consumer.Consume(types);
                    }
                }, (headers, registry, consumers[i])));
            }

            await Task.WhenAll(tasks);
        }
    }

    class MessageMetadata
    {
        public Type MessageType { get; set; }
    }

    static class Headers
    {
        public const string EnclosedMessageTypes = "EnclosedMessageTypes";
    }

    class MessageMetadataRegistry
    {
        ConcurrentDictionary<string, MessageMetadata> cachedTypes = new ConcurrentDictionary<string, MessageMetadata>();
        public MessageMetadata? GetMessageMetadata(string typeString)
        {
            // this is overly simplified but good enough to create a fair comparison
            return cachedTypes.GetOrAdd(typeString, _ => new MessageMetadata {MessageType = typeof(object)});
        }
    }
    
    static class ConnectorSimulator
    {
        public static List<Type> Before(Dictionary<string, string> headers, bool allowContentTypeInference, MessageMetadataRegistry messageMetadataRegistry)
        {
            var messageMetadata = new List<MessageMetadata>();

            if (headers.TryGetValue(Headers.EnclosedMessageTypes, out var messageTypeIdentifier))
            {   
                foreach (var messageTypeString in messageTypeIdentifier.Split(EnclosedMessageTypeSeparator))
                {
                    var typeString = messageTypeString;

                    if (DoesTypeHaveImplAddedByVersion3Before(typeString))
                    {
                        continue;
                    }

                    var metadata = messageMetadataRegistry.GetMessageMetadata(typeString);

                    if (metadata == null)
                    {
                        continue;
                    }

                    messageMetadata.Add(metadata);
                }

                if (messageMetadata.Count == 0 && allowContentTypeInference)
                {
                    Console.WriteLine("Could not determine message type from message header '{0}'. MessageId: {1}", messageTypeIdentifier);
                }
            }

            if (messageMetadata.Count == 0 && !allowContentTypeInference)
            {
                throw new Exception($"Could not determine the message type from the '{Headers.EnclosedMessageTypes}' header and message type inference from the message body has been disabled. Ensure the header is set or enable message type inference.");
            }

            var messageTypes = messageMetadata.Select(metadata => metadata.MessageType).ToList();
            return messageTypes;
        }
        
        static bool DoesTypeHaveImplAddedByVersion3Before(string existingTypeString)
        {
            return existingTypeString.Contains("__impl");
        }

        public static Type[] After(Dictionary<string, string> headers, bool allowContentTypeInference, MessageMetadataRegistry messageMetadataRegistry)
        {
            Type[] messageTypes = Array.Empty<Type>();
            if (headers.TryGetValue(Headers.EnclosedMessageTypes, out var messageTypeIdentifier))
            {
                messageTypes = enclosedMessageTypesStringToMessageTypes.GetOrAdd(messageTypeIdentifier,
                    (key, registry) =>
                    {
                        string[] messageTypeStrings = key.Split(EnclosedMessageTypeSeparator);
                        var types = new List<Type>(messageTypeStrings.Length);
                        for (var index = 0; index < messageTypeStrings.Length; index++)
                        {
                            string messageTypeString = messageTypeStrings[index];
                            if (DoesTypeHaveImplAddedByVersion3After(messageTypeString))
                            {
                                continue;
                            }

                            var metadata = registry.GetMessageMetadata(messageTypeString);

                            if (metadata == null)
                            {
                                continue;
                            }

                            types.Add(metadata.MessageType);
                        }

                        // using an array in order to be able to assign array empty as the default value
                        return types.ToArray();
                    }, messageMetadataRegistry);

                if (messageTypes.Length == 0 && allowContentTypeInference)
                {
                    Console.WriteLine("Could not determine message type from message header '{0}'. MessageId: {1}", messageTypeIdentifier);
                }
            }

            if (messageTypes.Length == 0 && !allowContentTypeInference)
            {
                throw new Exception($"Could not determine the message type from the '{Headers.EnclosedMessageTypes}' header and message type inference from the message body has been disabled. Ensure the header is set or enable message type inference.");
            }

            return messageTypes;
        }
        
        static bool DoesTypeHaveImplAddedByVersion3After(string existingTypeString) => existingTypeString.AsSpan().IndexOf("__impl".AsSpan()) != -1;
        
        static readonly ConcurrentDictionary<string, Type[]> enclosedMessageTypesStringToMessageTypes =
            new ConcurrentDictionary<string, Type[]>();
        
        static readonly char[] EnclosedMessageTypeSeparator =
        {
            ';'
        };
    }
}