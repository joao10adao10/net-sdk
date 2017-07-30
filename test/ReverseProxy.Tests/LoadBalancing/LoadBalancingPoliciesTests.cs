// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Xunit;
using Yarp.Tests.Common;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Model;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Utilities;

namespace Yarp.ReverseProxy.LoadBalancing.Tests;

public class LoadBalancingPoliciesTests : TestAutoMockBase
{
    public LoadBalancingPoliciesTests()
    {
        RandomFactory = new TestRandomFactory() { Instance = RandomInstance };
        Provide<IRandomFactory>(RandomFactory);
    }

    private TestRandom RandomInstance { get; set; } = new TestRandom();

    private TestRandomFactory RandomFactory { get; set; }

    [Fact]
    public void PickDestination_FirstWithDestinations_Works()
    {
        var destinations = new[]
        {
            new DestinationState("d1"),
            new DestinationState("d2"),
            new DestinationState("d3")
        };

        var context = new DefaultHttpContext();
        var loadBalancer = Create<FirstLoadBalancingPolicy>();

        for (var i = 0; i < 10; i++)
        {
            var result = loadBalancer.PickDestination(context, cluster: null, availableDestinations: destinations);
            Assert.Same(destinations[0], result);
            result.ConcurrentRequestCount++;
        }
    }

    [Fact]
    public void PickDestination_Random_Works()
    {
        var destinations = new[]
        {
            new DestinationState("d1"),
            new DestinationState("d2"),
            new DestinationState("d3")
        };

        const int Iterations = 10;
        var random = new Random(42);
        RandomInstance.Sequence = Enumerable.Range(0, Iterations).Select(_ => random.Next(destinations.Length)).ToArray();

        var context = new DefaultHttpContext();
        var loadBalancer = Create<RandomLoadBalancingPolicy>();

        for (var i = 0; i < Iterations; i++)
        {
            var result = loadBalancer.PickDestination(context, cluster: null, availableDestinations: destinations);
            Assert.Same(destinations[RandomInstance.Sequence[i]], result);
            result.ConcurrentRequestCount++;
        }
    }

    [Fact]
    public void PickDestination_PowerOfTwoChoices_Works()
    {
        var destinations = new[]
        {
            new DestinationState("d1"),
            new DestinationState("d2"),
            new DestinationState("d3")
        };
        destinations[0].ConcurrentRequestCount++;

        const int Iterations = 10;
        var random = new Random(42);
        RandomInstance.Sequence = Enumerable.Range(0, Iterations * 2).Select(_ => random.Next(destinations.Length)).ToArray();

        var context = new DefaultHttpContext();
        var loadBalancer = Create<PowerOfTwoChoicesLoadBalancingPolicy>();

        for (var i = 0; i < Iterations; i++)
        {
            var result = loadBalancer.PickDestination(context, cluster: null, availableDestinations: destinations);
            var first = destinations[RandomInstance.Sequence[i * 2]];
            var second = destinations[RandomInstance.Sequence[i * 2 + 1]];
            var expected = first.ConcurrentRequestCount <= second.ConcurrentRequestCount ? first : second;
            Assert.Same(expected, result);
            result.ConcurrentRequestCount++;
        }
    }

    [Fact]
    public void PickDestination_LeastRequests_Works()
    {
        var destinations = new[]
        {
            new DestinationState("d1"),
            new DestinationState("d2"),
            new DestinationState("d3")
        };
        destinations[0].ConcurrentRequestCount++;

        var context = new DefaultHttpContext();
        var loadBalancer = Create<LeastRequestsLoadBalancingPolicy>();

        for (var i = 0; i < 10; i++)
        {
            var result = loadBalancer.PickDestination(context, cluster: null, availableDestinations: destinations);
            Assert.Same(destinations.OrderBy(d => d.ConcurrentRequestCount).First(), result);
            result.ConcurrentRequestCount++;
        }
    }

    [Fact]
    public void PickDestination_RoundRobin_Works()
    {
        var destinations = new[]
        {
            new DestinationState("d1"),
            new DestinationState("d2"),
            new DestinationState("d3")
        };
        destinations[0].ConcurrentRequestCount++;

        var context = new DefaultHttpContext();

        var cluster = new ClusterState("cluster1");
        var routeConfig = new RouteModel(new RouteConfig(), cluster, HttpTransformer.Default);
        var feature = new ReverseProxyFeature()
        {
            Route = routeConfig,
        };
        context.Features.Set<IReverseProxyFeature>(feature);

        var loadBalancer = Create<RoundRobinLoadBalancingPolicy>();

        for (var i = 0; i < 10; i++)
        {
            var result = loadBalancer.PickDestination(context, cluster, availableDestinations: destinations);
            Assert.Same(destinations[i % destinations.Length], result);
            result.ConcurrentRequestCount++;
        }
    }
}
