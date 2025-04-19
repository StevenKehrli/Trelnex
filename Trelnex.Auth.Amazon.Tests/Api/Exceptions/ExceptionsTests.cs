using System.Net;
using Trelnex.Core.Api.Tests;

namespace Trelnex.Core.Api.Exceptions.Tests;

public class ExceptionsTests : BaseApiTests
{
    [Test]
    public void ThrowsException()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/exception");
        var response = _httpClient.SendAsync(request).Result;

        // this should fail because we have not set the authorization header
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
