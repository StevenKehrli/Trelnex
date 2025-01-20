using System.Collections.Concurrent;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Azure.Identity;

/// <summary>
/// A class that provides Azure credentials and status.
/// </summary>
internal class AzureCredentialProvider : ICredentialProvider
{
    /// <summary>
    /// The <see cref="ILogger"/>.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// The underlying <see cref="TokenCredential"/>.
    /// </summary>
    private readonly TokenCredential _credential;

    /// <summary>
    /// A thread-safe collection of <see cref="string"/> to <see cref="NamedCredential"/>.
    /// </summary>
    private readonly ConcurrentDictionary<string, Lazy<NamedCredential>> _namedCredentialsByName = new();

    private AzureCredentialProvider(
        ILogger logger,
        TokenCredential credential)
    {
        _logger = logger;
        _credential = credential;
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
        // create the credential
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

        var credential = new ChainedTokenCredential(sources);

        return new AzureCredentialProvider(logger, credential);
    }

    /// <summary>
    /// Gets the <see cref="TokenCredential"/> for the specified credential name.
    /// </summary>
    /// <param name="credentialName">The name of the specified credential.</param>
    /// <returns>The <see cref="TokenCredential"/>.</returns>
    public TokenCredential GetCredential(
        string credentialName)
    {
        return GetNamedCredential(credentialName);
    }

    /// <summary>
    /// Gets the array of <see cref="ICredentialStatusProvider"/> for all credentials.
    /// </summary>
    /// <returns>The array of <see cref="ICredentialStatusProvider"/>.</returns>
    public ICredentialStatusProvider[] GetStatusProviders()
    {
        return _namedCredentialsByName
            .OrderBy(kvp => kvp.Key)
            .Select(kvp =>
            {
                var credentialName = kvp.Key;

                // get the credential
                var lazyNamedCredential = kvp.Value;

                return lazyNamedCredential.Value;
            })
            .ToArray();
    }

    /// <summary>
    /// Gets the <see cref="IAccessTokenProvider"/> for the specified credential name and scope.
    /// </summary>
    /// <param name="credentialName">The name of the credential.</param>
    /// <param name="scope">The scope of the token.</param>
    /// <returns>The <see cref="IAccessTokenProvider"/> for the specified credential name.</returns>
    public IAccessTokenProvider GetTokenProvider(
        string credentialName,
        string scope)
    {
        var namedCredential = GetNamedCredential(credentialName);

        return TokenProvider.Create(credentialName, scope, namedCredential);
    }

    /// <summary>
    /// Gets the <see cref="NamedCredential"/> for the specified credential name.
    /// </summary>
    /// <param name="credentialName">The name of the specified credential.</param>
    /// <returns>The <see cref="NamedCredential"/>.</returns>
    private NamedCredential GetNamedCredential(
        string credentialName)
    {
        // https://andrewlock.net/making-getoradd-on-concurrentdictionary-thread-safe-using-lazy/
        var lazyNamedCredential =
            _namedCredentialsByName.GetOrAdd(
                key: credentialName,
                value: new Lazy<NamedCredential>(
                    () => new NamedCredential(_logger, credentialName, _credential)));

        return lazyNamedCredential.Value;
    }

    private class TokenProvider : IAccessTokenProvider
    {
        private readonly string _credentialName;
        private readonly string _scope;
        private readonly NamedCredential _namedCredential;

        private TokenProvider(
            string credentialName,
            string scope,
            NamedCredential namedCredential)
        {
            _credentialName = credentialName;
            _scope = scope;
            _namedCredential = namedCredential;
        }

        public static TokenProvider Create(
            string credentialName,
            string scope,
            NamedCredential namedCredential)
        {
            // create the token provider
            var tokenProvider = new TokenProvider(credentialName, scope, namedCredential);

            // warm-up this token
            tokenProvider.GetToken();

            return tokenProvider;
        }

        public string CredentialName => _credentialName;

        public string Scope => _scope;

        public string GetAuthorizationHeader()
        {
            return _namedCredential.GetAuthorizationHeader(_scope);
        }

        public IAccessToken GetToken()
        {
            return _namedCredential.GetToken(_scope);
        }
    }
}
