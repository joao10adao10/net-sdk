// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit;

namespace Yarp.ReverseProxy.Configuration.Tests;

public class RouteHeaderTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Equals_Positive(bool isCaseSensitive)
    {
        var a = new RouteHeader()
        {
            Name = "foo",
            Mode = HeaderMatchMode.Exists,
            Values = new[] { "v1", "v2" },
            IsCaseSensitive = isCaseSensitive,
        };
        var b = new RouteHeader()
        {
            Name = "Foo",
            Mode = HeaderMatchMode.Exists,
            Values = new[] { "v1", "v2" },
            IsCaseSensitive = isCaseSensitive,
        };
        var c = a with { }; // Clone
        Assert.True(a.Equals(b));
        Assert.True(a.Equals(c));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.Equal(a.GetHashCode(), c.GetHashCode());
    }

    [Fact]
    public void Equals_Negative()
    {
        var a = new RouteHeader()
        {
            Name = "foo",
            Mode = HeaderMatchMode.Exists,
            Values = new[] { "v1", "v2" },
            IsCaseSensitive = true,
        };
        var b = a with { Name = "bar" };
        var c = a with { Mode = HeaderMatchMode.ExactHeader };
        var d = a with { Values = new[] { "v1", "v3" } };
        var e = a with { IsCaseSensitive = false };
        Assert.False(a.Equals(b));
        Assert.False(a.Equals(c));
        Assert.False(a.Equals(d));
        Assert.False(a.Equals(e));
    }

    [Fact]
    public void Equals_Null_False()
    {
        Assert.False(new RouteHeader().Equals(null));
    }
}
