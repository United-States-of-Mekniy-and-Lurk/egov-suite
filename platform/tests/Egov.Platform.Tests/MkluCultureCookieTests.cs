using Egov.Platform.Localization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace Egov.Platform.Tests;

public sealed class MkluCultureCookieTests
{
    [Fact]
    public void SetCulture_OnMkluSubdomain_WritesSharedSecureCookie()
    {
        var context = CreateContext("portal.mklu.org");

        new MkluCultureCookie(new MkluLocalizationOptions()).SetCulture(context, "cs");

        var setCookie = context.Response.Headers.SetCookie.ToString();
        setCookie.Should().Contain("mklu.culture=c%3Dcs%7Cuic%3Dcs");
        setCookie.Should().Contain("domain=.mklu.org");
        setCookie.Should().Contain("path=/");
        setCookie.Should().Contain("secure");
        setCookie.Should().Contain("httponly");
        setCookie.Should().Contain("samesite=lax");
    }

    [Fact]
    public void SetCulture_OnLocalhost_WritesHostOnlyCookie()
    {
        var context = CreateContext("localhost");

        new MkluCultureCookie(new MkluLocalizationOptions()).SetCulture(context, "cs");

        context.Response.Headers.SetCookie.ToString().Should().NotContain("domain=");
    }

    [Fact]
    public void SetCulture_WithUnsupportedCulture_UsesDefaultCulture()
    {
        var context = CreateContext("portal.mklu.org");

        var selectedCulture = new MkluCultureCookie(new MkluLocalizationOptions())
            .SetCulture(context, "de");

        selectedCulture.Should().Be("en");
        context.Response.Headers.SetCookie.ToString().Should().Contain("c%3Den%7Cuic%3Den");
    }

    private static DefaultHttpContext CreateContext(string host)
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);
        return context;
    }
}