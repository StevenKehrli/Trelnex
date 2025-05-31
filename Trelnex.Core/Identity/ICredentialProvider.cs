namespace Trelnex.Core.Identity;

/// <summary>
/// Defines a provider for accessing authentication credentials and token providers.
/// </summary>
/// <remarks>
/// Serves as a factory and manager for credentials and access token providers.
/// </remarks>
public interface ICredentialProvider
{
    /// <summary>
    /// Gets the unique identifier for this credential provider.
    /// </summary>
    /// <remarks>
    /// Used for logging and distinguishing between multiple credential providers.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Creates or retrieves an access token provider for the specified scope.
    /// </summary>
    /// <param name="scope">The scope or resource identifier for which to obtain tokens.</param>
    /// <returns>An access token provider configured for the specified scope.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the scope is invalid or not supported by this provider.
    /// </exception>
    IAccessTokenProvider GetAccessTokenProvider(
        string scope);

    /// <summary>
    /// Retrieves diagnostic information about the health of all credentials managed by this provider.
    /// </summary>
    /// <returns>Status information for all credentials and their associated tokens.</returns>
    /// <remarks>
    /// Used for monitoring and diagnostics to assess the health of authentication mechanisms.
    /// </remarks>
    CredentialStatus GetStatus();
}

/// <summary>
/// Generic credential provider interface for accessing specific credential types.
/// </summary>
/// <typeparam name="TCredential">The specific credential type managed by this provider.</typeparam>
/// <remarks>
/// Extends the base credential provider interface with type-specific credential access.
/// </remarks>
public interface ICredentialProvider<TCredential> : ICredentialProvider
{
    /// <summary>
    /// Retrieves the raw credential object of the specified type.
    /// </summary>
    /// <returns>The credential object for direct use with platform-specific APIs.</returns>
    /// <exception cref="AccessTokenUnavailableException">
    /// Thrown when the underlying credential cannot be accessed or is invalid.
    /// </exception>
    TCredential GetCredential();
}
