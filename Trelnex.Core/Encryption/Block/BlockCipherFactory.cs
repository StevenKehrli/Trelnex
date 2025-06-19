namespace Trelnex.Core.Encryption;

/// <summary>
/// Factory class responsible for creating block cipher instances based on the specified cipher name and configuration.
/// This factory implements the Factory pattern to abstract the creation of different block cipher implementations,
/// providing a unified interface for instantiating various IBlockCipher implementations.
/// </summary>
public class BlockCipherFactory
{
    /// <summary>
    /// Creates a new block cipher instance of the specified type with the provided configuration.
    /// </summary>
    /// <param name="blockCipherName">The type of block cipher to create.</param>
    /// <param name="bind">An action that configures the cipher's configuration object.</param>
    /// <returns>A configured block cipher instance implementing IBlockCipher.</returns>
    /// <exception cref="NotSupportedException">Thrown when the specified cipher name is not supported.</exception>
    /// <exception cref="InvalidOperationException">Thrown when cipher creation fails due to configuration or instantiation errors.</exception>
    public static IBlockCipher Create(
        BlockCipherName blockCipherName,
        Action<object> bind)
    {
        var (configurationType, cipherType) = GetTypes(blockCipherName);

        var configuration = Activator.CreateInstance(configurationType)
            ?? throw new InvalidOperationException($"Failed to create configuration for cipher '{blockCipherName}' and type '{configurationType}'");

        bind(configuration);

        var cipher = Activator.CreateInstance(cipherType, configuration)
            ?? throw new InvalidOperationException($"Failed to create IBlockCipher for cipher '{blockCipherName}' and type '{cipherType}'");

        return cipher as IBlockCipher
            ?? throw new InvalidOperationException($"Failed to create IBlockCipher for cipher '{blockCipherName}' and type '{cipherType}'");
    }

    /// <summary>
    /// Gets the configuration and cipher types associated with the specified block cipher name.
    /// </summary>
    /// <param name="blockCipherName">The block cipher name to get types for.</param>
    /// <returns>A tuple containing the configuration type and cipher implementation type.</returns>
    /// <exception cref="NotSupportedException">Thrown when the specified cipher name is not supported.</exception>
    private static (Type configurationType, Type cipherType) GetTypes(
        BlockCipherName blockCipherName)
    {
        return blockCipherName switch
        {
            BlockCipherName.AesGcm => (typeof(AesGcmCipherConfiguration), typeof(AesGcmCipher)),
            _ => throw new NotSupportedException($"Unhandled BlockCipherName '{blockCipherName}'")
        };
    }
}
