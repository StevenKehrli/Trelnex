using Snapshooter.NUnit;
using Trelnex.Core.Api.Tests;

namespace Trelnex.Core.Api.Swagger.Tests;

public class SwaggerTests : BaseApiTests
{
    [Test]
    public void Swagger()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/swagger/v1/swagger.json");
        var response = _httpClient.SendAsync(request).Result;

        var swagger = response.Content.ReadAsStringAsync().Result;

        Snapshot.Match(swagger);
    }
}
