using Trelnex.Core.Client;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Api.Tests;

internal class TestClient1(
    HttpClient httpClient,
    IAccessTokenProvider accessTokenProvider)
    : BaseClient(httpClient, accessTokenProvider)
{
    public async Task<TestResponse> Delete()
    {
        return await Delete<TestResponse>(
            uri: BaseAddress.AppendPath("/delete1"));
    }

    public async Task<TestResponse> Get()
    {
        return await Get<TestResponse>(
            uri: BaseAddress.AppendPath("/get1"));
    }

    public async Task<TestResponse> Patch()
    {
        return await Patch<string, TestResponse>(
            uri: BaseAddress.AppendPath("/patch1"),
            content: null);
    }

    public async Task<TestResponse> Post()
    {
        return await Post<string, TestResponse>(
            uri: BaseAddress.AppendPath("/post1"),
            content: null);
    }

    public async Task<TestResponse> Put()
    {
        return await Put<string, TestResponse>(
            uri: BaseAddress.AppendPath("/put1"),
            content: null);
    }

    public async Task<TestResponse> QueryString(
        string value)
    {
        return await Get<TestResponse>(
            uri: BaseAddress.AppendPath("/queryString").AddQueryString("value", value));
    }
}
