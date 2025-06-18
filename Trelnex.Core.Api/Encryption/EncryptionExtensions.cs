using Microsoft.Extensions.Configuration;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Api.Encryption;

/// <summary>
/// Provides extension methods for <see cref="IConfiguration"/> to create encryption services.
/// These extensions simplify the process of creating and configuring encryption services from configuration data.
/// </summary>
public static class EncryptionExtensions
{
    /// <summary>
    /// Creates and configures an encryption service instance based on configuration values.
    /// This method reads the cipher name from configuration and uses the <see cref="CipherFactory"/>
    /// to create the appropriate cipher with configuration binding, then wraps it in an <see cref="EncryptionService"/>.
    /// </summary>
    /// <param name="configuration">The configuration instance containing encryption settings, including the CipherName.</param>
    /// <returns>
    /// A configured <see cref="IEncryptionService"/> implementation with the primary cipher specified in configuration,
    /// or <see langword="null"/> if no CipherName is configured.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the cipher cannot be created or configured properly.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the cipher name specified in configuration is not supported by the <see cref="CipherFactory"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The method expects a "CipherName" configuration value that maps to a valid <see cref="CipherName"/> enum value.
    /// If this value is not present or is null, the method returns <see langword="null"/>.
    /// </para>
    /// <para>
    /// Additional configuration properties are bound to the cipher instance using the provided configuration,
    /// with non-public properties bound and unknown configuration errors ignored.
    /// </para>
    /// <para>
    /// The returned <see cref="EncryptionService"/> is configured with the created cipher as the primary cipher
    /// and no secondary ciphers.
    /// </para>
    /// </remarks>
    public static IEncryptionService? CreateEncryptionService(
        this IConfiguration configuration)
    {
        var cipherName = configuration.GetValue<CipherName?>("CipherName");

        if (cipherName.HasValue is false) return null;

        var cipher = CipherFactory.Create(
            cipherName.Value,
            instance => configuration.Bind(instance, options =>
            {
                options.BindNonPublicProperties = true;
                options.ErrorOnUnknownConfiguration = false;
            }));

        return new EncryptionService(
            primaryCipher: cipher,
            secondaryCiphers: null);
    }
}
