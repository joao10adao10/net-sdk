// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;

namespace Yarp.ReverseProxy.Forwarder;

/// <summary>
/// Removes existing headers and then delegates to the inner propagator.
/// </summary>
public sealed class ReverseProxyPropagator : DistributedContextPropagator
{
    private readonly DistributedContextPropagator _innerPropagator;
    private readonly string[] _headersToRemove;

    /// <summary>
    /// ReverseProxyPropagator removes headers pointed out in innerPropagator.
    /// </summary>
    public ReverseProxyPropagator(DistributedContextPropagator innerPropagator)
    {
        _innerPropagator = innerPropagator ?? throw new ArgumentNullException(nameof(innerPropagator));
        _headersToRemove = _innerPropagator.Fields.ToArray();
    }

    public override void Inject(Activity? activity, object? carrier, PropagatorSetterCallback? setter)
    {
        if (carrier is HttpRequestMessage message)
        {
            var headers = message.Headers;

            foreach (var header in _headersToRemove)
            {
                headers.Remove(header);
            }
        }

        _innerPropagator.Inject(activity, carrier, setter);
    }

    public override void ExtractTraceIdAndState(object? carrier, PropagatorGetterCallback? getter, out string? traceId, out string? traceState) =>
        _innerPropagator.ExtractTraceIdAndState(carrier, getter, out traceId, out traceState);

    public override IEnumerable<KeyValuePair<string, string?>>? ExtractBaggage(object? carrier, PropagatorGetterCallback? getter) =>
        _innerPropagator.ExtractBaggage(carrier, getter);

    public override IReadOnlyCollection<string> Fields => _innerPropagator.Fields;
}
