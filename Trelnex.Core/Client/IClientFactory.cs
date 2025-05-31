using Trelnex.Core.Identity;

namespace Trelnex.Core.Client;

/// <summary>
/// Factory interface for creating API client instances.
/// </summary>
/// <typeparam name="IClient">The client interface type to be created by this factory.</typeparam>
/// <remarks>
/// Enables dependency injection and abstraction of client creation logic.
/// </remarks>
public interface IClientFactory<IClient>
{
    /// <summary>
    /// Gets the unique identifier for this client factory.
    /// </summary>
    /// <remarks>
    /// Used for registration and retrieval in dependency injection systems.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Creates a new instance of the API client.
    /// </summary>
    /// <param name="httpClient">The configured HTTP client to use for requests.</param>
    /// <param name="accessTokenProvider">Optional provider for authentication tokens.</param>
    /// <returns>A fully configured client implementation.</returns>
    IClient Create(
        HttpClient httpClient,
        IAccessTokenProvider? accessTokenProvider);
}
