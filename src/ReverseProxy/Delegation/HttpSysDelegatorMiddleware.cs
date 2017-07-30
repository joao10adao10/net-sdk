// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Model;
using Yarp.ReverseProxy.Utilities;

namespace Yarp.ReverseProxy.Delegation;

internal sealed class HttpSysDelegatorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HttpSysDelegatorMiddleware> _logger;
    private readonly HttpSysDelegator _delegator;
    private readonly IRandomFactory _randomFactory;

    public HttpSysDelegatorMiddleware(
        RequestDelegate next,
        ILogger<HttpSysDelegatorMiddleware> logger,
        HttpSysDelegator delegator,
        IRandomFactory randomFactory)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _delegator = delegator ?? throw new ArgumentNullException(nameof(delegator));
        _randomFactory = randomFactory ?? throw new ArgumentNullException(nameof(randomFactory));
    }

    public Task Invoke(HttpContext context)
    {
        _ = context ?? throw new ArgumentNullException(nameof(context));
        var reverseProxyFeature = context.GetReverseProxyFeature();
        var destinations = reverseProxyFeature.AvailableDestinations
            ?? throw new InvalidOperationException($"The {nameof(IReverseProxyFeature)} Destinations collection was not set.");
        var cluster = reverseProxyFeature.Cluster
            ?? throw new InvalidOperationException($"The {nameof(IReverseProxyFeature)} Cluster was not set.");

        if (destinations.Any())
        {
            // This logic mimics behavior in ForwarderMiddleware, except we save the chosen destination back
            // to the proxy feature to ensure a delegation destination doesn't slip past this middleware.
            var destination = destinations[0];
            if (destinations.Count > 1)
            {
                var random = _randomFactory.CreateRandomInstance();
                Log.MultipleDestinationsAvailable(_logger, reverseProxyFeature.Cluster.Config.ClusterId);
                destination = destinations[random.Next(destinations.Count)];
                reverseProxyFeature.AvailableDestinations = destination;
            }

            if (destination.ShouldUseHttpSysDelegation())
            {
                reverseProxyFeature.ProxiedDestination = destination;

                _delegator.DelegateRequest(context, destination);

                return Task.CompletedTask;
            }
        }

        return _next(context);
    }

    private static class Log
    {
        private static readonly Action<ILogger, string, Exception?> _multipleDestinationsAvailable = LoggerMessage.Define<string>(
            LogLevel.Warning,
            EventIds.MultipleDestinationsAvailable,
            "More than one destination available for cluster '{clusterId}', load balancing may not be configured correctly. Choosing randomly.");

        public static void MultipleDestinationsAvailable(ILogger logger, string clusterId)
        {
            _multipleDestinationsAvailable(logger, clusterId, null);
        }
    }
}
