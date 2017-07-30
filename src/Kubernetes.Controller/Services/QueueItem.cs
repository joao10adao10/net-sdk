// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Yarp.Kubernetes.Controller.Services;

/// <summary>
/// QueueItem acts as the "Key" for the _queue to manage items.
/// </summary>
public struct QueueItem : IEquatable<QueueItem>
{
    public QueueItem(string change)
    {
        Change = change;
    }

    /// <summary>
    /// This identifies that a change has occured and either configuration requires to be rebuilt, or needs to be dispatched.
    /// </summary>
    public string Change { get; }

    public override bool Equals(object obj)
    {
        return obj is QueueItem item && Equals(item);
    }

    public bool Equals(QueueItem other)
    {
        return Change.Equals(other.Change, StringComparison.Ordinal);
    }

    public override int GetHashCode()
    {
#pragma warning disable CA1307 // Specify StringComparison
        return Change.GetHashCode();
#pragma warning restore CA1307 // Specify StringComparison
    }

    public static bool operator ==(QueueItem left, QueueItem right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(QueueItem left, QueueItem right)
    {
        return !(left == right);
    }
}
