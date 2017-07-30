// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using k8s;
using k8s.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yarp.Kubernetes.Tests.TestCluster;
using Yarp.Kubernetes.Tests.Utils;

namespace Yarp.Kubernetes.Controller.Client.Tests;

public class ResourceInformerTests
{
    [Fact]
    public async Task ResourcesAreListedWhenReadyAsyncIsComplete()
    {
        using var cancellation = new CancellationTokenSource(Debugger.IsAttached ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(5));

        var testYaml = TestYaml.LoadFromEmbeddedStream<(V1Pod[] pods, NamespacedName[] shouldBe)>();

        using var clusterHost = new TestClusterHostBuilder()
            .UseInitialResources(testYaml.pods)
            .Build();

        using var testHost = new HostBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddKubernetesReverseProxy(context.Configuration);
                services.RegisterResourceInformer<V1Pod, V1PodResourceInformer>();
                services.Configure<KubernetesClientOptions>(options =>
                {
                    options.Configuration = KubernetesClientConfiguration.BuildConfigFromConfigObject(clusterHost.KubeConfig);
                });
            })
            .Build();

        var informer = testHost.Services.GetRequiredService<IResourceInformer<V1Pod>>();
        var pods = new Dictionary<NamespacedName, V1Pod>();

        informer.StartWatching();
        using var registration = informer.Register((eventType, pod) =>
        {
            pods[NamespacedName.From(pod)] = pod;
        });

        await clusterHost.StartAsync(cancellation.Token).ConfigureAwait(false);
        await testHost.StartAsync(cancellation.Token).ConfigureAwait(false);

        await registration.ReadyAsync(cancellation.Token).ConfigureAwait(false);

        Assert.Equal(testYaml.shouldBe, pods.Keys);
    }

    [Fact]
    public async Task ResourcesWithApiGroupAreListed()
    {
        using var cancellation = new CancellationTokenSource(Debugger.IsAttached ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(5));

        var testYaml = TestYaml.LoadFromEmbeddedStream<(V1Deployment[] deployments, NamespacedName[] shouldBe)>();

        using var clusterHost = new TestClusterHostBuilder()
            .UseInitialResources(testYaml.deployments)
            .Build();

        using var testHost = new HostBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddKubernetesReverseProxy(context.Configuration);
                services.RegisterResourceInformer<V1Deployment, V1DeploymentResourceInformer>();
                services.Configure<KubernetesClientOptions>(options =>
                {
                    options.Configuration = KubernetesClientConfiguration.BuildConfigFromConfigObject(clusterHost.KubeConfig);
                });
            })
            .Build();

        var informer = testHost.Services.GetRequiredService<IResourceInformer<V1Deployment>>();
        var deployments = new Dictionary<NamespacedName, V1Deployment>();

        informer.StartWatching();
        using var registration = informer.Register((eventType, deployment) =>
        {
            deployments[NamespacedName.From(deployment)] = deployment;
        });

        await clusterHost.StartAsync(cancellation.Token).ConfigureAwait(false);
        await testHost.StartAsync(cancellation.Token).ConfigureAwait(false);

        await registration.ReadyAsync(cancellation.Token).ConfigureAwait(false);

        Assert.Equal(testYaml.shouldBe, deployments.Keys);
    }
}
