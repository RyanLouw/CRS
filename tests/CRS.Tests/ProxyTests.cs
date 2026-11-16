using Microsoft.AspNetCore.Builder;
using System.Net;
using System.Text.RegularExpressions;
using CRS.Core.Models;
using CRS.Core.Models.DTO;
using CRS.Core.Models.ViewModels;
using CRS.Tests.Fakes;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CRS.Tests;

public sealed class ProxyTests
{
    [Theory]
    [InlineData("10.0.0.5", "198.51.100.8")]
    [InlineData("10.0.0.99", "10.0.0.99")]
    public async Task AcceptsForwardedClientIpOnlyFromTrustedProxyAndPreservesHostIp(string remoteIp, string expectedClientIp)
    {
        var manager = A.Fake<ICallManager>();
        var date = new DateOnly(2026, 11, 13);
        A.CallTo(() => manager.GetCallsAsync(A<Guid>._, A<DateOnly>._, A<CancellationToken>._))
            .Returns(new CallListPage(new(DemoServices.OperatorId, "330", "Test", null), date, [], new([], [], [], [])));
        A.CallTo(() => manager.SaveAsync(A<Guid>._, A<RatingInput>._, A<string?>._, A<string?>._, A<CancellationToken>._))
            .Returns(new SaveResult(true, true, "Saved"));
        using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<ICallManager>();
            services.AddSingleton(manager);
            services.Configure<ForwardedHeadersOptions>(options => options.KnownProxies.Add(IPAddress.Parse("10.0.0.5")));
        }, initializeRequest: context => context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "198.51.100.8");
        var page = await client.GetStringAsync("/CallList");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        using var response = await client.PostAsync("/CallList/Rate", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token), ["Input.CallId"] = "call-1",
            ["Input.Date"] = "2026-11-13", ["Input.ProblemIds"] = "1", ["Input.PharmacyId"] = "1"
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        A.CallTo(() => manager.SaveAsync(DemoServices.OperatorId, A<RatingInput>._, expectedClientIp, remoteIp, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
}
