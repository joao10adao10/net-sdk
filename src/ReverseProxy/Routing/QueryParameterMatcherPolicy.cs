// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Yarp.ReverseProxy.Routing;

internal sealed class QueryParameterMatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, IEndpointSelectorPolicy
{
    /// <inheritdoc/>
    // Run after HttpMethodMatcherPolicy (-1000) and HostMatcherPolicy (-100), and HeaderMatcherPolicy (-50), but before default (0)
    public override int Order => -25;

    /// <inheritdoc/>
    public IComparer<Endpoint> Comparer => new QueryParameterMetadataEndpointComparer();

    /// <inheritdoc/>
    bool IEndpointSelectorPolicy.AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
    {
        _ = endpoints ?? throw new ArgumentNullException(nameof(endpoints));

        // When the node contains dynamic endpoints we can't make any assumptions.
        if (ContainsDynamicEndpoints(endpoints))
        {
            return true;
        }

        return AppliesToEndpointsCore(endpoints);
    }

    private static bool AppliesToEndpointsCore(IReadOnlyList<Endpoint> endpoints)
    {
        return endpoints.Any(e =>
        {
            var metadata = e.Metadata.GetMetadata<IQueryParameterMetadata>();
            return metadata?.Matchers?.Length > 0;
        });
    }

    /// <inheritdoc/>
    public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
    {
        _ = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
        _ = candidates ?? throw new ArgumentNullException(nameof(candidates));

        var query = httpContext.Request.Query;

        for (var i = 0; i < candidates.Count; i++)
        {
            if (!candidates.IsValidCandidate(i))
            {
                continue;
            }

            var matchers = candidates[i].Endpoint.Metadata.GetMetadata<IQueryParameterMetadata>()?.Matchers;

            if (matchers is null)
            {
                continue;
            }

            foreach (var matcher in matchers)
            {
                if (query.TryGetValue(matcher.Name, out var requestQueryParameterValues) &&
                    !StringValues.IsNullOrEmpty(requestQueryParameterValues))
                {
                    if (matcher.Mode is QueryParameterMatchMode.Exists)
                    {
                        continue;
                    }

                    if (TryMatch(matcher, requestQueryParameterValues))
                    {
                        continue;
                    }
                }

                candidates.SetValidity(i, false);
                break;
            }
        }

        return Task.CompletedTask;
    }

    private static bool TryMatch(QueryParameterMatcher matcher, StringValues requestHeaderValues)
    {
        var requestHeaderCount = requestHeaderValues.Count;

        for (var i = 0; i < requestHeaderCount; i++)
        {
            var requestValue = requestHeaderValues[i];
            if (requestValue is null)
            {
                continue;
            }

            foreach (var expectedValue in matcher.Values)
            {
                if (TryMatch(matcher, requestValue, expectedValue))
                {
                    return matcher.Mode != QueryParameterMatchMode.NotContains;
                }
            }
        }

        return matcher.Mode == QueryParameterMatchMode.NotContains;

        static bool TryMatch(QueryParameterMatcher matcher, string queryValue, string expectedValue)
        {
            return matcher.Mode switch
            {
                QueryParameterMatchMode.Exact => queryValue.Equals(expectedValue, matcher.Comparison),
                QueryParameterMatchMode.Prefix => queryValue.StartsWith(expectedValue, matcher.Comparison),
                _ => queryValue.Contains(expectedValue, matcher.Comparison)
            };
        }
    }

    private class QueryParameterMetadataEndpointComparer : EndpointMetadataComparer<IQueryParameterMetadata>
    {
        protected override int CompareMetadata(IQueryParameterMetadata? x, IQueryParameterMetadata? y)
        {
            return (y?.Matchers?.Length ?? 0).CompareTo(x?.Matchers?.Length ?? 0);
        }
    }
}
