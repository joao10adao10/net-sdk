// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yarp.ReverseProxy.Configuration;

namespace Yarp.ReverseProxy.Transforms.Builder.Tests;

public class TransformBuilderTests
{
    [Fact]
    public void CreateBuilder_Success()
    {
        CreateTransformBuilder();
    }

    [Fact]
    public void NullTransforms_AddsDefaults()
    {
        NullOrEmptyTransforms_AddsDefaults(null);
    }

    [Fact]
    public void EmptyTransforms_AddsDefaults()
    {
        NullOrEmptyTransforms_AddsDefaults(new List<IReadOnlyDictionary<string, string>>());
    }

    private void NullOrEmptyTransforms_AddsDefaults(IReadOnlyList<IReadOnlyDictionary<string, string>> transforms)
    {
        var transformBuilder = CreateTransformBuilder();

        var route = new RouteConfig { Transforms = transforms };
        var errors = transformBuilder.ValidateRoute(route);
        Assert.Empty(errors);

        var results = transformBuilder.BuildInternal(route, new ClusterConfig());
        Assert.NotNull(results);
        Assert.Null(results.ShouldCopyRequestHeaders);
        Assert.Null(results.ShouldCopyResponseHeaders);
        Assert.Null(results.ShouldCopyResponseTrailers);
        Assert.Empty(results.ResponseTransforms);
        Assert.Empty(results.ResponseTrailerTransforms);

        Assert.Equal(6, results.RequestTransforms.Length);
        var hostTransform = Assert.Single(results.RequestTransforms.OfType<RequestHeaderOriginalHostTransform>());
        Assert.False(hostTransform.UseOriginalHost);
        var forTransform = Assert.Single(results.RequestTransforms.OfType<RequestHeaderXForwardedForTransform>());
        Assert.Equal(ForwardedHeadersDefaults.XForwardedForHeaderName, forTransform.HeaderName);
        var xHostTransform = Assert.Single(results.RequestTransforms.OfType<RequestHeaderXForwardedHostTransform>());
        Assert.Equal(ForwardedHeadersDefaults.XForwardedHostHeaderName, xHostTransform.HeaderName);
        var prefixTransform = Assert.Single(results.RequestTransforms.OfType<RequestHeaderXForwardedPrefixTransform>());
        Assert.Equal("X-Forwarded-Prefix", prefixTransform.HeaderName);
        var protoTransform = Assert.Single(results.RequestTransforms.OfType<RequestHeaderXForwardedProtoTransform>());
        Assert.Equal(ForwardedHeadersDefaults.XForwardedProtoHeaderName, protoTransform.HeaderName);

        var removeForwardedTransform = Assert.Single(results.RequestTransforms.OfType<RequestHeaderForwardedTransform>());
        Assert.Equal(ForwardedTransformActions.Remove, removeForwardedTransform.TransformAction);
    }

    [Fact]
    public void CreateTransforms_ExecutesAction()
    {
        var transformBuilder = CreateTransformBuilder();

        var executed = false;
        var results = transformBuilder.CreateInternal(_ =>
        {
            executed = true;
        });

        Assert.True(executed);
    }

    [Fact]
    public void CreateTransforms_AddsDefaults()
    {
        var transformBuilder = CreateTransformBuilder();

        var results = transformBuilder.CreateInternal(_ => { });
        Assert.NotNull(results);
        Assert.Null(results.ShouldCopyRequestHeaders);
        Assert.Null(results.ShouldCopyResponseHeaders);
        Assert.Null(results.ShouldCopyResponseTrailers);
        Assert.Empty(results.ResponseTransforms);
        Assert.Empty(results.ResponseTrailerTransforms);

        Assert.Equal(6, results.RequestTransforms.Length);
        var hostTransform = Assert.Single(results.RequestTransforms.OfType<RequestHeaderOriginalHostTransform>());
        Assert.False(hostTransform.UseOriginalHost);
        var forTransform = Assert.Single(results.RequestTransforms.OfType<RequestHeaderXForwardedForTransform>());
        Assert.Equal(ForwardedHeadersDefaults.XForwardedForHeaderName, forTransform.HeaderName);
        var xHostTransform = Assert.Single(results.RequestTransforms.OfType<RequestHeaderXForwardedHostTransform>());
        Assert.Equal(ForwardedHeadersDefaults.XForwardedHostHeaderName, xHostTransform.HeaderName);
        var prefixTransform = Assert.Single(results.RequestTransforms.OfType<RequestHeaderXForwardedPrefixTransform>());
        Assert.Equal("X-Forwarded-Prefix", prefixTransform.HeaderName);
        var protoTransform = Assert.Single(results.RequestTransforms.OfType<RequestHeaderXForwardedProtoTransform>());
        Assert.Equal(ForwardedHeadersDefaults.XForwardedProtoHeaderName, protoTransform.HeaderName);

        var removeForwardedTransform = Assert.Single(results.RequestTransforms.OfType<RequestHeaderForwardedTransform>());
        Assert.Equal(ForwardedTransformActions.Remove, removeForwardedTransform.TransformAction);
    }

    [Fact]
    public void EmptyTransform_Error()
    {
        var transformBuilder = CreateTransformBuilder();
        var transforms = new[]
        {
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), // Empty
        };

        var route = new RouteConfig() { Transforms = transforms };
        var errors = transformBuilder.ValidateRoute(route);
        var error = Assert.Single(errors);
        Assert.Equal("Unknown transform: ", error.Message);

        var nie = Assert.Throws<ArgumentException>(() => transformBuilder.BuildInternal(route, new ClusterConfig()));
        Assert.Equal("Unknown transform: ", nie.Message);
    }

    [Fact]
    public void UnknownTransforms_Error()
    {
        var transformBuilder = CreateTransformBuilder();
        var transforms = new[]
        {
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) // Unknown transform
            {
                {  "string1", "value1" },
                {  "string2", "value2" }
            },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) // Unknown transform
            {
                {  "string3", "value3" },
                {  "string4", "value4" }
            },
        };

        var route = new RouteConfig() { Transforms = transforms };
        var errors = transformBuilder.ValidateRoute(route);
        //All errors reported
        Assert.Equal(2, errors.Count);
        Assert.Equal("Unknown transform: string1;string2", errors.First().Message);
        Assert.Equal("Unknown transform: string3;string4", errors.Skip(1).First().Message);
        var ex = Assert.Throws<ArgumentException>(() => transformBuilder.BuildInternal(route, new ClusterConfig()));
        // First error reported
        Assert.Equal("Unknown transform: string1;string2", ex.Message);
    }

    [Fact]
    public void CallsTransformFactories()
    {
        var factory1 = new TestTransformFactory("1");
        var factory2 = new TestTransformFactory("2");
        var factory3 = new TestTransformFactory("3");
        var builder = new TransformBuilder(new ServiceCollection().BuildServiceProvider(),
            new[] { factory1, factory2, factory3 }, Array.Empty<ITransformProvider>());

        var route = new RouteConfig().WithTransform(transform =>
        {
            transform["2"] = "B";
        });
        var errors = builder.ValidateRoute(route);
        Assert.Empty(errors);
        Assert.Equal(1, factory1.ValidationCalls);
        Assert.Equal(1, factory2.ValidationCalls);
        Assert.Equal(0, factory3.ValidationCalls);

        var transforms = builder.BuildInternal(route, new ClusterConfig());
        Assert.Equal(1, factory1.BuildCalls);
        Assert.Equal(1, factory2.BuildCalls);
        Assert.Equal(0, factory3.BuildCalls);

        Assert.Single(transforms.ResponseTrailerTransforms);
    }

    [Fact]
    public void CallsTransformProviders()
    {
        var provider1 = new TestTransformProvider();
        var provider2 = new TestTransformProvider();
        var provider3 = new TestTransformProvider();
        var builder = new TransformBuilder(new ServiceCollection().BuildServiceProvider(),
            Array.Empty<ITransformFactory>(), new[] { provider1, provider2, provider3 });

        var route = new RouteConfig();
        var errors = builder.ValidateRoute(route);
        Assert.Empty(errors);
        Assert.Equal(1, provider1.ValidateRouteCalls);
        Assert.Equal(1, provider2.ValidateRouteCalls);
        Assert.Equal(1, provider3.ValidateRouteCalls);

        var cluster = new ClusterConfig();
        errors = builder.ValidateCluster(cluster);
        Assert.Empty(errors);
        Assert.Equal(1, provider1.ValidateClusterCalls);
        Assert.Equal(1, provider2.ValidateClusterCalls);
        Assert.Equal(1, provider3.ValidateClusterCalls);

        var transforms = builder.BuildInternal(route, cluster);
        Assert.Equal(1, provider1.ApplyCalls);
        Assert.Equal(1, provider2.ApplyCalls);
        Assert.Equal(1, provider3.ApplyCalls);

        Assert.Equal(3, transforms.ResponseTrailerTransforms.Length);
    }

    [Fact]
    public void DefaultsCanBeDisabled()
    {
        var transformBuilder = CreateTransformBuilder();
        var transforms = new[]
        {
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {  "RequestHeadersCopy", "false" }
            },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {  "X-Forwarded", "Off" }
            },
        };

        var route = new RouteConfig() { Transforms = transforms };
        var errors = transformBuilder.ValidateRoute(route);
        Assert.Empty(errors);

        var results = transformBuilder.BuildInternal(route, new ClusterConfig());
        Assert.NotNull(results);
        Assert.False(results.ShouldCopyRequestHeaders);
        Assert.Empty(results.RequestTransforms);
        Assert.Empty(results.ResponseTransforms);
        Assert.Empty(results.ResponseTrailerTransforms);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, true)]
    [InlineData(null, false)]
    [InlineData(true, null)]
    [InlineData(false, null)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task UseOriginalHost(bool? useOriginalHost, bool? copyHeaders)
    {
        var transformBuilder = CreateTransformBuilder();
        var transforms = new List<Dictionary<string, string>>();
        // Disable default forwarders
        transforms.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {  "X-Forwarded", "Off" }
        });
        if (useOriginalHost.HasValue)
        {
            transforms.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {  "RequestHeaderOriginalHost", useOriginalHost.ToString() }
            });
        }
        if (copyHeaders.HasValue)
        {
            transforms.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {  "RequestHeadersCopy", copyHeaders.ToString() }
            });
        }

        var route = new RouteConfig() { Transforms = transforms };
        var errors = transformBuilder.ValidateRoute(route);
        Assert.Empty(errors);

        var results = transformBuilder.BuildInternal(route, new ClusterConfig());
        Assert.NotNull(results);
        Assert.Equal(copyHeaders, results.ShouldCopyRequestHeaders);
        Assert.Empty(results.ResponseTransforms);
        Assert.Empty(results.ResponseTrailerTransforms);

        if (useOriginalHost.HasValue)
        {
            var transform = Assert.Single(results.RequestTransforms);
            var hostTransform = Assert.IsType<RequestHeaderOriginalHostTransform>(transform);
            Assert.Equal(useOriginalHost.Value, hostTransform.UseOriginalHost);
        }
        else if (copyHeaders.GetValueOrDefault(true))
        {
            var transform = Assert.Single(results.RequestTransforms);
            var hostTransform = Assert.IsType<RequestHeaderOriginalHostTransform>(transform);
            Assert.False(hostTransform.UseOriginalHost);
        }
        else
        {
            Assert.Empty(results.RequestTransforms);
        }

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("StartHost");
        var proxyRequest = new HttpRequestMessage();
        var destinationPrefix = "http://destinationhost:9090/path";
        await results.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix);

        if (useOriginalHost.GetValueOrDefault(false))
        {
            Assert.Equal("StartHost", proxyRequest.Headers.Host);
        }
        else
        {
            Assert.Null(proxyRequest.Headers.Host);
        }
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, true)]
    [InlineData(null, false)]
    [InlineData(true, null)]
    [InlineData(false, null)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    // https://github.com/microsoft/reverse-proxy/issues/859
    // Verify that a custom host works no matter what combination of
    // useOriginalHost and copyHeaders is used.
    public async Task UseCustomHost(bool? useOriginalHost, bool? copyHeaders)
    {
        var transformBuilder = CreateTransformBuilder();
        var transforms = new List<Dictionary<string, string>>();
        // Disable default forwarders
        transforms.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {  "X-Forwarded", "Off" }
        });
        transforms.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {  "RequestHeader", "Host" },
            {  "Set", "CustomHost" }
        });
        if (useOriginalHost.HasValue)
        {
            transforms.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {  "RequestHeaderOriginalHost", useOriginalHost.ToString() }
            });
        }
        if (copyHeaders.HasValue)
        {
            transforms.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {  "RequestHeadersCopy", copyHeaders.ToString() }
            });
        }

        var route = new RouteConfig() { Transforms = transforms };
        var errors = transformBuilder.ValidateRoute(route);
        Assert.Empty(errors);

        var results = transformBuilder.BuildInternal(route, new ClusterConfig());
        Assert.Equal(copyHeaders, results.ShouldCopyRequestHeaders);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("StartHost");
        var proxyRequest = new HttpRequestMessage();
        var destinationPrefix = "http://destinationhost:9090/path";

        await results.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix);

        Assert.Equal("CustomHost", proxyRequest.Headers.Host);
    }

    [Fact]
    public void DefaultsCanBeOverridenByForwarded()
    {
        var transformBuilder = CreateTransformBuilder();
        var transforms = new[]
        {
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {  "RequestHeadersCopy", "false" }
            },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {  "Forwarded", "proto" }
            },
        };

        var route = new RouteConfig() { Transforms = transforms };
        var errors = transformBuilder.ValidateRoute(route);
        Assert.Empty(errors);

        var results = transformBuilder.BuildInternal(route, new ClusterConfig());
        Assert.Equal(5, results.RequestTransforms.Length);
        Assert.All(
            results.RequestTransforms.Skip(1).Select(t => (dynamic)t),
            t =>
            {
                Assert.StartsWith("X-Forwarded-", t.HeaderName);
                Assert.Equal(ForwardedTransformActions.Remove, t.TransformAction);
            });
        var transform = results.RequestTransforms[0];
        var forwardedTransform = Assert.IsType<RequestHeaderForwardedTransform>(transform);
        Assert.True(forwardedTransform.ProtoEnabled);
    }

    private static TransformBuilder CreateTransformBuilder()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddReverseProxy();
        using var services = serviceCollection.BuildServiceProvider();
        return (TransformBuilder)services.GetRequiredService<ITransformBuilder>();
    }

    private class TestTransformFactory : ITransformFactory
    {
        private readonly string _v;

        public int ValidationCalls { get; set; }
        public int BuildCalls { get; set; }

        public TestTransformFactory(string v)
        {
            _v = v;
        }

        public bool Validate(TransformRouteValidationContext context, IReadOnlyDictionary<string, string> transformValues)
        {
            Assert.NotNull(context.Services);
            Assert.NotNull(context.Route);
            Assert.NotNull(context.Errors);
            ValidationCalls++;
            return transformValues.TryGetValue(_v, out var _);
        }

        public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
        {
            Assert.NotNull(context.Services);
            Assert.NotNull(context.Route);
            BuildCalls++;
            if (transformValues.TryGetValue(_v, out var _))
            {
                context.AddResponseTrailersTransform(context => default);
                return true;
            }

            return false;
        }
    }

    private class TestTransformProvider : ITransformProvider
    {
        public int ValidateRouteCalls { get; set; }
        public int ValidateClusterCalls { get; set; }
        public int ApplyCalls { get; set; }

        public void ValidateRoute(TransformRouteValidationContext context)
        {
            Assert.NotNull(context.Services);
            Assert.NotNull(context.Route);
            Assert.NotNull(context.Errors);
            ValidateRouteCalls++;
        }

        public void ValidateCluster(TransformClusterValidationContext context)
        {
            Assert.NotNull(context.Services);
            Assert.NotNull(context.Cluster);
            Assert.NotNull(context.Errors);
            ValidateClusterCalls++;
        }

        public void Apply(TransformBuilderContext context)
        {
            Assert.NotNull(context.Services);
            Assert.NotNull(context.Route);
            Assert.NotNull(context.Cluster);
            ApplyCalls++;
            context.AddResponseTrailer("key", "value");
        }
    }
}
