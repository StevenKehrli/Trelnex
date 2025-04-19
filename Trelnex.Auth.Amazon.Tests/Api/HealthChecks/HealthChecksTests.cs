using Snapshooter.NUnit;
using Trelnex.Core.Api.Tests;

namespace Trelnex.Core.Api.HealthChecks.Tests;

public class HealthChecksTests : BaseApiTests
{
    [Test]
    public void HealthChecks()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/healthz");
        var response = _httpClient.SendAsync(request).Result;

        var healthz = response.Content.ReadAsStringAsync().Result;

        Snapshot.Match(
            healthz,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    Assert.Multiple(() =>
                    {
                        // duration
                        Assert.That(
                            fieldOption.Field<string>("duration"),
                            Is.Not.Default);

                        // into[0].duration
                        Assert.That(
                            fieldOption.Field<string>("info[0].duration"),
                            Is.Not.Default);

                        // into[1].duration
                        Assert.That(
                            fieldOption.Field<string>("info[1].duration"),
                            Is.Not.Default);
                    });
                }));
    }
}
