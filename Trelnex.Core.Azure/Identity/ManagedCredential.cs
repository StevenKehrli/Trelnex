using System.Collections;
using System.Collections.Concurrent;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Identity;

using AzureToken = Azure.Core.AccessToken;
using TrelnexToken = Trelnex.Core.Identity.AccessToken;

namespace Trelnex.Core.Azure.Identity;

/// <summary>
/// A credential that manages authentication to Microsoft Entra ID (formerly Azure AD) to obtain access tokens.
/// </summary>
/// <remarks>
/// <para>
/// ManagedCredential implements both <see cref="TokenCredential"/> and <see cref="ICredential"/>, allowing it
/// to be used with both Azure SDK clients (via TokenCredential) and Trelnex's credential system (via ICredential).
/// </para>
/// <para>
/// This credential is responsible for:
/// <list type="bullet">
///   <item><description>Caching tokens by their request context (scopes, tenant, etc.)</description></item>
///   <item><description>Automatically refreshing tokens before they expire</description></item>
///   <item><description>Providing token status reporting</description></item>
///   <item><description>Converting between Azure and Trelnex token formats</description></item>
/// </list>
/// </para>
/// <para>
/// ManagedCredential is an internal class. Application code will interact with this via the <see cref="AzureCredentialProvider"/>.
/// </para>
/// </remarks>
/// <param name="logger">The logger used for recording token acquisition and refresh events.</param>
/// <param name="tokenCredential">The underlying credential used to acquire tokens from Azure.</param>
internal class ManagedCredential(
    ILogger logger,
    TokenCredential tokenCredential)
    : TokenCredential, ICredential
{
    /// <summary>
    /// A thread-safe cache of token items indexed by their request contexts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This dictionary maps token request contexts to their corresponding token items.
    /// The Lazy wrapper ensures that token acquisition for a given context happens only once,
    /// even if multiple threads request the same token simultaneously.
    /// </para>
    /// <para>
    /// Each AzureTokenItem handles its own token refresh scheduling.
    /// </para>
    /// </remarks>
    private readonly ConcurrentDictionary<TokenRequestContextKey, Lazy<AzureTokenItem>> _azureTokenItemsByTokenRequestContextKey = new();

#region TokenCredential Implementation

    /// <summary>
    /// Gets an access token for the specified context.
    /// </summary>
    /// <param name="tokenRequestContext">The context containing the details of the token request.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>An access token for the specified request context.</returns>
    /// <exception cref="CredentialUnavailableException">Thrown when the credential cannot retrieve a token.</exception>
    /// <remarks>
    /// <para>
    /// This method implements the <see cref="TokenCredential.GetToken"/> method, making this class
    /// usable with Azure SDK clients.
    /// </para>
    /// <para>
    /// Tokens are cached by request context. If a token for the specified context already exists
    /// and is valid, it is returned immediately. Otherwise, a new token is acquired and cached.
    /// </para>
    /// <para>
    /// Tokens are refreshed before expiry by a background timer.
    /// </para>
    /// </remarks>
    public override AzureToken GetToken(
        TokenRequestContext tokenRequestContext,
        CancellationToken cancellationToken)
    {
        // Create a cache key from the token request context.
        var key = TokenRequestContextKey.FromTokenRequestContext(tokenRequestContext);

        // Get or create a token item for this request context.
        // Using Lazy<T> ensures thread safety during creation.
        // See: https://andrewlock.net/making-getoradd-on-concurrentdictionary-thread-safe-using-lazy/
        var lazyAzureTokenItem =
            _azureTokenItemsByTokenRequestContextKey.GetOrAdd(
                key: key,
                value: new Lazy<AzureTokenItem>(
                    AzureTokenItem.Create(
                        logger,
                        tokenCredential,
                        key)));

        // Return the token from the token item.
        return lazyAzureTokenItem.Value.GetAzureToken();
    }

    /// <summary>
    /// Gets an access token for the specified context asynchronously.
    /// </summary>
    /// <param name="tokenRequestContext">The context containing the details of the token request.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>A task that completes with an access token for the specified request context.</returns>
    /// <remarks>
    /// This implementation delegates to <see cref="GetToken(TokenRequestContext, CancellationToken)"/>.
    /// The synchronous implementation is sufficient because tokens are cached and refreshed in the background.
    /// </remarks>
    public override ValueTask<AzureToken> GetTokenAsync(
        TokenRequestContext tokenRequestContext,
        CancellationToken cancellationToken)
    {
        // Delegate to the synchronous GetToken method.
        return ValueTask.FromResult(
            GetToken(tokenRequestContext, cancellationToken));
    }

#endregion

#region ICredential Implementation

    /// <summary>
    /// Gets a Trelnex access token for the specified scope.
    /// </summary>
    /// <param name="scope">The scope required for the token (e.g., "https://storage.azure.com/.default").</param>
    /// <returns>A Trelnex <see cref="AccessToken"/> for the specified scope.</returns>
    /// <exception cref="AccessTokenUnavailableException">Thrown when the credential cannot retrieve a token.</exception>
    /// <remarks>
    /// <para>
    /// This method implements the <see cref="ICredential.GetAccessToken"/> method, making this class
    /// usable with Trelnex's token provider system.
    /// </para>
    /// <para>
    /// It creates a token request context for the specified scope, acquires an Azure token,
    /// and converts it to a Trelnex token format.
    /// </para>
    /// </remarks>
    public TrelnexToken GetAccessToken(
        string scope)
    {
        // Format the scope into a TokenRequestContext.
        var tokenRequestContext = new TokenRequestContext(
            scopes: [ scope ]);

        try
        {
            // Get the Azure access token.
            var azureToken = GetToken(tokenRequestContext, default);

            // Convert to Trelnex access token format.
            return new TrelnexToken{
                Token = azureToken.Token,
                TokenType = azureToken.TokenType,
                ExpiresOn = azureToken.ExpiresOn,
                RefreshOn = azureToken.RefreshOn
            };
        }
        catch (CredentialUnavailableException ex)
        {
            // Translate Azure exception to Trelnex exception.
            throw new AccessTokenUnavailableException(ex.Message, ex.InnerException);
        }
    }

    /// <summary>
    /// Gets the status of all credentials managed by this provider.
    /// </summary>
    /// <returns>A <see cref="CredentialStatus"/> object containing the status of all managed tokens.</returns>
    /// <remarks>
    /// <para>
    /// This method implements the <see cref="ICredential.GetStatus"/> method, allowing health monitoring
    /// of the credential.
    /// </para>
    /// <para>
    /// It collects status information from all token items in the cache, including their health,
    /// scopes, expiration, and other metadata.
    /// </para>
    /// </remarks>
    public CredentialStatus GetStatus()
    {
        // Collect status of all token items in the cache.
        var statuses = _azureTokenItemsByTokenRequestContextKey
            .Select(kvp =>
            {
                var lazy = kvp.Value;
                var azureTokenItem = lazy.Value;

                return azureTokenItem.GetStatus();
            })
            .OrderBy(status => string.Join(", ", status.Scopes))
            .ToArray();

        // Return a consolidated credential status with all token statuses.
        return new CredentialStatus(
            Statuses: statuses ?? []);
    }

#endregion

    /// <summary>
    /// A reference-type wrapper for <see cref="TokenRequestContext"/> that serves as a key for caching tokens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides a stable key for the token cache dictionary, with proper equality comparison
    /// and hash code generation for the relevant properties of a token request context.
    /// </para>
    /// <para>
    /// It deliberately ignores the <see cref="TokenRequestContext.ParentRequestId"/> property since
    /// it's not relevant for token caching.
    /// </para>
    /// </remarks>
    private class TokenRequestContextKey(
        string? claims,
        bool isCaeEnabled,
        string[] scopes,
        string? tenantId)
    {
        /// <summary>
        /// Gets the additional claims to be included in the token.
        /// </summary>
        /// <value>
        /// A JSON string of claims as defined in the OpenID Connect Core specification.
        /// See <see href="https://openid.net/specs/openid-connect-core-1_0-final.html#ClaimsParameter">
        /// OpenID Connect Claims Parameter</see> for more information.
        /// </value>
        public string? Claims => claims;

        /// <summary>
        /// Gets a value indicating whether Continuous Access Evaluation (CAE) is enabled for the token.
        /// </summary>
        /// <value>
        /// <c>true</c> if CAE is enabled; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// Continuous Access Evaluation enables real-time security signals during token usage.
        /// See <see href="https://learn.microsoft.com/en-us/azure/active-directory/conditional-access/concept-continuous-access-evaluation">
        /// Continuous Access Evaluation</see> for more information.
        /// </remarks>
        public bool IsCaeEnabled => isCaeEnabled;

        /// <summary>
        /// Gets the scopes required for the token.
        /// </summary>
        /// <value>
        /// An array of scope strings identifying the Azure resources and permissions needed.
        /// </value>
        public string[] Scopes => scopes;

        /// <summary>
        /// Gets the tenant ID to be included in the token request.
        /// </summary>
        /// <value>
        /// The tenant ID, or <c>null</c> to use the default tenant.
        /// </value>
        public string? TenantId => tenantId;

        /// <summary>
        /// Creates a <see cref="TokenRequestContextKey"/> from a <see cref="TokenRequestContext"/>.
        /// </summary>
        /// <param name="tokenRequestContext">The token request context to convert.</param>
        /// <returns>A new <see cref="TokenRequestContextKey"/> with values from the token request context.</returns>
        public static TokenRequestContextKey FromTokenRequestContext(
            TokenRequestContext tokenRequestContext)
        {
            // Create a new TokenRequestContextKey from the provided TokenRequestContext.
            return new TokenRequestContextKey(
                claims: tokenRequestContext.Claims,
                isCaeEnabled: tokenRequestContext.IsCaeEnabled,
                scopes: tokenRequestContext.Scopes,
                tenantId: tokenRequestContext.TenantId);
        }

        /// <summary>
        /// Converts this <see cref="TokenRequestContextKey"/> back to a <see cref="TokenRequestContext"/>.
        /// </summary>
        /// <returns>A new <see cref="TokenRequestContext"/> with values from this key.</returns>
        public TokenRequestContext ToTokenRequestContext()
        {
            // Create a new TokenRequestContext from the values in this TokenRequestContextKey.
            return new TokenRequestContext(
                claims: Claims,
                isCaeEnabled: IsCaeEnabled,
                scopes: Scopes,
                tenantId: TenantId);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current <see cref="TokenRequestContextKey"/>.
        /// </summary>
        /// <param name="obj">The object to compare with the current key.</param>
        /// <returns><c>true</c> if the specified object is a <see cref="TokenRequestContextKey"/> and
        /// has the same property values; otherwise, <c>false</c>.</returns>
        public override bool Equals(object? obj)
        {
            // Check if the object is null or of a different type.
            return (obj is TokenRequestContextKey other) && Equals(other);
        }

        /// <summary>
        /// Determines whether the specified <see cref="TokenRequestContextKey"/> is equal to the current key.
        /// </summary>
        /// <param name="other">The <see cref="TokenRequestContextKey"/> to compare with the current key.</param>
        /// <returns><c>true</c> if the specified key has the same property values; otherwise, <c>false</c>.</returns>
        private bool Equals(
            TokenRequestContextKey other)
        {
            // Compare each property for equality.
            if (string.Equals(Claims, other.Claims) is false) return false;

            if (IsCaeEnabled != other.IsCaeEnabled) return false;

            // Use structural comparison for the scopes array.
            if (StructuralComparisons.StructuralEqualityComparer.Equals(Scopes, other.Scopes) is false) return false;

            if (string.Equals(TenantId, other.TenantId) is false) return false;

            return true;
        }

        /// <summary>
        /// Returns a hash code for the current <see cref="TokenRequestContextKey"/>.
        /// </summary>
        /// <returns>A hash code for the current key.</returns>
        public override int GetHashCode()
        {
            // Create a hash code that combines all property values.
            var hashCode = HashCode.Combine(
                claims,
                isCaeEnabled,
                StructuralComparisons.StructuralEqualityComparer.GetHashCode(scopes),
                tenantId);

            return hashCode;
        }
    }

    /// <summary>
    /// Manages an Azure access token with automatic refresh capabilities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An <see cref="AzureTokenItem"/> is responsible for acquiring, caching, and refreshing a token
    /// for a specific request context. It uses a timer to refresh the token before it expires,
    /// ensuring that valid tokens are always available.
    /// </para>
    /// <para>
    /// This class handles both successful token acquisition and failure cases, maintaining
    /// appropriate state to report token status and errors.
    /// </para>
    /// </remarks>
    private class AzureTokenItem
    {
        /// <summary>
        /// The logger used for diagnostic information.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The credential used to acquire tokens.
        /// </summary>
        private readonly TokenCredential _tokenCredential;

        /// <summary>
        /// The request context for which this item acquires tokens.
        /// </summary>
        private readonly TokenRequestContextKey _tokenRequestContextKey;

        /// <summary>
        /// The timer used to schedule token refresh operations.
        /// </summary>
        private readonly Timer _timer;

        /// <summary>
        /// The current access token, or null if no valid token is available.
        /// </summary>
        private AzureToken? _azureToken;

        /// <summary>
        /// The error message to use when a token is unavailable, or null if no error occurred.
        /// </summary>
        private string? _unavailableMessage;

        /// <summary>
        /// The inner exception to include when a token is unavailable, or null if no error occurred.
        /// </summary>
        private Exception? _unavailableInnerException;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureTokenItem"/> class.
        /// </summary>
        /// <param name="logger">The logger used for diagnostic information.</param>
        /// <param name="tokenCredential">The credential used to acquire tokens.</param>
        /// <param name="tokenRequestContextKey">The request context for which to acquire tokens.</param>
        private AzureTokenItem(
            ILogger logger,
            TokenCredential tokenCredential,
            TokenRequestContextKey tokenRequestContextKey)
        {
            _logger = logger;
            _tokenCredential = tokenCredential;
            _tokenRequestContextKey = tokenRequestContextKey;

            // Create a timer but don't start it yet.
            _timer = new Timer(Refresh, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Creates and initializes a new <see cref="AzureTokenItem"/> instance.
        /// </summary>
        /// <param name="logger">The logger used for diagnostic information.</param>
        /// <param name="tokenCredential">The credential used to acquire tokens.</param>
        /// <param name="tokenRequestContextKey">The request context for which to acquire tokens.</param>
        /// <returns>A new <see cref="AzureTokenItem"/> instance with an initial token.</returns>
        /// <remarks>This method creates the token item and immediately triggers a token refresh to acquire the initial token.</remarks>
        public static AzureTokenItem Create(
            ILogger logger,
            TokenCredential tokenCredential,
            TokenRequestContextKey tokenRequestContextKey)
        {
            // Create the token item.
            var azureTokenItem = new AzureTokenItem(
                logger,
                tokenCredential,
                tokenRequestContextKey);

            // Immediately trigger a refresh to acquire the initial token.
            azureTokenItem.Refresh(null);

            return azureTokenItem;
        }

        /// <summary>
        /// Gets the current access token.
        /// </summary>
        /// <returns>The current access token.</returns>
        /// <exception cref="CredentialUnavailableException">Thrown when no valid token is available.</exception>
        public AzureToken GetAzureToken()
        {
            lock (this)
            {
                // If no token is available, throw an exception with the error details.
                return _azureToken ?? throw new CredentialUnavailableException(_unavailableMessage, _unavailableInnerException);
            }
        }

        /// <summary>
        /// Gets the status of the access token managed by this item.
        /// </summary>
        /// <returns>An <see cref="AccessTokenStatus"/> containing the token's health and metadata.</returns>
        public AccessTokenStatus GetStatus()
        {
            lock (this)
            {
                // Determine if the token is valid based on its expiration time.
                var health = ((_azureToken?.ExpiresOn ?? DateTimeOffset.MinValue) < DateTimeOffset.UtcNow)
                    ? AccessTokenHealth.Expired
                    : AccessTokenHealth.Valid;

                // Include additional metadata about the token.
                var data = new Dictionary<string, object?>
                {
                    { "tenantId", _tokenRequestContextKey.TenantId },
                    { "claims", _tokenRequestContextKey.Claims },
                    { "isCaeEnabled", _tokenRequestContextKey.IsCaeEnabled },
                };

                // Return a status object with all relevant information.
                return new AccessTokenStatus(
                    Health: health,
                    Scopes: _tokenRequestContextKey.Scopes,
                    ExpiresOn: _azureToken?.ExpiresOn,
                    Data: data);
            }
        }

        /// <summary>
        /// Refreshes the access token and schedules the next refresh.
        /// </summary>
        /// <param name="state">The state object passed by the Timer (not used).</param>
        private void Refresh(object? state)
        {
            // Log the refresh attempt.
            _logger.LogInformation(
                "AzureTokenItem.Refresh: scopes = '{scopes:l}', tenantId = '{tenantId:l}', claims = '{claims:l}', isCaeEnabled = '{isCaeEnabled}'",
                string.Join(", ", _tokenRequestContextKey.Scopes),
                _tokenRequestContextKey.TenantId,
                _tokenRequestContextKey.Claims,
                _tokenRequestContextKey.IsCaeEnabled);

            // Default to refreshing in 5 seconds if something goes wrong.
            var dueTime = TimeSpan.FromSeconds(5);

            try
            {
                // Attempt to get a new token.
                var azureToken = _tokenCredential.GetToken(
                    requestContext: _tokenRequestContextKey.ToTokenRequestContext(),
                    cancellationToken: default);

                // Store the new token.
                SetAzureToken(azureToken);

                // Determine when to refresh the token.
                // WorkloadIdentityCredential will have RefreshOn set - use that.
                // AzureCliCredential will not - fall back to ExpiresOn.
                var refreshOn = azureToken.RefreshOn ?? azureToken.ExpiresOn;

                // Log the successful token acquisition.
                _logger.LogInformation(
                    "AzureTokenItem.AccessToken: scopes = '{scopes:l}', tenantId = '{tenantId:l}', claims = '{claims:l}', isCaeEnabled = '{isCaeEnabled}', refreshOn = '{refreshOn:o}'.",
                    string.Join(", ", _tokenRequestContextKey.Scopes),
                    _tokenRequestContextKey.TenantId,
                    _tokenRequestContextKey.Claims,
                    _tokenRequestContextKey.IsCaeEnabled,
                    refreshOn);

                // Schedule the next refresh at the token's refresh time.
                dueTime = refreshOn - DateTimeOffset.UtcNow;
            }
            catch (CredentialUnavailableException ex)
            {
                // Handle credential unavailable errors.
                SetUnavailable(ex);

                // Log the error.
                _logger.LogError(
                    "AzureTokenItem.Unavailable: scopes = '{scopes:l}', tenantId = '{tenantId:l}', claims = '{claims:l}', isCaeEnabled = '{isCaeEnabled}', message = '{message:}'.",
                    string.Join(", ", _tokenRequestContextKey.Scopes),
                    _tokenRequestContextKey.TenantId,
                    _tokenRequestContextKey.Claims,
                    _tokenRequestContextKey.IsCaeEnabled,
                    ex.Message);
            }
            catch
            {
                // Catch any other exceptions to ensure the timer is always rescheduled.
            }

            // Schedule the next refresh.
            _timer.Change(
                dueTime: dueTime,
                period: Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Sets a new access token and clears any error state.
        /// </summary>
        /// <param name="azureToken">The new access token.</param>
        private void SetAzureToken(
            AzureToken azureToken)
        {
            lock (this)
            {
                // Store the new token.
                _azureToken = azureToken;

                // Clear any error state.
                _unavailableMessage = null;
                _unavailableInnerException = null;
            }
        }

        /// <summary>
        /// Sets the error information to use when the token is unavailable.
        /// </summary>
        /// <param name="ex">The exception that occurred during token acquisition.</param>
        private void SetUnavailable(
            CredentialUnavailableException ex)
        {
            lock (this)
            {
                // Store the error information.
                _unavailableMessage = ex.Message;
                _unavailableInnerException = ex.InnerException;
            }
        }
    }
}
