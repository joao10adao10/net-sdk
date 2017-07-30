// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using k8s.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Yarp.Kubernetes.Tests.TestCluster.Models;

namespace Yarp.Kubernetes.Tests.TestCluster;

[Route("apis/{group}/{version}/{plural}")]
public class ResourceApiGroupController : ControllerBase
{
    private readonly ITestCluster _testCluster;

    public ResourceApiGroupController(ITestCluster testCluster)
    {
        _testCluster = testCluster;
    }

    [FromRoute]
    public string Group { get; set; }

    [FromRoute]
    public string Version { get; set; }

    [FromRoute]
    public string Plural { get; set; }

    [HttpGet]
    public async Task<IActionResult> ListAsync(ListParameters parameters)
    {
        var list = await _testCluster.ListResourcesAsync(Group, Version, Plural, parameters);

        var result = new KubernetesList<ResourceObject>(
            apiVersion: $"{Group}/{Version}",
            kind: "DeploymentList",
            metadata: new V1ListMeta(
                continueProperty: list.Continue,
                remainingItemCount: null,
                resourceVersion: list.ResourceVersion),
            items: list.Items);

        return new ObjectResult(result);
    }
}
