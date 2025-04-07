namespace Trelnex.Core.Identity;

public interface IAccessTokenProvider
{
    /// <summary>
    /// Gets the scope of the access token
    /// </summary>
    string Scope { get; }

    /// <summary>
    /// Gets the <see cref="AccessToken"/>.
    /// </summary>
    /// <returns>The <see cref="AccessToken"/>.</returns>
    AccessToken GetAccessToken();
}

public class AccessTokenProvider : IAccessTokenProvider
{
    private readonly ICredential _credential;

    private readonly string _scope;

    private AccessTokenProvider(
        ICredential credential,
        string scope)
    {
        _credential = credential;
        _scope = scope;
    }

    public static AccessTokenProvider Create(
        ICredential credential,
        string scope)
    {
        // create the provider
        var accessTokenProvider = new AccessTokenProvider(credential, scope);

        // warm-up this token
        accessTokenProvider.GetAccessToken();

        return accessTokenProvider;
    }

    public string Scope => _scope;

    public AccessToken GetAccessToken()
    {
        return _credential.GetAccessToken(_scope);
    }
}
