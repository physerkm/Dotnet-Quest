using System.Net;
using DotnetQuest.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DotnetQuest.Api.Tests;

/// <summary>
/// Render'daki gibi Cloudflare + Render proxy'si arkasından gelen bir isteği, uygulamanın kendi ayarlarıyla
/// ForwardedHeaders middleware'inden geçirir. Rate limit IP'ye göre bölümlediği için doğru IP kritik.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ForwardedHeadersTests(ApiFactory factory)
{
    private static readonly IPAddress RenderProxy = IPAddress.Parse("10.21.157.68");

    [Fact]
    public async Task ClientIp_ComesFromCloudflareHeader_NotFromXForwardedFor()
    {
        var context = await ProcessAsync(new()
        {
            ["CF-Connecting-IP"] = "198.51.100.7",
            // En soldaki değeri istemci kendisi uydurdu, en sağdaki ise Cloudflare sunucusunun IP'si.
            ["X-Forwarded-For"] = "1.2.3.4, 198.51.100.7, 104.22.17.40",
            ["X-Forwarded-Proto"] = "https",
        });

        Assert.Equal(IPAddress.Parse("198.51.100.7"), context.Connection.RemoteIpAddress);
        Assert.Equal("https", context.Request.Scheme);
    }

    [Fact]
    public async Task WithoutCloudflareHeader_XForwardedForIsIgnored()
    {
        var context = await ProcessAsync(new() { ["X-Forwarded-For"] = "1.2.3.4" });

        Assert.Equal(RenderProxy, context.Connection.RemoteIpAddress);
    }

    private async Task<HttpContext> ProcessAsync(Dictionary<string, string> headers)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = RenderProxy;
        foreach (var (name, value) in headers)
        {
            context.Request.Headers[name] = value;
        }

        var options = factory.Services.GetRequiredService<IOptions<ForwardedHeadersOptions>>();
        var middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask, NullLoggerFactory.Instance, options);
        await middleware.Invoke(context);

        return context;
    }
}
