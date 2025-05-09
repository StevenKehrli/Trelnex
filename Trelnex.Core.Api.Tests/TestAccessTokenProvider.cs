using Trelnex.Core.Identity;

namespace Trelnex.Core.Api.Tests;

/// <summary>
/// A test implementation of the <see cref="IAccessTokenProvider"/> interface.
/// This class is used in tests to provide a fixed access token for a given scope,
/// allowing tests to simulate authenticated requests without needing a real authentication server.
/// </summary>
internal class TestAccessTokenProvider(
    string scope,
    AccessToken accessToken)
    : IAccessTokenProvider
{
    /// <summary>
    /// Gets the scope of the access token.
    /// </summary>
    public string Scope => scope;

    /// <summary>
    /// Gets the access token.
    /// </summary>
    /// <returns>The access token.</returns>
    public AccessToken GetAccessToken() => accessToken;
}
