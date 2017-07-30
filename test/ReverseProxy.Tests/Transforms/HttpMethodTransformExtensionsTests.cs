// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Xunit;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Yarp.ReverseProxy.Transforms.Tests;

public class HttpMethodTransformExtensionsTests : TransformExtentionsTestsBase
{
    private readonly HttpMethodTransformFactory _factory = new();

    [Fact]
    public void WithTransformHttpMethodChange()
    {
        var routeConfig = new RouteConfig();
        routeConfig = routeConfig.WithTransformHttpMethodChange(HttpMethods.Put, HttpMethods.Post);

        var builderContext = ValidateAndBuild(routeConfig, _factory);

        ValidateHttpMethod(builderContext);
    }

    [Fact]
    public void AddHttpMethodChange()
    {
        var builderContext = CreateBuilderContext();
        builderContext.AddHttpMethodChange(HttpMethods.Put, HttpMethods.Post);

        ValidateHttpMethod(builderContext);
    }

    private static void ValidateHttpMethod(TransformBuilderContext builderContext)
    {
        var requestTransform = Assert.Single(builderContext.RequestTransforms);
        var httpMethodTransform = Assert.IsType<HttpMethodChangeTransform>(requestTransform);
        Assert.Equal(HttpMethod.Put, httpMethodTransform.FromMethod);
        Assert.Equal(HttpMethod.Post, httpMethodTransform.ToMethod);
    }
}
