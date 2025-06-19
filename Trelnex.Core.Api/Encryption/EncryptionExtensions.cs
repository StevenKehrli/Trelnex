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
    /// Creates and configures a block cipher service instance based on configuration values.
    /// This method reads the cipher configuration from an "Encryption" section and uses the <see cref="BlockCipherFactory"/>
    /// to create the appropriate ciphers with configuration binding, then wraps them in a <see cref="BlockCipherService"/>.
    /// </summary>
    /// <param name="configuration">The configuration instance containing encryption settings under an "Encryption" section.</param>
    /// <returns>
    /// A configured <see cref="IBlockCipherService"/> implementation with the primary cipher and any secondary ciphers
    /// specified in configuration, or <see langword="null"/> if no primary cipher is configured.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the cipher cannot be created or configured properly.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the cipher name specified in configuration is not supported by the <see cref="BlockCipherFactory"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The method expects an "Encryption" configuration section with:
    /// - "Primary" subsection containing a "CipherName" value that maps to a valid <see cref="BlockCipherName"/> enum value
    /// - Optional "Secondary" subsection with child sections, each containing their own "CipherName" values
    /// </para>
    /// <para>
    /// Additional configuration properties are bound to the cipher instances using the provided configuration,
    /// with non-public properties bound and unknown configuration errors ignored.
    /// </para>
    /// <para>
    /// The returned <see cref="BlockCipherService"/> is configured with the created primary cipher
    /// and any valid secondary ciphers for key rotation scenarios.
    /// </para>
    /// </remarks>
    public static IBlockCipherService? CreateBlockCipherService(
        this IConfiguration configuration)
    {
        var encryptionSection = configuration.GetSection("Encryption");

        var primaryCipher = encryptionSection
            .GetSection("Primary")
            .CreateBlockCipher();

        if (primaryCipher is null) return null;

        var secondaryCiphers = encryptionSection
            .GetSection("Secondary")
            .GetChildren()
            .Select(section => section.CreateBlockCipher())
            .OfType<IBlockCipher>()
            .ToArray();

        return new BlockCipherService(
            primaryCipher: primaryCipher,
            secondaryCiphers: secondaryCiphers);
    }

    /// <summary>
    /// Creates a block cipher instance from configuration.
    /// </summary>
    /// <param name="configuration">Configuration section containing cipher settings.</param>
    /// <returns>A configured <see cref="IBlockCipher"/> instance, or <see langword="null"/> if no CipherName is specified.</returns>
    private static IBlockCipher? CreateBlockCipher(
        this IConfiguration configuration)
    {
        var blockCipherName = configuration.GetValue<BlockCipherName?>("CipherName");

        if (blockCipherName.HasValue is false) return null;

        var cipher = BlockCipherFactory.Create(
            blockCipherName.Value,
            instance => configuration.Bind(instance, options =>
            {
                options.BindNonPublicProperties = true;
                options.ErrorOnUnknownConfiguration = false;
            }));

        return cipher;
    }
}
