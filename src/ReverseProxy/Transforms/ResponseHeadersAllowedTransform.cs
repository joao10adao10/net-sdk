// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Yarp.ReverseProxy.Forwarder;

namespace Yarp.ReverseProxy.Transforms;

/// <summary>
/// Copies only allowed response headers.
/// </summary>
public class ResponseHeadersAllowedTransform : ResponseTransform
{
    public ResponseHeadersAllowedTransform(string[] allowedHeaders)
    {
        if (allowedHeaders is null)
        {
            throw new ArgumentNullException(nameof(allowedHeaders));
        }

        AllowedHeaders = allowedHeaders;
        AllowedHeadersSet = new HashSet<string>(allowedHeaders, StringComparer.OrdinalIgnoreCase);
    }

    internal string[] AllowedHeaders { get; }

    private HashSet<string> AllowedHeadersSet { get; }

    /// <inheritdoc/>
    public override ValueTask ApplyAsync(ResponseTransformContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.ProxyResponse is null)
        {
            return default;
        }

        Debug.Assert(!context.HeadersCopied);

        // See https://github.com/microsoft/reverse-proxy/blob/51d797986b1fea03500a1ad173d13a1176fb5552/src/ReverseProxy/Forwarder/HttpTransformer.cs#L67-L77
        var responseHeaders = context.HttpContext.Response.Headers;
        CopyResponseHeaders(context.ProxyResponse.Headers, responseHeaders);
        if (context.ProxyResponse.Content is not null)
        {
            CopyResponseHeaders(context.ProxyResponse.Content.Headers, responseHeaders);
        }

        context.HeadersCopied = true;

        return default;
    }

    // See https://github.com/microsoft/reverse-proxy/blob/main/src/ReverseProxy/Forwarder/HttpTransformer.cs#:~:text=void-,CopyResponseHeaders
    private void CopyResponseHeaders(HttpHeaders source, IHeaderDictionary destination)
    {
        foreach (var header in source.NonValidated)
        {
            var headerName = header.Key;
            if (!AllowedHeadersSet.Contains(headerName))
            {
                continue;
            }

            destination[headerName] = RequestUtilities.Concat(destination[headerName], header.Value);
        }
    }
}
