using System.Net;
using Trelnex.Core.Api.Tests;

namespace Trelnex.Core.Api.Exceptions.Tests;

[Category("Exceptions")]
public class ExceptionsTests : BaseApiTests
{
    [Test]
    [Description("Tests that the /exception endpoint returns a BadRequest status code when no authorization header is set")]
    public async Task Exception_BadRequest_NoAuthorization()
    {
        // Create a request to the /exception endpoint
        var request = new HttpRequestMessage(HttpMethod.Get, "/exception");

        // Send the request to the /exception endpoint
        var response = await _httpClient.SendAsync(request);
        Assert.That(response, Is.Not.Null);

        // Verify that the response status code is BadRequest (400) because no authorization header is set
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
