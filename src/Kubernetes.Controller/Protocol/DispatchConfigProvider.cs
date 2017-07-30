using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Yarp.Kubernetes.Controller.Configuration;
using Yarp.Kubernetes.Controller.Dispatching;
using Yarp.Kubernetes.Protocol;
using Yarp.ReverseProxy.Configuration;

namespace Yarp.Kubernetes.Controller.Protocol;

public class DispatchConfigProvider : IUpdateConfig
{
    private readonly IDispatcher _dispatcher;

    public DispatchConfigProvider(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    public async Task UpdateAsync(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters, CancellationToken cancellationToken)
    {
        var message = new Message
        {
            MessageType = MessageType.Update,
            Key = string.Empty,
            Cluster = clusters.ToList(),
            Routes = routes.ToList(),
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(message);

        await _dispatcher.SendAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}
