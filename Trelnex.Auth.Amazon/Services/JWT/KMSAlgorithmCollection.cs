using System.Configuration;
using Amazon;
using Amazon.Runtime;
using Trelnex.Core.Identity;

namespace Trelnex.Auth.Amazon.Services.JWT;

/// <summary>
/// Manages a collection of KMS signing algorithms for JWT token generation.
/// </summary>
/// <remarks>
/// This class handles the initialization and organization of KMS algorithms
/// based on their intended usage (default, regional, or secondary). It provides
/// validation of KMS key configurations and ensures proper initialization of
/// all required algorithms.
/// </remarks>
internal class KMSAlgorithmCollection
{
    #region Private Fields

    /// <summary>
    /// The primary KMS algorithm used for JWT token signing.
    /// </summary>
    private readonly KMSAlgorithm _defaultAlgorithm;

    /// <summary>
    /// Optional region-specific KMS algorithms for JWT token signing.
    /// </summary>
    /// <remarks>
    /// Each algorithm is specific to an AWS region and is used for
    /// signing tokens when the client is in the same region.
    /// </remarks>
    private readonly KMSAlgorithm[]? _regionalAlgorithms;

    /// <summary>
    /// Optional backup KMS algorithms for JWT token signing.
    /// </summary>
    /// <remarks>
    /// These algorithms are used as fallbacks in case the primary
    /// or regional algorithms are unavailable.
    /// </remarks>
    private readonly KMSAlgorithm[]? _secondaryAlgorithms;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="KMSAlgorithmCollection"/> class.
    /// </summary>
    /// <param name="defaultAlgorithm">The primary KMS algorithm for JWT token signing.</param>
    /// <param name="regionalAlgorithms">Optional region-specific KMS algorithms for JWT token signing.</param>
    /// <param name="secondaryAlgorithms">Optional backup KMS algorithms for JWT token signing.</param>
    private KMSAlgorithmCollection(
        KMSAlgorithm defaultAlgorithm,
        KMSAlgorithm[]? regionalAlgorithms,
        KMSAlgorithm[]? secondaryAlgorithms)
    {
        // Set the KMS algorithms.
        _defaultAlgorithm = defaultAlgorithm;
        _regionalAlgorithms = regionalAlgorithms;
        _secondaryAlgorithms = secondaryAlgorithms;
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates a new <see cref="KMSAlgorithmCollection"/> with the specified KMS keys.
    /// </summary>
    /// <param name="bootstrapLogger">Logger for recording initialization information.</param>
    /// <param name="credentialProvider">Provider for AWS credentials needed to access KMS keys.</param>
    /// <param name="defaultKey">The ARN of the default KMS key for JWT token signing.</param>
    /// <param name="regionalKeys">Optional ARNs of region-specific KMS keys for JWT token signing.</param>
    /// <param name="secondaryKeys">Optional ARNs of backup KMS keys for JWT token signing.</param>
    /// <returns>A new <see cref="KMSAlgorithmCollection"/> instance.</returns>
    /// <exception cref="AggregateException">Thrown when validation or initialization of KMS keys fails.</exception>
    /// <remarks>
    /// This method performs the following steps:
    /// 1. Validates the provided KMS key configurations
    /// 2. Initializes KMS algorithm instances for each key
    /// 3. Logs information about the initialized algorithms
    /// 4. Returns a collection containing all successfully initialized algorithms
    ///
    /// If any key fails validation or initialization, an AggregateException containing
    /// all encountered errors is thrown.
    /// </remarks>
    public static KMSAlgorithmCollection Create(
        ILogger bootstrapLogger,
        ICredentialProvider<AWSCredentials> credentialProvider,
        string defaultKey,
        string[]? regionalKeys = null,
        string[]? secondaryKeys = null)
    {
        // Collection of validation and initialization errors.
        var exs = new List<ConfigurationErrorsException>();

        // Get the AWS credentials for KMS API calls.
        var credentials = credentialProvider.GetCredential();

        // Parse and validate the KMS key configurations.
        var algorithmResources = KMSAlgorithmResources.GetResources(
            defaultKey,
            regionalKeys,
            secondaryKeys);

        // Initialize the default algorithm.
        var defaultAlgorithmTask = (
            keyArn: algorithmResources.DefaultKey.keyArn,
            algorithmTask: KMSAlgorithm.CreateAsync(
                credentials,
                algorithmResources.DefaultKey.regionEndpoint,
                algorithmResources.DefaultKey.keyArn));

        // Initialize the regional algorithms (if specified).
        var regionalAlgorithmTasks = algorithmResources
            .RegionalKeys?
            .Select(regionalKey => (
                keyArn: regionalKey.keyArn,
                algorithmTask: KMSAlgorithm.CreateAsync(
                    credentials,
                    regionalKey.regionEndpoint,
                    regionalKey.keyArn)))
            .ToArray();

        // Initialize the secondary algorithms (if specified).
        var secondaryAlgorithmTasks = algorithmResources
            .SecondaryKeys?
            .Select(secondaryKey => (
                keyArn: secondaryKey.keyArn,
                algorithmTask: KMSAlgorithm.CreateAsync(
                    credentials,
                    secondaryKey.regionEndpoint,
                    secondaryKey.keyArn)))
            .ToArray();

        // Verify the default algorithm was successfully initialized.
        try
        {
            _ = defaultAlgorithmTask.algorithmTask.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            // Add any exceptions to the list of configuration errors.
            exs.Add(
                new ConfigurationErrorsException(
                    $"The DefaultKey '{defaultAlgorithmTask.keyArn}' is not valid.",
                    ex.InnerException ?? ex));
        }

        // Verify the regional algorithms were successfully initialized.
        Array.ForEach(regionalAlgorithmTasks ?? [], regionalAlgorithmTask =>
        {
            try
            {
                _ = regionalAlgorithmTask.algorithmTask.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // Add any exceptions to the list of configuration errors.
                exs.Add(
                    new ConfigurationErrorsException(
                        $"The RegionalKey '{regionalAlgorithmTask.keyArn}' is not valid.",
                        ex.InnerException ?? ex));
            }
        });

        // Verify the secondary algorithms were successfully initialized.
        Array.ForEach(secondaryAlgorithmTasks ?? [], secondaryAlgorithmTask =>
        {
            try
            {
                _ = secondaryAlgorithmTask.algorithmTask.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // Add any exceptions to the list of configuration errors.
                exs.Add(
                    new ConfigurationErrorsException(
                        $"The SecondaryKey '{secondaryAlgorithmTask.keyArn}' is not valid.",
                        ex.InnerException ?? ex));
            }
        });

        // If any errors occurred during validation or initialization, throw them all.
        if (exs.Count > 0)
        {
            throw new AggregateException(exs);
        }

        // Get the fully initialized algorithms.
        var defaultAlgorithm = defaultAlgorithmTask.algorithmTask.GetAwaiter().GetResult();

        var regionalAlgorithms = regionalAlgorithmTasks?
            .Select(task => task.algorithmTask.Result)
            .ToArray();

        var secondaryAlgorithms = secondaryAlgorithmTasks?
            .Select(task => task.algorithmTask.Result)
            .ToArray();

        // Log information about the initialized algorithms.
        // The :l format parameter (l = literal) is used to avoid quoting issues in log output.
        bootstrapLogger.LogInformation(
            message: "Added Default KMSAlgorithm: kid = '{kid:l}', keyArn = '{keyArn:l}'.",
            args: [
                defaultAlgorithm.JWK.KeyId,
                defaultAlgorithm.KeyArn ]);

        Array.ForEach(regionalAlgorithms ?? [], regionalAlgorithm =>
        {
            bootstrapLogger.LogInformation(
                message: "Added Regional KMSAlgorithm: region = '{region:l}', kid '{kid:l}', keyArn = '{keyArn:l}'.",
                args: [
                    regionalAlgorithm.RegionEndpoint.SystemName,
                    regionalAlgorithm.JWK.KeyId,
                    regionalAlgorithm.KeyArn ]);
        });

        Array.ForEach(secondaryAlgorithms ?? [], secondaryAlgorithm =>
        {
            bootstrapLogger.LogInformation(
                message: "Added Secondary KMSAlgorithm: region = '{region:l}', kid = '{kid:l}', keyArn = '{keyArn:l}'.",
                args: [
                    secondaryAlgorithm.RegionEndpoint.SystemName,
                    secondaryAlgorithm.JWK.KeyId,
                    secondaryAlgorithm.KeyArn ]);
        });

        // Create and return the collection with the initialized algorithms.
        return new KMSAlgorithmCollection(
            defaultAlgorithm,
            regionalAlgorithms,
            secondaryAlgorithms);
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the primary KMS algorithm used for JWT token signing.
    /// </summary>
    public KMSAlgorithm DefaultAlgorithm => _defaultAlgorithm;

    /// <summary>
    /// Gets the optional region-specific KMS algorithms for JWT token signing.
    /// </summary>
    /// <remarks>
    /// These algorithms are matched to the client's region to provide
    /// region-specific token signing capabilities.
    /// </remarks>
    public KMSAlgorithm[]? RegionalAlgorithms => _regionalAlgorithms;

    /// <summary>
    /// Gets the optional backup KMS algorithms for JWT token signing.
    /// </summary>
    /// <remarks>
    /// These algorithms are used when the default or regional algorithms
    /// are unavailable due to service disruptions or other issues.
    /// </remarks>
    public KMSAlgorithm[]? SecondaryAlgorithms => _secondaryAlgorithms;

    #endregion

    #region Nested Types

    /// <summary>
    /// Helper class for validating and organizing KMS key resources.
    /// </summary>
    /// <remarks>
    /// This class handles the parsing and validation of KMS key ARNs,
    /// organizing them into appropriate categories (default, regional, secondary),
    /// and ensuring they meet requirements for proper operation.
    /// </remarks>
    private class KMSAlgorithmResources
    {
        #region Public Properties

        /// <summary>
        /// Gets the region endpoint and ARN for the default KMS key.
        /// </summary>
        public (RegionEndpoint regionEndpoint, string keyArn) DefaultKey { get; init; }

        /// <summary>
        /// Gets the region endpoints and ARNs for the regional KMS keys.
        /// </summary>
        public (RegionEndpoint regionEndpoint, string keyArn)[]? RegionalKeys { get; init; }

        /// <summary>
        /// Gets the region endpoints and ARNs for the secondary KMS keys.
        /// </summary>
        public (RegionEndpoint regionEndpoint, string keyArn)[]? SecondaryKeys { get; init; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="KMSAlgorithmResources"/> class.
        /// </summary>
        /// <param name="defaultKey">The region endpoint and ARN for the default KMS key.</param>
        /// <param name="regionalKeys">The region endpoints and ARNs for the regional KMS keys.</param>
        /// <param name="secondaryKeys">The region endpoints and ARNs for the secondary KMS keys.</param>
        private KMSAlgorithmResources(
            (RegionEndpoint regionEndpoint, string keyArn) defaultKey,
            (RegionEndpoint regionEndpoint, string keyArn)[]? regionalKeys,
            (RegionEndpoint regionEndpoint, string keyArn)[]? secondaryKeys)
        {
            // Set the KMS key resources.
            DefaultKey = defaultKey;
            RegionalKeys = regionalKeys;
            SecondaryKeys = secondaryKeys;
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Validates and organizes KMS key resources from key ARNs.
        /// </summary>
        /// <param name="defaultKey">The ARN of the default KMS key.</param>
        /// <param name="regionalKeys">Optional ARNs of region-specific KMS keys.</param>
        /// <param name="secondaryKeys">Optional ARNs of backup KMS keys.</param>
        /// <returns>A <see cref="KMSAlgorithmResources"/> instance containing the validated keys.</returns>
        /// <exception cref="AggregateException">Thrown when validation of key configurations fails.</exception>
        /// <remarks>
        /// This method performs several validation checks:
        /// - Ensures all key ARNs are valid and contain region information
        /// - Verifies there is only one key per region in the regional keys
        /// - Ensures keys are not duplicated across different categories
        ///
        /// If any validation check fails, an AggregateException containing all
        /// validation errors is thrown.
        /// </remarks>
        public static KMSAlgorithmResources GetResources(
            string defaultKey,
            string[]? regionalKeys,
            string[]? secondaryKeys)
        {
            // Collection of validation errors.
            var exs = new List<ConfigurationErrorsException>();

            // Parse and validate the default key.
            var defaultKeyTuple = (
                regionEndpoint: KeyArnUtilities.GetRegion(defaultKey),
                keyArn: defaultKey);

            // If the default key is invalid, add an error to the list.
            if (defaultKeyTuple.regionEndpoint is null)
            {
                exs.Add(new ConfigurationErrorsException($"The DefaultKey '{defaultKeyTuple.keyArn}' is invalid."));
            }

            // Parse and validate the regional keys.
            var regionalKeyTuples = regionalKeys?
                .Select(key => (
                    regionEndpoint: KeyArnUtilities.GetRegion(key),
                    keyArn: key))
                .ToArray();

            // If any regional keys are invalid, add an error to the list.
            Array.ForEach(regionalKeyTuples ?? [], regionalKeyTuple =>
            {
                if (regionalKeyTuple.regionEndpoint is null)
                {
                    exs.Add(new ConfigurationErrorsException($"The RegionalKey '{regionalKeyTuple.keyArn}' is invalid."));
                }
            });

            // Verify there is only one key per region in the regional keys.
            var regionalKeyGroups = regionalKeyTuples?
                .Where(regionalKeyTuple => regionalKeyTuple.regionEndpoint is not null)
                .GroupBy(regionalKeyTuple => regionalKeyTuple.regionEndpoint)
                .ToArray();

            // If any region has more than one key, add an error to the list.
            Array.ForEach(regionalKeyGroups ?? [], group =>
            {
                if (group.Count() <= 1) return;

                exs.Add(new ConfigurationErrorsException($"A RegionalKey for Region '{group.Key?.SystemName}' is specified more than once."));
            });

            // Verify regional keys do not duplicate the default key.
            Array.ForEach(regionalKeyTuples ?? [], regionalKeyTuple =>
            {
                // If a regional key is the same as the default key, add an error to the list.
                if (defaultKeyTuple.keyArn == regionalKeyTuple.keyArn)
                {
                    exs.Add(new ConfigurationErrorsException($"The RegionalKey '{regionalKeyTuple.keyArn}' is previously configured as the DefaultKey."));
                }
            });

            // Parse and validate the secondary keys.
            var secondaryKeyTuples = secondaryKeys?
                .Select(key => (
                    regionEndpoint: KeyArnUtilities.GetRegion(key),
                    keyArn: key))
                .ToArray();

            // If any secondary keys are invalid, add an error to the list.
            Array.ForEach(secondaryKeyTuples ?? [], secondaryKeyTuple =>
            {
                if (secondaryKeyTuple.regionEndpoint is null)
                {
                    exs.Add(new ConfigurationErrorsException($"The SecondaryKey '{secondaryKeyTuple.keyArn}' is invalid."));
                }
            });

            // Verify there are no duplicate secondary keys.
            var secondaryKeyGroups = secondaryKeyTuples?
                .Where(secondaryKeyTuple => secondaryKeyTuple.regionEndpoint is not null)
                .GroupBy(secondaryKeyTuple => secondaryKeyTuple.keyArn)
                .ToArray();

            // If any secondary key is specified more than once, add an error to the list.
            Array.ForEach(secondaryKeyGroups ?? [], group =>
            {
                if (group.Count() <= 1) return;

                exs.Add(new ConfigurationErrorsException($"A SecondaryKey '{group.Key} is specified more than once."));
            });

            // Verify secondary keys do not duplicate default or regional keys.
            Array.ForEach(secondaryKeyTuples ?? [], secondaryKeyTuple =>
            {
                // If a secondary key is the same as the default key, add an error to the list.
                if (defaultKeyTuple.keyArn == secondaryKeyTuple.keyArn)
                {
                    exs.Add(new ConfigurationErrorsException($"The SecondaryKey '{secondaryKeyTuple.keyArn}' is previously configured as the DefaultKey."));
                }

                // If a secondary key is the same as a regional key, add an error to the list.
                if (regionalKeyTuples?.Any(regionalKeyTuple => regionalKeyTuple == secondaryKeyTuple) is true)
                {
                    exs.Add(new ConfigurationErrorsException($"The SecondaryKey '{secondaryKeyTuple.keyArn}' is previously configured as a RegionalKey."));
                }
            });

            // If any validation errors occurred, throw them all.
            if (exs.Count > 0)
            {
                throw new AggregateException(exs);
            }

            // Create and return the resources with the validated keys.
            return new KMSAlgorithmResources(
                defaultKey: (defaultKeyTuple.regionEndpoint!, defaultKeyTuple.keyArn!),
                regionalKeys: regionalKeyTuples?
                    .Select(regionalKeyTuple => (
                        regionalKeyTuple.regionEndpoint!,
                        regionalKeyTuple.keyArn!))
                    .ToArray(),
                secondaryKeys: secondaryKeyTuples?
                    .Select(secondaryKeyTuple => (
                        secondaryKeyTuple.regionEndpoint!,
                        secondaryKeyTuple.keyArn!))
                    .ToArray());
        }

        #endregion
    }

    #endregion
}
