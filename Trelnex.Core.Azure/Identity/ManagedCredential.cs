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
/// Enables authentication to Microsoft Entra ID to obtain an access token.
/// </summary>
/// <remarks>
/// <para>
/// ManagedCredential is an internal class. Users will get an instance of the <see cref="TokenCredential"/> class.
/// </para>
/// </remarks>
/// <param name="logger">The <see cref="ILogger"> used to perform logging.</param>
/// <param name="tokenCredential">The underlying <see cref="ChainedTokenCredential"/>.</param>
internal class ManagedCredential(
    ILogger logger,
    TokenCredential tokenCredential)
    : TokenCredential, ICredential
{
    /// <summary>
    /// A thread-safe collection of <see cref="TokenRequestContext"/> to <see cref="AzureTokenItem"/>.
    /// </summary>
    private readonly ConcurrentDictionary<TokenRequestContextKey, Lazy<AzureTokenItem>> _azureTokenItemsByTokenRequestContextKey = new();

#region TokenCredential

    public override AzureToken GetToken(
        TokenRequestContext tokenRequestContext,
        CancellationToken cancellationToken)
    {
        // create a TokenRequestContextKey - we do not care about ParentRequestId
        var key = TokenRequestContextKey.FromTokenRequestContext(tokenRequestContext);

        // https://andrewlock.net/making-getoradd-on-concurrentdictionary-thread-safe-using-lazy/
        var lazyAzureTokenItem =
            _azureTokenItemsByTokenRequestContextKey.GetOrAdd(
                key: key,
                value: new Lazy<AzureTokenItem>(
                    AzureTokenItem.Create(
                        logger,
                        tokenCredential,
                        key)));

        return lazyAzureTokenItem.Value.GetAzureToken();
    }

    public override ValueTask<AzureToken> GetTokenAsync(
        TokenRequestContext tokenRequestContext,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(
            GetToken(tokenRequestContext, cancellationToken));
    }

#endregion

#region ICredential

    /// <summary>
    /// Gets the <see cref="Trelnex.Core.Identity.AccessToken"/> for the specified scope.
    /// </summary>
    /// <param name="scope">The scope required for the token.</param>
    /// <returns>The <see cref="Trelnex.Core.Identity.AccessToken"/> for the specified scope.</returns>
    public TrelnexToken GetAccessToken(
        string scope)
    {
        // format the scope into a TokenRequestContext
        var tokenRequestContext = new TokenRequestContext(
            scopes: [ scope ]);

        try
        {
            // get the azure access token
            var azureToken = GetToken(tokenRequestContext, default);

            // convert to trelnex access token
            return new TrelnexToken{
                Token = azureToken.Token,
                TokenType = azureToken.TokenType,
                ExpiresOn = azureToken.ExpiresOn,
                RefreshOn = azureToken.RefreshOn
            };
        }
        catch (CredentialUnavailableException ex)
        {
            throw new AccessTokenUnavailableException(ex.Message, ex.InnerException);
        }
    }

    /// <summary>
    /// Gets the <see cref="CredentialStatus"/> of the credential used by this token provider.
    /// </summary>
    /// <returns>The <see cref="CredentialStatus"/> of the credential used by this token provider.</returns>
    public CredentialStatus GetStatus()
    {
        // get the azure token item
        var statuses = _azureTokenItemsByTokenRequestContextKey
            .Select(kvp =>
            {
                var lazy = kvp.Value;
                var azureTokenItem = lazy.Value;

                return azureTokenItem.GetStatus();
            })
            .OrderBy(status => string.Join(", ", status.Scopes))
            .ToArray();

        return new CredentialStatus(
            Statuses: statuses ?? []);
    }

#endregion

    /// <summary>
    /// Contains the details of an access token request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is used as the key to <see cref="_azureTokenItemsByTokenRequestContextKey"/>.
    /// </para>
    /// <para>
    /// This is a class (reference type) alternative to the struct (value type) of <see cref="TokenRequestContext"/>.
    /// </para>
    /// <para>
    /// This ignores the <see cref="TokenRequestContext.ParentRequestId"/> property.
    /// It is not used by <see cref="GetToken"/> and <see cref="GetTokenAsync"/>.
    /// </para>
    /// <para>
    /// This implements the <see cref="Equals"/> and <see cref="GetHashCode"/> necessary for the <see cref="_azureTokenItemsByTokenRequestContextKey"/>.
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
    /// Combines an <see cref="Azure.Core.AccessToken"/> with a <see cref="System.Threading.Timer"/> to refresh the <see cref="Azure.Core.AccessToken"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An AzureTokenItem will refresh its token in accordance with <see cref="Azure.Core.AccessToken.RefreshOn"/>.
    /// This will maintain a valid token and enable <see cref="GetToken"/> to return immediately.
    /// </para>
    /// </remarks>
    private class AzureTokenItem
    {
        /// <summary>
        /// The <see cref="ILogger"> used to perform logging.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The underlying <see cref="TokenCredential"/>.
        /// </summary>
        private readonly TokenCredential _tokenCredential;

        /// <summary>
        /// The underlying <see cref="TokenRequestContextKey"/>.
        /// </summary>
        private readonly TokenRequestContextKey _tokenRequestContextKey;

        /// <summary>
        /// The <see cref="Timer"/> to refresh the <see cref="Azure.Core.AccessToken"/>.
        /// </summary>
        private readonly Timer _timer;

        /// <summary>
        /// The underlying <see cref="Azure.Core.AccessToken"/>.
        /// </summary>
        private AzureToken? _azureToken;

        /// <summary>
        /// The message to throw when the token is unavailable.
        /// </summary>
        private string? _unavailableMessage;

        /// <summary>
        /// The inner exception to include when the token is unavailable.
        /// </summary>
        private Exception? _unavailableInnerException;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureTokenItem"/>.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"> used to perform logging.</param>
        /// <param name="tokenCredential">The <see cref="TokenCredential"/> capable of providing a <see cref="Azure.Core.AccessToken"/>.</param>
        /// <param name="tokenRequestContextKey">The <see cref="TokenRequestContextKey"/> containing the details for the access token request.</param>
        private AzureTokenItem(
            ILogger logger,
            TokenCredential tokenCredential,
            TokenRequestContextKey tokenRequestContextKey)
        {
            _logger = logger;
            _tokenCredential = tokenCredential;
            _tokenRequestContextKey = tokenRequestContextKey;

            _timer = new Timer(Refresh, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureTokenItem"/>.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"> used to perform logging.</param>
        /// <param name="tokenCredential">The <see cref="TokenCredential"/> capable of providing a <see cref="Azure.Core.AccessToken"/>.</param>
        /// <param name="tokenRequestContextKey">The <see cref="TokenRequestContextKey"/> containing the details for the access token request.</param>
        public static AzureTokenItem Create(
            ILogger logger,
            TokenCredential tokenCredential,
            TokenRequestContextKey tokenRequestContextKey)
        {
            // create the azureTokenItem and schedule the refresh (to get its token)
            // this will set _azureToken
            var azureTokenItem = new AzureTokenItem(
                logger,
                tokenCredential,
                tokenRequestContextKey);

            azureTokenItem.Refresh(null);

            return azureTokenItem;
        }

        /// <summary>
        /// Gets the <see cref="Azure.Core.AccessToken"/>.
        /// </summary>
        public AzureToken GetAzureToken()
        {
            lock (this)
            {
                return _azureToken ?? throw new CredentialUnavailableException(_unavailableMessage, _unavailableInnerException);
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
                var health = ((_azureToken?.ExpiresOn ?? DateTimeOffset.MinValue) < DateTimeOffset.UtcNow)
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
                    ExpiresOn: _azureToken?.ExpiresOn,
                    Data: data);
            }
        }

        /// <summary>
        /// The <see cref="TimerCallback"/> delegate of the <see cref="_timer"/> <see cref="Timer"/>.
        /// </summary>
        /// <param name="state">An object containing application-specific information relevant to the method invoked by this delegate, or null.</param>
        private void Refresh(object? state)
        {
            _logger.LogInformation(
                "AzureTokenItem.Refresh: scopes = '{scopes:l}', tenantId = '{tenantId:l}', claims = '{claims:l}', isCaeEnabled = '{isCaeEnabled}'",
                string.Join(", ", _tokenRequestContextKey.Scopes),
                _tokenRequestContextKey.TenantId,
                _tokenRequestContextKey.Claims,
                _tokenRequestContextKey.IsCaeEnabled);

            // assume we need to refresh in 5 seconds
            var dueTime = TimeSpan.FromSeconds(5);

            try
            {
                // get a new token and set
                var azureToken = _tokenCredential.GetToken(
                    requestContext: _tokenRequestContextKey.ToTokenRequestContext(),
                    cancellationToken: default);

                SetAzureToken(azureToken);

                // we got a token - schedule refresh
                // workloadIdentityCredential will have RefreshOn set - use RefreshOn
                // azureCliCredential will not - use ExpiresOn
                var refreshOn = azureToken.RefreshOn ?? azureToken.ExpiresOn;

                _logger.LogInformation(
                    "AzureTokenItem.AccessToken: scopes = '{scopes:l}', tenantId = '{tenantId:l}', claims = '{claims:l}', isCaeEnabled = '{isCaeEnabled}', refreshOn = '{refreshOn:o}'.",
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
                    "AzureTokenItem.Unavailable: scopes = '{scopes:l}', tenantId = '{tenantId:l}', claims = '{claims:l}', isCaeEnabled = '{isCaeEnabled}', message = '{message:}'.",
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
        /// Sets the <see cref="Azure.Core.AccessToken"/> for this object.
        /// </summary>
        private void SetAzureToken(
            AzureToken azureToken)
        {
            lock (this)
            {
                _azureToken = azureToken;

                _unavailableMessage = null;
                _unavailableInnerException = null;
            }
        }

        /// <summary>
        /// Sets the message to throw when the token is unavailable.
        /// </summary>
        /// <param name="ex">The <see cref="CredentialUnavailableException"/> with the message to throw when the token is unavailable.</param>
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
