// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace Yarp.Kubernetes.Controller.Services;

/// <summary>
/// IReconciler is a service interface called by the <see cref="IngressController"/> to process
/// the work items as they are dequeued.
/// </summary>
public interface IReconciler
{
    Task ProcessAsync(CancellationToken cancellationToken);
}
