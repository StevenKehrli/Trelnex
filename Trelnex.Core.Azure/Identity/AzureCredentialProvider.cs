using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Azure.Identity;

/// <summary>
/// Provides Azure credentials and access tokens for Azure-based authentication.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="ICredentialProvider{T}"/> for <see cref="TokenCredential"/>,
/// serving as the entry point for Azure credential and token management.
/// </para>
/// <para>
/// Wraps a <see cref="ManagedCredential"/> instance that handles token caching,
/// automatic refresh, and Azure SDK integration.
/// </para>
/// </remarks>
public class AzureCredentialProvider : ICredentialProvider<TokenCredential>
{
    #region Private Fields

    /// <summary>
    /// The managed credential that wraps the underlying <see cref="TokenCredential"/>.
    /// </summary>
    /// <remarks>
    /// Handles token caching, refresh, and status reporting.
    /// </remarks>
    private readonly ManagedCredential _managedCredential;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureCredentialProvider"/> class.
    /// </summary>
    /// <param name="managedCredential">The managed credential instance.</param>
    private AzureCredentialProvider(
        ManagedCredential managedCredential)
    {
        _managedCredential = managedCredential;
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates a new <see cref="AzureCredentialProvider"/> instance with initialized credentials.
    /// </summary>
    /// <param name="credentialOptions">Configuration options for which credential sources to use.</param>
    /// <param name="logger">The logger for recording operations.</param>
    /// <returns>A fully initialized <see cref="AzureCredentialProvider"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="credentialOptions.Sources"/> is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an unsupported <see cref="CredentialSource"/> is specified.</exception>
    /// <remarks>
    /// <para>
    /// Creates a <see cref="ChainedTokenCredential"/> from the credential sources in <paramref name="credentialOptions.Sources"/>.
    /// Credential sources are tried in the order provided.
    /// </para>
    /// <para>
    /// This is the primary entry point for creating an Azure credential provider.
    /// </para>
    /// </remarks>
    public static async Task<AzureCredentialProvider> CreateAsync(
        AzureCredentialOptions credentialOptions,
        ILogger logger)
    {
        // Ensure that the Sources array is not null or empty
        if (credentialOptions?.Sources is null || credentialOptions.Sources.Length == 0)
        {
            throw new ArgumentNullException(nameof(credentialOptions.Sources));
        }

        // Create a ChainedTokenCredential from the specified sources
        var sources = credentialOptions.Sources
            .Select(source => source switch
            {
                // Use WorkloadIdentityCredential for Kubernetes and Azure services
                CredentialSource.WorkloadIdentity => new WorkloadIdentityCredential() as TokenCredential,
                // Use AzureCliCredential for local development environments
                CredentialSource.AzureCli => new AzureCliCredential() as TokenCredential,

                // Throw an exception if an unsupported credential source is specified
                _ => throw new ArgumentOutOfRangeException(nameof(credentialOptions.Sources))
            })
            .ToArray();

        // Create a ChainedTokenCredential that tries each source in order
        // This allows the application to authenticate using different methods depending on the environment
        var tokenCredential = new ChainedTokenCredential(sources);

        // Initialize the managed credential with the token credential
        var managedCredential = await ManagedCredential.CreateAsync(tokenCredential, logger);

        // Wrap in the provider
        return new AzureCredentialProvider(managedCredential);
    }

    #endregion

    #region ICredentialProvider

    /// <summary>
    /// Gets the name of the credential provider.
    /// </summary>
    /// <value>The string "Azure".</value>
    public string Name => "Azure";

    /// <summary>
    /// Creates an <see cref="IAccessTokenProvider"/> for the specified scope.
    /// </summary>
    /// <param name="scope">The scope of the token to request (e.g., "https://storage.azure.com/.default").</param>
    /// <returns>An <see cref="IAccessTokenProvider"/> that can provide access tokens for the specified scope.</returns>
    /// <remarks>
    /// The returned provider uses the underlying <see cref="ManagedCredential"/> to obtain tokens.
    /// A new token provider is created for each scope, but the underlying credential caches tokens.
    /// </remarks>
    public IAccessTokenProvider GetAccessTokenProvider(
        string scope)
    {
        // Create and return a new AccessTokenProvider that uses the ManagedCredential.
        return AccessTokenProvider.Create(_managedCredential, scope);
    }

    /// <summary>
    /// Gets the current status of the credential.
    /// </summary>
    /// <returns>A <see cref="CredentialStatus"/> object containing the health and details of all managed tokens.</returns>
    /// <remarks>
    /// Delegates to <see cref="ManagedCredential.GetStatus"/> to obtain the status of all tokens.
    /// </remarks>
    public CredentialStatus GetStatus()
    {
        // Delegate to the ManagedCredential to get the status.
        return _managedCredential.GetStatus();
    }

    #endregion ICredentialProvider

    #region ICredentialProvider<TokenCredential>

    /// <summary>
    /// Gets the underlying <see cref="TokenCredential"/> for direct use with Azure SDK clients.
    /// </summary>
    /// <returns>The <see cref="TokenCredential"/> instance wrapped by this provider.</returns>
    /// <remarks>
    /// Provides direct access to the underlying <see cref="TokenCredential"/> for Azure SDK clients.
    /// The returned credential is a <see cref="ManagedCredential"/> wrapping the <see cref="ChainedTokenCredential"/>.
    /// </remarks>
    public TokenCredential GetCredential()
    {
        // Return the ManagedCredential, which implements TokenCredential.
        return _managedCredential;
    }

    #endregion ICredentialProvider<TokenCredential>
}
