using System.Collections;
using System.Collections.Concurrent;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Azure.Identity;

/// <summary>
/// Enables authentication to Microsoft Entra ID to obtain an access token.
/// </summary>
/// <remarks>
/// <para>
/// NamedCredential is an internal class. Users will get an instance of the <see cref="TokenCredential"/> abstract class.
/// </para>
/// <para>
/// A NamedCredential is addressable by the credential name within <see cref="CredentialFactory"/>.
/// </para>
/// </remarks>
/// <param name="logger">The <see cref="ILogger"> used to perform logging.</param>
/// <param name="credentialName">The name of this <see cref="TokenCredential"/>.</param>
/// <param name="tokenCredential">The underlying <see cref="ChainedTokenCredential"/> (<see cref="WorkloadIdentityCredential"/> and <see cref="AzureCliCredential"/>).</param>
internal class NamedCredential(
    ILogger logger,
    string credentialName,
    TokenCredential tokenCredential)
    : TokenCredential, ICredentialStatusProvider
{
    /// <summary>
    /// A thread-safe collection of <see cref="TokenRequestContext"/> to <see cref="AccessTokenItem"/>.
    /// </summary>
    private readonly ConcurrentDictionary<TokenRequestContextKey, Lazy<AccessTokenItem>> _accessTokenItemsByTokenRequestContextKey = new();

    public string CredentialName => credentialName;

#region TokenCredential

    public override AccessToken GetToken(
        TokenRequestContext tokenRequestContext,
        CancellationToken cancellationToken)
    {
        // create a TokenRequestContextKey - we do not care about ParentRequestId
        var key = TokenRequestContextKey.FromTokenRequestContext(tokenRequestContext);

        // https://andrewlock.net/making-getoradd-on-concurrentdictionary-thread-safe-using-lazy/
        var lazyAccessTokenItem =
            _accessTokenItemsByTokenRequestContextKey.GetOrAdd(
                key: key,
                value: new Lazy<AccessTokenItem>(
                    AccessTokenItem.Create(
                        logger,
                        credentialName,
                        tokenCredential,
                        key)));

        return lazyAccessTokenItem.Value.GetToken();
    }

    public override ValueTask<AccessToken> GetTokenAsync(
        TokenRequestContext tokenRequestContext,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(
            GetToken(tokenRequestContext, cancellationToken));
    }

#endregion

#region ICredentialStatusProvider

    /// <summary>
    /// Gets the <see cref="CredentialStatus"/> of the credential used by this token provider.
    /// </summary>
    /// <returns>The <see cref="CredentialStatus"/> of the credential used by this token provider.</returns>
    public CredentialStatus GetStatus()
    {
        // get the access token item
        var statuses = _accessTokenItemsByTokenRequestContextKey
            .Select(kvp =>
            {
                var lazy = kvp.Value;
                var accessTokenItem = lazy.Value;

                return accessTokenItem.GetStatus();
            })
            .OrderBy(status => string.Join(", ", status.Scopes))
            .ToArray();

        return new CredentialStatus(
            CredentialName: credentialName,
            Statuses: statuses ?? []);
    }

#endregion

    /// <summary>
    /// Gets the authorization header for the specified scope.
    /// </summary>
    /// <param name="scope">The scope required for the authorization header.</param>
    /// <returns>The authorization header for the specified scope.</returns>
    public string GetAuthorizationHeader(
        string scope)
    {
        // get the access token and return its authorization header
        var accessToken = GetToken(scope);

        return accessToken.GetAuthorizationHeader();
    }

    /// <summary>
    /// Gets the <see cref="IAccessToken"/> for the specified scope.
    /// </summary>
    /// <param name="scope">The scope required for the token.</param>
    /// <returns>The <see cref="IAccessToken"/> for the specified scope.</returns>
    public IAccessToken GetToken(
        string scope)
    {
        // format the scope into a TokenRequestContext
        var tokenRequestContext = new TokenRequestContext(
            scopes: [ scope ]);

        // get the access token
        var accessToken = GetToken(tokenRequestContext, default);

        return new NamedCredentialAccesssToken{
            Token = accessToken.Token,
            TokenType = accessToken.TokenType,
            ExpiresOn = accessToken.ExpiresOn,
            RefreshOn = accessToken.RefreshOn
        };
    }

    private class NamedCredentialAccesssToken : IAccessToken
    {
        /// <summary>
        /// Get the access token value.
        /// </summary>
        public required string Token { get; init; }

        /// <summary>
        /// Identifies the type of access token.
        /// </summary>
        public required string TokenType { get; init; }

        /// <summary>
        /// Gets the time when the provided token expires.
        /// </summary>
        public required DateTimeOffset ExpiresOn { get; init; }

        /// <summary>
        /// Gets the time when the token should be refreshed.
        /// </summary>
        public required DateTimeOffset? RefreshOn { get; init; }

        /// <summary>
        /// Gets the authorization header for this access token.
        /// </summary>
        /// <returns>The authorization header for this access token.</returns>
        public string GetAuthorizationHeader()
        {
            // format the authorization header
            return $"{TokenType} {Token}";
        }
    }

    /// <summary>
    /// Contains the details of an authentication token request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is used as the key to <see cref="_accessTokenItemsByTokenRequestContextKey"/>.
    /// </para>
    /// <para>
    /// This is a class (reference type) alternative to the struct (value type) of <see cref="TokenRequestContext"/>.
    /// </para>
    /// <para>
    /// This ignores the <see cref="TokenRequestContext.ParentRequestId"/> property.
    /// It is not used by <see cref="GetToken"/> and <see cref="GetTokenAsync"/>.
    /// </para>
    /// <para>
    /// This implements the <see cref="Equals"/> and <see cref="GetHashCode"/> necessary for the <see cref="_accessTokenItemsByTokenRequestContextKey"/>.
    /// </para>
    /// </remarks>
    /// <param name="claims">Additional claims to be included in the token.</param>
    /// <param name="isCaeEnabled">Indicates whether to enable Continuous Access Evaluation (CAE) for the requested token.</param>
    /// <param name="scopes">The scopes required for the token.</param>
    /// <param name="tenantId">The tenantId to be included in the token request.</param>
    private class TokenRequestContextKey(
        string? claims,
        bool isCaeEnabled,
        string[] scopes,
        string? tenantId)
    {
        /// <summary>
        /// Additional claims to be included in the token. See <see href="https://openid.net/specs/openid-connect-core-1_0-final.html#ClaimsParameter">https://openid.net/specs/openid-connect-core-1_0-final.html#ClaimsParameter</see> for more information on format and content.
        /// </summary>
        public string? Claims => claims;

        /// <summary>
        /// Indicates whether to enable Continuous Access Evaluation (CAE) for the requested token.
        /// </summary>
        public bool IsCaeEnabled => isCaeEnabled;

        /// <summary>
        /// The scopes required for the token.
        /// </summary>
        public string[] Scopes => scopes;

        /// <summary>
        /// The tenantId to be included in the token request.
        /// </summary>
        public string? TenantId => tenantId;

        /// <summary>
        /// Converts a <see cref="TokenRequestContext"/> to a <see cref="TokenRequestContextKey"/>.
        /// </summary>
        /// <param name="tokenRequestContext">The <see cref="TokenRequestContext"/>.</param>
        /// <returns>A <see cref="TokenRequestContextKey"/>.</returns>
        public static TokenRequestContextKey FromTokenRequestContext(
            TokenRequestContext tokenRequestContext)
        {
            return new TokenRequestContextKey(
                claims: tokenRequestContext.Claims,
                isCaeEnabled: tokenRequestContext.IsCaeEnabled,
                scopes: tokenRequestContext.Scopes,
                tenantId: tokenRequestContext.TenantId);
        }

        /// <summary>
        /// Converts this <see cref="TokenRequestContextKey"/> to a <see cref="TokenRequestContext"/>.
        /// </summary>
        /// <returns>A <see cref="TokenRequestContext"/>.</returns>
        public TokenRequestContext ToTokenRequestContext()
        {
            return new TokenRequestContext(
                claims: Claims,
                isCaeEnabled: IsCaeEnabled,
                scopes: Scopes,
                tenantId: TenantId);
        }

        public override bool Equals(object? obj)
        {
            return (obj is TokenRequestContextKey other) && Equals(other);
        }

        /// <summary>
        /// Determines whether the specified <see cref="TokenRequestContextKey"/> is equal to the current object.
        /// </summary>
        /// <param name="other">The <see cref="TokenRequestContextKey"/> to compare with the current object.</param>
        /// <returns>true if the specified <see cref="TokenRequestContextKey"/> is equal to the current object; otherwise, false.</returns>
        private bool Equals(
            TokenRequestContextKey other)
        {
            if (string.Equals(Claims, other.Claims) is false) return false;

            if (IsCaeEnabled != other.IsCaeEnabled) return false;

            if (StructuralComparisons.StructuralEqualityComparer.Equals(Scopes, other.Scopes) is false) return false;

            if (string.Equals(TenantId, other.TenantId) is false) return false;

            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = HashCode.Combine(
                claims,
                isCaeEnabled,
                StructuralComparisons.StructuralEqualityComparer.GetHashCode(scopes),
                tenantId);

            return hashCode;
        }
    }

    /// <summary>
    /// Combines an <see cref="AccessToken"/> with a <see cref="System.Threading.Timer"/> to refresh the <see cref="AccessToken"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An AccessTokenItem will refresh its access token in accordance with <see cref="AccessToken.RefreshOn"/>.
    /// This will maintain a valid access token and enable <see cref="GetToken"/> or <see cref="GetTokenAsync"/> to return immediately.
    /// </para>
    /// </remarks>
    private class AccessTokenItem
    {
        /// <summary>
        /// The <see cref="ILogger"> used to perform logging.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The name of this <see cref="TokenCredential"/>.
        /// </summary>
        private readonly string _credentialName;

        /// <summary>
        /// The underlying <see cref="TokenCredential"/>.
        /// </summary>
        private readonly TokenCredential _tokenCredential;

        /// <summary>
        /// The underlying <see cref="TokenRequestContextKey"/>.
        /// </summary>
        private readonly TokenRequestContextKey _tokenRequestContextKey;

        /// <summary>
        /// The <see cref="Timer"/> to refresh the <see cref="AccessToken"/>.
        /// </summary>
        private readonly Timer _timer;

        /// <summary>
        /// The underlying <see cref="AccessToken"/>.
        /// </summary>
        private AccessToken? _accessToken;

        /// <summary>
        /// The message to throw when the access token is unavailable.
        /// </summary>
        private string? _unavailableMessage;

        /// <summary>
        /// The inner exception to include when the access token is unavailable.
        /// </summary>
        private Exception? _unavailableInnerException;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccessTokenItem"/>.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"> used to perform logging.</param>
        /// <param name="credentialName">The name of this <see cref="TokenCredential"/>.</param>
        /// <param name="tokenCredential">The <see cref="TokenCredential"/> capable of providing a <see cref="AccessToken"/>.</param>
        /// <param name="tokenRequestContextKey">The <see cref="TokenRequestContextKey"/> containing the details of an authentication token request.</param>
        private AccessTokenItem(
            ILogger logger,
            string credentialName,
            TokenCredential tokenCredential,
            TokenRequestContextKey tokenRequestContextKey)
        {
            _logger = logger;
            _credentialName = credentialName;
            _tokenCredential = tokenCredential;
            _tokenRequestContextKey = tokenRequestContextKey;

            _timer = new Timer(Refresh, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccessTokenItem"/>.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"> used to perform logging.</param>
        /// <param name="credentialName">The name of this <see cref="TokenCredential"/>.</param>
        /// <param name="tokenCredential">The <see cref="TokenCredential"/> capable of providing a <see cref="AccessToken"/>.</param>
        /// <param name="tokenRequestContextKey">The <see cref="TokenRequestContextKey"/> containing the details of an authentication token request.</param>
        public static AccessTokenItem Create(
            ILogger logger,
            string credentialName,
            TokenCredential tokenCredential,
            TokenRequestContextKey tokenRequestContextKey)
        {
            // create the accessTokenItem and schedule the refresh (to get its access token)
            // this will set _accessToken
            var accessTokenItem = new AccessTokenItem(
                logger,
                credentialName,
                tokenCredential,
                tokenRequestContextKey);

            accessTokenItem.Refresh(null);

            return accessTokenItem;
        }

        /// <summary>
        /// Gets the <see cref="AccessToken"/>.
        /// </summary>
        public AccessToken GetToken()
        {
            lock (this)
            {
                return _accessToken ?? throw new CredentialUnavailableException(_unavailableMessage, _unavailableInnerException);
            }
        }

        /// <summary>
        /// Gets the <see cref="AccessTokenStatus"/> for this access token.
        /// </summary>
        /// <returns>A <see cref="AccessTokenStatus"/> describing the status of this access token.</returns>
        public AccessTokenStatus GetStatus()
        {
            lock (this)
            {
                var health = ((_accessToken?.ExpiresOn ?? DateTimeOffset.MinValue) < DateTimeOffset.UtcNow)
                    ? AccessTokenHealth.Expired
                    : AccessTokenHealth.Valid;

                var data = new Dictionary<string, object?>
                {
                    { "tenantId", _tokenRequestContextKey.TenantId },
                    { "claims", _tokenRequestContextKey.Claims },
                    { "isCaeEnabled", _tokenRequestContextKey.IsCaeEnabled },
                };

                return new AccessTokenStatus(
                    Health: health,
                    Scopes: _tokenRequestContextKey.Scopes,
                    Data: data,
                    ExpiresOn: _accessToken?.ExpiresOn);
            }
        }

        /// <summary>
        /// The <see cref="TimerCallback"/> delegate of the <see cref="_timer"/> <see cref="Timer"/>.
        /// </summary>
        /// <param name="state">An object containing application-specific information relevant to the method invoked by this delegate, or null.</param>
        private void Refresh(object? state)
        {
            _logger.LogInformation(
                "AccessTokenItem.Refresh: '{credentialName:l}', scopes: '{scopes:l}', tenantId: '{tenantId:l}', claims: '{claims:l}', isCaeEnabled: '{isCaeEnabled}'...",
                _credentialName,
                string.Join(", ", _tokenRequestContextKey.Scopes),
                _tokenRequestContextKey.TenantId,
                _tokenRequestContextKey.Claims,
                _tokenRequestContextKey.IsCaeEnabled);

            // assume we need to refresh in 5 seconds
            var dueTime = TimeSpan.FromSeconds(5);

            try
            {
                // get a new access token and set
                var accessToken = _tokenCredential.GetToken(
                    requestContext: _tokenRequestContextKey.ToTokenRequestContext(),
                    cancellationToken: default);

                SetAccessToken(accessToken);

                // we got a token - schedule refresh
                // workloadIdentityCredential will have RefreshOn set - use RefreshOn
                // azureCliCredential will not - use ExpiresOn
                var refreshOn = accessToken.RefreshOn ?? accessToken.ExpiresOn;

                _logger.LogInformation(
                    "AccessTokenItem.SetAccessToken: '{credentialName:l}', scopes: '{scopes:l}', tenantId: '{tenantId:l}', claims: '{claims:l}', isCaeEnabled: '{isCaeEnabled}', refreshOn: '{refreshOn:o}'.",
                    _credentialName,
                    string.Join(", ", _tokenRequestContextKey.Scopes),
                    _tokenRequestContextKey.TenantId,
                    _tokenRequestContextKey.Claims,
                    _tokenRequestContextKey.IsCaeEnabled,
                    refreshOn);

                dueTime = refreshOn - DateTimeOffset.UtcNow;
            }
            catch (CredentialUnavailableException ex)
            {
                SetUnavailable(ex);

                _logger.LogError(
                    "AccessTokenItem.SetUnavailable: '{credentialName:l}', scopes: '{scopes:l}', tenantId: '{tenantId:l}', claims: '{claims:l}', isCaeEnabled: '{isCaeEnabled}', message: '{message:}'.",
                    _credentialName,
                    string.Join(", ", _tokenRequestContextKey.Scopes),
                    _tokenRequestContextKey.TenantId,
                    _tokenRequestContextKey.Claims,
                    _tokenRequestContextKey.IsCaeEnabled,
                    ex.Message);
            }
            catch
            {
            }

            _timer.Change(
                dueTime: dueTime,
                period: Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Sets the <see cref="AccessToken"/> for this object.
        /// </summary>
        private void SetAccessToken(
            AccessToken accessToken)
        {
            lock (this)
            {
                _accessToken = accessToken;

                _unavailableMessage = null;
                _unavailableInnerException = null;
            }
        }

        /// <summary>
        /// Sets the message to throw when the access token is unavailable.
        /// </summary>
        /// <param name="ex">The <see cref="CredentialUnavailableException"/> with the message to throw when the access token is unavailable.</param>
        private void SetUnavailable(
            CredentialUnavailableException ex)
        {
            lock (this)
            {
                _unavailableMessage = ex.Message;
                _unavailableInnerException = ex.InnerException;
            }
        }
    }
}
