namespace Trelnex.Core.Identity;

public interface ICredentialStatusProvider
{
    /// <summary>
    /// Gest the name of the credential used by this token provider.
    /// </summary>
    string CredentialName { get; }

    /// <summary>
    /// Gets the <see cref="CredentialStatus"/> of the credential used by this token provider.
    /// </summary>
    /// <returns>The <see cref="CredentialStatus"/> of the credential used by this token provider.</returns>
    CredentialStatus GetStatus();
}
