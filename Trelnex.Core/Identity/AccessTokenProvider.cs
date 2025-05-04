namespace Trelnex.Core.Identity;

/// <summary>
/// Interface for access token providers.
/// </summary>
public interface IAccessTokenProvider
{
    /// <summary>
    /// Gets the scope of the access token.
    /// </summary>
    string Scope { get; }

    /// <summary>
    /// Gets the <see cref="AccessToken"/>.
    /// </summary>
    /// <returns>The <see cref="AccessToken"/> for authentication.</returns>
    AccessToken GetAccessToken();
}

/// <summary>
/// Implementation of <see cref="IAccessTokenProvider"/> that retrieves tokens from credentials.
/// </summary>
/// <remarks>
/// Manages token lifecycle and caching behavior.
/// </remarks>
public class AccessTokenProvider : IAccessTokenProvider
{
    private readonly ICredential _credential;

    private readonly string _scope;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessTokenProvider"/> class.
    /// </summary>
    /// <param name="credential">The credential to use for token acquisition.</param>
    /// <param name="scope">The scope of permissions to request.</param>
    private AccessTokenProvider(
        ICredential credential,
        string scope)
    {
        _credential = credential;
        _scope = scope;
    }

    /// <summary>
    /// Creates a new <see cref="AccessTokenProvider"/> instance.
    /// </summary>
    /// <param name="credential">The credential to use for token acquisition.</param>
    /// <param name="scope">The scope of permissions to request.</param>
    /// <returns>An initialized <see cref="AccessTokenProvider"/>.</returns>
    public static AccessTokenProvider Create(
        ICredential credential,
        string scope)
    {
        // Create the provider.
        var accessTokenProvider = new AccessTokenProvider(credential, scope);

        // Warm-up this token by fetching it immediately. This helps to cache the token and avoid initial delays.
        accessTokenProvider.GetAccessToken();

        // Return the created provider.
        return accessTokenProvider;
    }

    /// <inheritdoc />
    public string Scope => _scope;

    /// <inheritdoc />
    public AccessToken GetAccessToken()
    {
        // Retrieve the access token from the credential for the specified scope.
        return _credential.GetAccessToken(_scope);
    }
}
