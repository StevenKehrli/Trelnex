using Trelnex.Core.Identity;

namespace Trelnex.Core.Client;

/// <summary>
/// The client factory interface.
/// </summary>
public interface IClientFactory<IClient>
{
    /// <summary>
    /// The name of the client.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Create a new instance of the client.
    /// </summary>
    /// <param name="httpClient">The http client.</param>
    /// <param name="accessTokenProvider">The access token provider.</param>
    /// <returns>The client.</returns>
    IClient Create(
        HttpClient httpClient,
        IAccessTokenProvider? accessTokenProvider);
}
