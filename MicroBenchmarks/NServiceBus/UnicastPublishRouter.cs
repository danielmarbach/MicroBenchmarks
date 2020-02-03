using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;

namespace MicroBenchmarks.NServiceBus
{
    [Config(typeof(Config))]
    public class UnicastPublishRouter
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(MemoryDiagnoser.Default);
            }
        }

        private IEnumerable<Subscriber> subscriber;

        [Params("Empty", "NullEndpoint", "NullEndpointAndNormalEndpoint", "NullEndpointAndNormalEndpointWithRedundancy", "NullEndpointAndTwoOthers")]
        public string Scenario { get; set; }

        Dictionary<string, IEnumerable<Subscriber>> scenarios = new Dictionary<string, IEnumerable<Subscriber>>
        {
            {"Empty", new List<Subscriber>()},
            {"NullEndpoint", new List<Subscriber>{ new Subscriber { Endpoint = null, TransportAddress = "someaddress" }}},
            {"NullEndpointAndNormalEndpoint", new List<Subscriber>
            {
                new Subscriber { Endpoint = null, TransportAddress = "someaddress" },
                new Subscriber { Endpoint = "NormalEndpoint", TransportAddress = "otheraddress" },
            }},
            {"NullEndpointAndNormalEndpointWithRedundancy", new List<Subscriber>
            {
                new Subscriber { Endpoint = null, TransportAddress = "someaddress" },
                new Subscriber { Endpoint = null, TransportAddress = "someaddress" },
                new Subscriber { Endpoint = "NormalEndpoint", TransportAddress = "otheraddress" },
                new Subscriber { Endpoint = "NormalEndpoint", TransportAddress = "otheraddress" },
            }},
            {"NullEndpointAndTwoOthers", new List<Subscriber>
            {
                new Subscriber { Endpoint = null, TransportAddress = "someaddress1" },
                new Subscriber { Endpoint = null, TransportAddress = "someaddress2" },
                new Subscriber { Endpoint = null, TransportAddress = "someaddress3" },
                new Subscriber { Endpoint = null, TransportAddress = "someaddress4" },
                new Subscriber { Endpoint = "Endpoint1", TransportAddress = "otheraddress1" },
                new Subscriber { Endpoint = "Endpoint1", TransportAddress = "otheraddress2" },
                new Subscriber { Endpoint = "Endpoint1", TransportAddress = "otheraddress3" },
                new Subscriber { Endpoint = "Endpoint1", TransportAddress = "otheraddress4" },
                new Subscriber { Endpoint = "Endpoint2", TransportAddress = "yetanother1" },
                new Subscriber { Endpoint = "Endpoint2", TransportAddress = "yetanother2" },
                new Subscriber { Endpoint = "Endpoint2", TransportAddress = "yetanother3" },
                new Subscriber { Endpoint = "Endpoint2", TransportAddress = "yetanother4" },
            }},
        };

        [Benchmark(Baseline = true)]
        public IEnumerable<UnicastRoutingStrategy> Before()
        {
            return RouteOld(scenarios[Scenario]);
        }

        [Benchmark]
        public IEnumerable<UnicastRoutingStrategy> After()
        {
            return RouteNew(scenarios[Scenario]);
        }

        List<UnicastRoutingStrategy> RouteOld(IEnumerable<Subscriber> subscribers)
        {
            return SelectDestinationsForEachEndpointOld(subscribers).Select(s => new UnicastRoutingStrategy(s)).ToList(); // need to materialize here to be fair
        }

        HashSet<string> SelectDestinationsForEachEndpointOld(IEnumerable<Subscriber> subscribers)
        {
            //Make sure we are sending only one to each transport destination. Might happen when there are multiple routing information sources.
            var addresses = new HashSet<string>();
            var destinationsByEndpoint = subscribers
                .GroupBy(d => d.Endpoint, d => d);

            foreach (var group in destinationsByEndpoint)
            {
                if (group.Key == null) //Routing targets that do not specify endpoint name
                {
                    //Send a message to each target as we have no idea which endpoint they represent
                    foreach (var subscriber in group)
                    {
                        addresses.Add(subscriber.TransportAddress);
                    }
                }
                else
                {
                    var instances = group.Select(s => s.TransportAddress).ToArray();
                    var subscriber = instances.First();
                    addresses.Add(subscriber);
                }
            }

            return addresses;
        }

        Dictionary<string, UnicastRoutingStrategy>.ValueCollection RouteNew(IEnumerable<Subscriber> subscribers)
        {
            return SelectDestinationsForEachEndpointNew(subscribers);
        }

        Dictionary<string, UnicastRoutingStrategy>.ValueCollection SelectDestinationsForEachEndpointNew(IEnumerable<Subscriber> subscribers)
        {
            //Make sure we are sending only one to each transport destination. Might happen when there are multiple routing information sources.
            var addresses = new Dictionary<string, UnicastRoutingStrategy>();
            Dictionary<string, List<string>> groups = null;
            foreach (var subscriber in subscribers)
            {
                if(subscriber.Endpoint == null)
                {
                    if (!addresses.ContainsKey(subscriber.TransportAddress))
                    {
                        addresses.Add(subscriber.TransportAddress, new UnicastRoutingStrategy(subscriber.TransportAddress));
                    }

                    continue;
                }

                groups = groups ?? new Dictionary<string, List<string>>();

                List<string> transportAddresses;
                if (groups.TryGetValue(subscriber.Endpoint, out transportAddresses))
                {
                    transportAddresses.Add(subscriber.TransportAddress);
                }
                else
                {
                    groups[subscriber.Endpoint] = new List<string> { subscriber.TransportAddress };
                }
            }

            if (groups != null)
            {
                foreach (var group in groups)
                {
                    var instances = group.Value.ToArray(); // could we avoid this?
                    var subscriber = instances.First();

                    if (!addresses.ContainsKey(subscriber))
                    {
                        addresses.Add(subscriber, new UnicastRoutingStrategy(subscriber));
                    }
                }
            }

            return addresses.Values;
        }


        class Subscriber
        {
            public string TransportAddress { get; set; }
            public string Endpoint { get; set; }
        }

        public class UnicastRoutingStrategy
        {
            public string Address { get; }

            public UnicastRoutingStrategy(string address)
            {
                Address = address;
            }
        }
    }
}