using System.Text.Json;

namespace Trelnex.Core.Encryption;

/// <summary>
/// Factory class responsible for creating encryption service instances based on the specified algorithm and configuration.
/// This factory implements the Factory pattern to abstract the creation of different encryption services.
/// </summary>
public class EncryptionServiceFactory
{
    /// <summary>
    /// Gets the encryption algorithm used for creating the encryption service.
    /// </summary>
    /// <value>The encryption algorithm that determines which encryption service implementation to create.</value>
    private EncryptionAlgorithm Algorithm { get; init; }

    /// <summary>
    /// Gets the configuration settings specific to the encryption algorithm.
    /// </summary>
    /// <value>
    /// A dictionary containing the configuration settings as key-value pairs that will be serialized to JSON
    /// and deserialized to the algorithm-specific configuration type. Can be null if no settings are required.
    /// </value>
    private Dictionary<string, object>? Settings { get; init; }

    /// <summary>
    /// Creates and returns a configured encryption service instance based on the algorithm and settings provided during factory initialization.
    /// </summary>
    /// <returns>A fully configured <see cref="IEncryptionService"/> instance ready for encryption and decryption operations.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when:
    /// - The settings parameter is not a valid JSON object
    /// - The configuration cannot be deserialized to the expected configuration type
    /// - The encryption service instance cannot be created or cast to <see cref="IEncryptionService"/>
    /// </exception>
    /// <exception cref="NotSupportedException">Thrown when the specified encryption algorithm is not supported by this factory.</exception>
    public IEncryptionService GetEncryptionService()
    {
        var (encryptionConfigurationType, encryptionServiceType) = GetEncryptionTypes();

        var json = JsonSerializer.Serialize(Settings);
        var encryptionConfiguration = JsonSerializer.Deserialize(json, encryptionConfigurationType)
            ?? throw new InvalidOperationException("Encryption configuration is not valid");

        var encryptionService = Activator.CreateInstance(encryptionServiceType, encryptionConfiguration);

        return encryptionService as IEncryptionService
            ?? throw new InvalidOperationException($"Failed to create IEncryptionService for {Algorithm}");
    }

    /// <summary>
    /// Determines and returns the appropriate configuration and service types for the specified encryption algorithm.
    /// This method maps each supported algorithm to its corresponding configuration and service implementation types.
    /// </summary>
    /// <returns>
    /// A tuple containing:
    /// - configurationType: The <see cref="Type"/> of the configuration class for the algorithm
    /// - serviceType: The <see cref="Type"/> of the service implementation class for the algorithm
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when the encryption algorithm is not supported or recognized.</exception>
    private (Type configurationType, Type serviceType) GetEncryptionTypes()
    {
        return Algorithm switch
        {
            EncryptionAlgorithm.AesGcm => (typeof(AesGcmEncryptionConfiguration), typeof(AesGcmEncryptionService)),
            _ => throw new NotSupportedException($"Unhandled EncryptionAlgorithm: {Algorithm}")
        };
    }
}
