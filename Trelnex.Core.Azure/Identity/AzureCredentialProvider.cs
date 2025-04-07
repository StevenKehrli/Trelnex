using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Azure.Identity;

/// <summary>
/// A class that provides Azure credentials and status.
/// </summary>
internal class AzureCredentialProvider : ICredentialProvider<TokenCredential>
{
    /// <summary>
    /// The <see cref="ILogger"/>.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// The <see cref="ManagedCredential"/> over the <see cref="TokenCredential"/>.
    /// </summary>
    private readonly ManagedCredential _managedCredential;

    private AzureCredentialProvider(
        ILogger logger,
        TokenCredential tokenCredential)
    {
        _logger = logger;
        _managedCredential = new ManagedCredential(logger, tokenCredential);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureCredentialProvider"/> class.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="options">The <see cref="AzureCredentialOptions"/>.</param>
    /// <returns>The <see cref="AzureCredentialProvider"/>.</returns>
    public static AzureCredentialProvider Create(
        ILogger logger,
        AzureCredentialOptions options)
    {
        // create the token credential
        if (options?.Sources == null || options.Sources.Length == 0)
        {
            throw new ArgumentNullException(nameof(options.Sources));
        }

        var sources = options.Sources
            .Select(source => source switch
            {
                CredentialSource.WorkloadIdentity => new WorkloadIdentityCredential() as TokenCredential,
                CredentialSource.AzureCli => new AzureCliCredential() as TokenCredential,

                _ => throw new ArgumentOutOfRangeException(nameof(options.Sources))
            })
            .ToArray();

        var tokenCredential = new ChainedTokenCredential(sources);

        return new AzureCredentialProvider(logger, tokenCredential);
    }

#region ICredentialProvider

    /// <summary>
    /// Gets the name of the credential provider.
    /// </summary>
    public string Name => "Azure";

    /// <summary>
    /// Gets the <see cref="IAccessTokenProvider"/> for the specified scope.
    /// </summary>
    /// <param name="scope">The scope of the token.</param>
    /// <returns>The <see cref="IAccessTokenProvider"/> for the specified scope.</returns>
    public IAccessTokenProvider GetAccessTokenProvider(
        string scope)
    {
        return AccessTokenProvider.Create(_managedCredential, scope);
    }

    /// <summary>
    /// Gets the array of <see cref="ICredentialStatusProvider"/> for all credentials.
    /// </summary>
    /// <returns>The array of <see cref="ICredentialStatusProvider"/>.</returns>
    public CredentialStatus GetStatus()
    {
        return _managedCredential.GetStatus();
    }

#endregion ICredentialProvider

#region ICredentialProvider<TokenCredential>

    /// <summary>
    /// Gets the <see cref="TokenCredential"/> for the specified credential name.
    /// </summary>
    /// <returns>The <see cref="TokenCredential"/>.</returns>
    public TokenCredential GetCredential()
    {
        return _managedCredential;
    }

#endregion ICredentialProvider<TokenCredential>

}
