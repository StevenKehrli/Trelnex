using Snapshooter.NUnit;
using Trelnex.Core.Api.Tests;

namespace Trelnex.Core.Api.Swagger.Tests;

[Category("Swagger")]
public class SwaggerTests : BaseApiTests
{
    [Test]
    [Description("Tests that the swagger endpoint returns a valid swagger.json")]
    public async Task Swagger_ReturnsValidJson()
    {
        // Create a request to get the swagger.json file
        var request = new HttpRequestMessage(HttpMethod.Get, "/swagger/v1/swagger.json");

        // Send the request to the swagger endpoint
        var response = await _httpClient.SendAsync(request);
        Assert.That(response, Is.Not.Null);

        // Read the content of the response and verify it's not empty
        var swagger = await response.Content.ReadAsStringAsync();
        Assert.That(string.IsNullOrEmpty(swagger), Is.False);

        // Match the swagger json with a snapshot
        Snapshot.Match(swagger);
    }
}
