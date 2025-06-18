namespace Trelnex.Core.Encryption;

/// <summary>
/// Factory class responsible for creating cipher instances based on the specified cipher name and configuration.
/// This factory implements the Factory pattern to abstract the creation of different cipher implementations,
/// providing a unified interface for instantiating various ICipher implementations.
/// </summary>
public class CipherFactory
{
    /// <summary>
    /// Creates a new cipher instance of the specified type with the provided configuration.
    /// </summary>
    /// <param name="cipherName">The type of cipher to create.</param>
    /// <param name="bind">An action that configures the cipher's configuration object.</param>
    /// <returns>A configured cipher instance implementing ICipher.</returns>
    /// <exception cref="NotSupportedException">Thrown when the specified cipher name is not supported.</exception>
    /// <exception cref="InvalidOperationException">Thrown when cipher creation fails due to configuration or instantiation errors.</exception>
    public static ICipher Create(
        CipherName cipherName,
        Action<object> bind)
    {
        var (configurationType, cipherType) = GetTypes(cipherName);

        var configuration = Activator.CreateInstance(configurationType)
            ?? throw new InvalidOperationException($"Failed to create ICipherConfiguration for cipher '{cipherName}' and type '{configurationType}'");

        bind(configuration);

        var cipher = Activator.CreateInstance(cipherType, configuration)
            ?? throw new InvalidOperationException($"Failed to create ICipher for cipher '{cipherName}' and type '{cipherType}'");

        return cipher as ICipher
            ?? throw new InvalidOperationException($"Failed to create ICipher for cipher '{cipherName}' and type '{cipherType}'");
    }

    /// <summary>
    /// Gets the configuration and cipher types associated with the specified cipher name.
    /// </summary>
    /// <param name="cipherName">The cipher name to get types for.</param>
    /// <returns>A tuple containing the configuration type and cipher implementation type.</returns>
    /// <exception cref="NotSupportedException">Thrown when the specified cipher name is not supported.</exception>
    private static (Type configurationType, Type cipherType) GetTypes(
        CipherName cipherName)
    {
        return cipherName switch
        {
            CipherName.AesGcm => (typeof(AesGcmCipherConfiguration), typeof(AesGcmCipher)),
            _ => throw new NotSupportedException($"Unhandled CipherName '{cipherName}'")
        };
    }
}
