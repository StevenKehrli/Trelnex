using Trelnex.Core.Identity;

namespace Trelnex.Core.Api.Tests;

internal class TestAccessTokenProvider(
    string scope,
    AccessToken accessToken)
    : IAccessTokenProvider
{
    public string Scope => scope;

    public AccessToken GetAccessToken() => accessToken;
}
