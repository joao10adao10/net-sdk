// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Xunit;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Yarp.ReverseProxy.Transforms.Tests;

public class RequestHeadersTransformExtensionsTests : TransformExtentionsTestsBase
{
    private readonly RequestHeadersTransformFactory _factory = new();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithTransformCopyRequestHeaders(bool copy)
    {
        var routeConfig = new RouteConfig();
        routeConfig = routeConfig.WithTransformCopyRequestHeaders(copy);

        var builderContext = ValidateAndBuild(routeConfig, _factory);

        Assert.Equal(copy, builderContext.CopyRequestHeaders);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithTransformUseOriginalHostHeader(bool useOriginal)
    {
        var routeConfig = new RouteConfig();
        routeConfig = routeConfig.WithTransformUseOriginalHostHeader(useOriginal);

        var builderContext = ValidateAndBuild(routeConfig, _factory);

        var transform = Assert.Single(builderContext.RequestTransforms);
        var hostTransform = Assert.IsType<RequestHeaderOriginalHostTransform>(transform);

        Assert.Equal(useOriginal, hostTransform.UseOriginalHost);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithTransformRequestHeader(bool append)
    {
        var routeConfig = new RouteConfig();
        routeConfig = routeConfig.WithTransformRequestHeader("name", "value", append);

        var builderContext = ValidateAndBuild(routeConfig, _factory);

        ValidateRequestHeader(append, builderContext);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddRequestHeader(bool append)
    {
        var builderContext = CreateBuilderContext();
        builderContext.AddRequestHeader("name", "value", append);

        ValidateRequestHeader(append, builderContext);
    }

    [Fact]
    public void WithTransformRequestHeaderRemove()
    {
        var routeConfig = new RouteConfig();
        routeConfig = routeConfig.WithTransformRequestHeaderRemove("MyHeader");

        var builderContext = ValidateAndBuild(routeConfig, _factory);
        var transform = Assert.Single(builderContext.RequestTransforms) as RequestHeaderRemoveTransform;
        Assert.Equal("MyHeader", transform.HeaderName);
    }

    [Fact]
    public void AddRequestHeaderRemove()
    {
        var builderContext = CreateBuilderContext();
        builderContext.AddRequestHeaderRemove("MyHeader");

        var transform = Assert.Single(builderContext.RequestTransforms) as RequestHeaderRemoveTransform;
        Assert.Equal("MyHeader", transform.HeaderName);
    }

    [Fact]
    public void WithTransformRequestHeadersAllowed()
    {
        var routeConfig = new RouteConfig();
        routeConfig = routeConfig.WithTransformRequestHeadersAllowed("header1", "Header2");

        var builderContext = ValidateAndBuild(routeConfig, _factory);
        var transform = Assert.Single(builderContext.RequestTransforms) as RequestHeadersAllowedTransform;
        Assert.Equal(new[] { "header1", "Header2" }, transform.AllowedHeaders);
        Assert.False(builderContext.CopyRequestHeaders);
    }

    [Fact]
    public void AddRequestHeadersAllowed()
    {
        var builderContext = CreateBuilderContext();
        builderContext.AddRequestHeadersAllowed("header1", "Header2");

        var transform = Assert.Single(builderContext.RequestTransforms) as RequestHeadersAllowedTransform;
        Assert.Equal(new[] { "header1", "Header2" }, transform.AllowedHeaders);
        Assert.False(builderContext.CopyRequestHeaders);
    }

    private static void ValidateRequestHeader(bool append, TransformBuilderContext builderContext)
    {
        var requestHeaderValueTransform = Assert.Single(builderContext.RequestTransforms.OfType<RequestHeaderValueTransform>().Where(x => x.HeaderName == "name"));
        Assert.Equal("value", requestHeaderValueTransform.Value);
        Assert.Equal(append, requestHeaderValueTransform.Append);
    }
}
