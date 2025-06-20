using Snapshooter.NUnit;
using Trelnex.Core.Api.Tests;

namespace Trelnex.Core.Api.HealthChecks.Tests;

[Category("HealthChecks")]
public class HealthChecksTests : BaseApiTests
{
    [Test]
    [Description("Tests that the healthz endpoint returns a valid response")]
    public async Task HealthChecks_ReturnsValidResponse()
    {
        // Create a request to get the healthz endpoint
        var request = new HttpRequestMessage(HttpMethod.Get, "/healthz");

        // Send the request to the healthz endpoint
        using var httpClient = CreateAnonymousHttpClient();
        var response = await httpClient.SendAsync(request);
        Assert.That(response, Is.Not.Null);

        // Read the content of the response
        var healthz = await response.Content.ReadAsStringAsync();
        Assert.That(string.IsNullOrEmpty(healthz), Is.False);

        // Match the healthz response with a snapshot
        Snapshot.Match(
            healthz,
            matchOptions => matchOptions
                .Assert(fieldOption =>
                {
                    using (Assert.EnterMultipleScope())
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
                    }
                }));
    }
}
