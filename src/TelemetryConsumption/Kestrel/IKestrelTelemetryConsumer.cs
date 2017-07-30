// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Yarp.Telemetry.Consumption;

/// <summary>
/// A consumer of Microsoft-AspNetCore-Server-Kestrel EventSource events.
/// </summary>
public interface IKestrelTelemetryConsumer
{
    /// <summary>
    /// Called at the start of a request.
    /// </summary>
    /// <param name="timestamp">Timestamp when the event was fired.</param>
    /// <param name="connectionId">ID of the connection.</param>
    /// <param name="requestId">ID of the request.</param>
    /// <param name="httpVersion">HTTP version of the request.</param>
    /// <param name="path">Path of the request.</param>
    /// <param name="method">HTTP method of the request.</param>
    void OnRequestStart(DateTime timestamp, string connectionId, string requestId, string httpVersion, string path, string method) { }

    /// <summary>
    /// Called at the end of a request.
    /// </summary>
    /// <param name="timestamp">Timestamp when the event was fired.</param>
    /// <param name="connectionId">ID of the connection.</param>
    /// <param name="requestId">ID of the request.</param>
    /// <param name="httpVersion">HTTP version of the request.</param>
    /// <param name="path">Path of the request.</param>
    /// <param name="method">HTTP method of the request.</param>
    void OnRequestStop(DateTime timestamp, string connectionId, string requestId, string httpVersion, string path, string method) { }
}
