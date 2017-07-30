// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Yarp.Kubernetes.Controller.Configuration;

internal class KubernetesConfigProvider : IProxyConfigProvider, IUpdateConfig
{
    private volatile MessageConfig _config;

    public KubernetesConfigProvider()
    {
        _config = new MessageConfig(null, null);
    }

    public IProxyConfig GetConfig() => _config;

    public Task UpdateAsync(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters, CancellationToken cancellationToken)
    {
        var newConfig = new MessageConfig(routes, clusters);
        var oldConfig = Interlocked.Exchange(ref _config, newConfig);
        oldConfig.SignalChange();

        return Task.CompletedTask;
    }

#pragma warning disable CA1001 // Types that own disposable fields should be disposable
    private class MessageConfig : IProxyConfig
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public MessageConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
            : this(routes, clusters, Guid.NewGuid().ToString())
        { }

        public MessageConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters, string revisionId)
        {
            RevisionId = revisionId ?? throw new ArgumentNullException(nameof(revisionId));
            Routes = routes;
            Clusters = clusters;
            ChangeToken = new CancellationChangeToken(_cts.Token);
        }

        public string RevisionId { get; }

        public IReadOnlyList<RouteConfig> Routes { get; }

        public IReadOnlyList<ClusterConfig> Clusters { get; }

        public IChangeToken ChangeToken { get; }

        internal void SignalChange()
        {
            _cts.Cancel();
        }
    }
}
