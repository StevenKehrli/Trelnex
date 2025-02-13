using System.Configuration;
using Amazon;
using Amazon.Runtime;
using Microsoft.IdentityModel.Tokens;
using Trelnex.Core.Identity;

namespace Trelnex.Auth.Amazon.Services.JWT;

internal class KMSAlgorithmProvider
{
    private readonly IKMSAlgorithm _defaultAlgorithm;

    private readonly Dictionary<RegionEndpoint, IKMSAlgorithm> _regionalAlgorithms;

    private readonly JsonWebKeySet _jwks;

    private KMSAlgorithmProvider(
        KMSAlgorithm defaultAlgorithm,
        KMSAlgorithm[]? regionalAlgorithms,
        JsonWebKeySet jwks)
    {
        // set the default algorithm
        _defaultAlgorithm = defaultAlgorithm;

        // set the regional algorithms
        _regionalAlgorithms = regionalAlgorithms?
            .ToDictionary(
                algorithm => algorithm.Region,
                algorithm => algorithm as IKMSAlgorithm) ?? [];

        _jwks = jwks;
    }

    /// <summary>
    /// Get the json web key set
    /// </summary>
    public JsonWebKeySet JWKS => _jwks;

    /// <summary>
    /// Creates a new instance of the <see cref="KMSAlgorithmProvider"/>.
    /// </summary>
    /// <param name="bootstrapLogger">The <see cref="ILogger"/> to write the KMSAlgorithmProvider bootstrap logs.</param>
    /// <param name="credentialProvider">The credential provider to get the AWS credentials.</param>
    /// <param name="algorithmConfiguration">The <see cref="KMSAlgorithmConfiguration"/> for the KMS algorithms.</param>
    /// <returns>The <see cref="KMSAlgorithmProvider"/>.</returns>
    public static KMSAlgorithmProvider Create(
        ILogger bootstrapLogger,
        ICredentialProvider<AWSCredentials> credentialProvider,
        KMSAlgorithmConfiguration algorithmConfiguration)
    {
        // get the aws credentials
        var credentials = credentialProvider.GetCredential();

        // parse the configuration into the resources
        var algorithmResources = KMSAlgorithmResources.Parse(algorithmConfiguration);

        // create the collection of kms algorithms
        var algorithmCollection = CreateAlgorithmCollection(
            bootstrapLogger,
            credentials,
            algorithmResources);

        // create the json web key set
        var jwks = new JsonWebKeySet();

        // add the default jwk to the set
        jwks.Keys.Add(algorithmCollection.DefaultAlgorithm.JWK);

        // add the regional jwks to the set
        Array.ForEach(algorithmCollection.RegionalAlgorithms ?? [], algorithm =>
        {
            jwks.Keys.Add(algorithm.JWK);
        });

        // add the secondary jwks to the set
        Array.ForEach(algorithmCollection.SecondaryAlgorithms ?? [], algorithm =>
        {
            jwks.Keys.Add(algorithm.JWK);
        });

        // create the algorithm provider
        return new KMSAlgorithmProvider(
            algorithmCollection.DefaultAlgorithm,
            algorithmCollection.RegionalAlgorithms,
            jwks);
    }

    /// <summary>
    /// Get the algorithm for the region.
    /// </summary>
    /// <param name="region">The region.</param>
    /// <returns>The algorithm.</returns>
    public IKMSAlgorithm GetAlgorithm(
        RegionEndpoint region)
    {
        // get the algorithm
        return _regionalAlgorithms.TryGetValue(region, out var algorithm)
            ? algorithm
            : _defaultAlgorithm;
    }

    private static KMSAlgorithmCollection CreateAlgorithmCollection(
        ILogger bootstrapLogger,
        AWSCredentials credentials,
        KMSAlgorithmResources algorithmResources)
    {
        // any exceptions
        var exs = new List<ConfigurationErrorsException>();

        // get the default algorithm
        var defaultAlgorithmTask = (
            keyArn: algorithmResources.DefaultKey.keyArn,
            algorithmTask: KMSAlgorithm.CreateAsync(
                credentials,
                algorithmResources.DefaultKey.region,
                algorithmResources.DefaultKey.keyArn));

        // get the regional algorithms
        var regionalAlgorithmTasks = algorithmResources
            .RegionalKeys?
            .Select(regionalKey => (
                keyArn: regionalKey.keyArn,
                algorithmTask: KMSAlgorithm.CreateAsync(
                    credentials,
                    regionalKey.region,
                    regionalKey.keyArn)))
            .ToArray();

        // get the secondary algorithms
        var secondaryAlgorithmTasks = algorithmResources
            .SecondaryKeys?
            .Select(secondaryKey => (
                keyArn: secondaryKey.keyArn,
                algorithmTask: KMSAlgorithm.CreateAsync(
                    credentials,
                    secondaryKey.region,
                    secondaryKey.keyArn)))
            .ToArray();

        // check the default algorithm was successful
        try
        {
            _ = defaultAlgorithmTask.algorithmTask.Result;
        }
        catch (Exception ex)
        {
            exs.Add(
                new ConfigurationErrorsException(
                    $"The DefaultKey '{defaultAlgorithmTask.keyArn}' is not valid.",
                    ex.InnerException ?? ex));
        }

        // check the regional algorithms were successful
        Array.ForEach(regionalAlgorithmTasks ?? [], regionalAlgorithmTask =>
        {
            try
            {
                _ = regionalAlgorithmTask.algorithmTask.Result;
            }
            catch (Exception ex)
            {
                exs.Add(
                    new ConfigurationErrorsException(
                        $"The RegionalKey '{regionalAlgorithmTask.keyArn}' is not valid.",
                        ex.InnerException ?? ex));
            }
        });

        // check the secondary algorithms were successful
        Array.ForEach(secondaryAlgorithmTasks ?? [], secondaryAlgorithmTask =>
        {
            try
            {
                _ = secondaryAlgorithmTask.algorithmTask.Result;
            }
            catch (Exception ex)
            {
                exs.Add(
                    new ConfigurationErrorsException(
                        $"The SecondaryKey '{secondaryAlgorithmTask.keyArn}' is not valid.",
                        ex.InnerException ?? ex));
            }
        });

        // if there are any exceptions, then throw an aggregate exception of all exceptions
        if (exs.Count > 0)
        {
            throw new AggregateException(exs);
        }

        // get the algorithms
        var defaultAlgorithm = defaultAlgorithmTask.algorithmTask.Result;

        var regionalAlgorithms = regionalAlgorithmTasks?
            .Select(task => task.algorithmTask.Result)
            .ToArray();

        var secondaryAlgorithms = secondaryAlgorithmTasks?
            .Select(task => task.algorithmTask.Result)
            .ToArray();

        // log - the :l format parameter (l = literal) to avoid the quotes
        bootstrapLogger.LogInformation(
            message: "Added Default KMSAlgorithm: kid = '{kid:l}', arn = '{keyArn:l}'.",
            args: [
                defaultAlgorithm.JWK.KeyId,
                defaultAlgorithm.KeyArn ]);

        Array.ForEach(regionalAlgorithms ?? [], regionalAlgorithm =>
        {
            bootstrapLogger.LogInformation(
                message: "Added Regional KMSAlgorithm: region = '{region:l}', kid '{kid:l}', arn = '{keyArn:l}'.",
                args: [
                    regionalAlgorithm.Region.SystemName,
                    regionalAlgorithm.JWK.KeyId,
                    regionalAlgorithm.KeyArn ]);
        });

        Array.ForEach(secondaryAlgorithms ?? [], secondaryAlgorithm =>
        {
            bootstrapLogger.LogInformation(
                message: "Added Secondary KMSAlgorithm: region = '{region:l}', kid = '{kid:l}', arn = '{keyArn:l}'.",
                args: [
                    secondaryAlgorithm.Region.SystemName,
                    secondaryAlgorithm.JWK.KeyId,
                    secondaryAlgorithm.KeyArn ]);
        });

        // create the collection
        return new KMSAlgorithmCollection
        {
            DefaultAlgorithm = defaultAlgorithm,
            RegionalAlgorithms = regionalAlgorithms,
            SecondaryAlgorithms = secondaryAlgorithms
        };
    }

    private record KMSAlgorithmCollection
    {
        public required KMSAlgorithm DefaultAlgorithm { get; init; }

        public KMSAlgorithm[]? RegionalAlgorithms { get; init; }

        public KMSAlgorithm[]? SecondaryAlgorithms { get; init; }
    }

    private class KMSAlgorithmResources
    {
        public (RegionEndpoint region, string keyArn) DefaultKey { get; init; }

        public (RegionEndpoint region, string keyArn)[]? RegionalKeys { get; init; }

        public (RegionEndpoint region, string keyArn)[]? SecondaryKeys { get; init; }

        private KMSAlgorithmResources(
            (RegionEndpoint region, string keyArn) defaultKey,
            (RegionEndpoint region, string keyArn)[]? regionalKeys,
            (RegionEndpoint region, string keyArn)[]? secondaryKeys)
        {
            DefaultKey = defaultKey;
            RegionalKeys = regionalKeys;
            SecondaryKeys = secondaryKeys;
        }

        /// <summary>
        /// Parse the <see cref="KMSAlgorithmConfiguration"/> into a <see cref="KMSAlgorithmResources"/>.
        /// </summary>
        /// <param name="algorithmConfiguration">The <see cref="KMSAlgorithmConfiguration"/> to parse.</param>
        /// <returns></returns>
        /// <exception cref="AggregateException">An aggregate exception of all errors that occurred during validation.</exception>
        public static KMSAlgorithmResources Parse(
            KMSAlgorithmConfiguration algorithmConfiguration)
        {
            // any exceptions
            var exs = new List<ConfigurationErrorsException>();

            // parse the default key
            var defaultKey = (
                region: KeyArnUtilities.GetRegion(algorithmConfiguration.DefaultKey),
                keyArn: algorithmConfiguration.DefaultKey);

            if (defaultKey.region is null)
            {
                exs.Add(new ConfigurationErrorsException($"The DefaultKey '{defaultKey.keyArn}' is invalid."));
            }

            // parse the regional keys
            var regionalKeys = algorithmConfiguration
                .RegionalKeys?
                .Select(keyArn => (
                    region: KeyArnUtilities.GetRegion(keyArn),
                    keyArn: keyArn))
                .ToArray();

            Array.ForEach(regionalKeys ?? [], regionalKey =>
            {
                if (regionalKey.region is null)
                {
                    exs.Add(new ConfigurationErrorsException($"The RegionalKey '{regionalKey.keyArn}' is invalid."));
                }
            });

            // group the regional keys by region - only one key per region
            var regionalKeyGroups = regionalKeys?
                .Where(regionalKey => regionalKey.region is not null)
                .GroupBy(regionalKey => regionalKey.region)
                .ToArray();

            // enumerate each group - should be one
            Array.ForEach(regionalKeyGroups ?? [], group =>
            {
                if (group.Count() <= 1) return;

                exs.Add(new ConfigurationErrorsException($"A RegionalKey for Region '{group.Key?.SystemName}' is specified more than once."));
            });

            // enumerate each regional key - should not be the default
            Array.ForEach(regionalKeys ?? [], regionalKey =>
            {
                if (defaultKey.keyArn == regionalKey.keyArn)
                {
                    exs.Add(new ConfigurationErrorsException($"The RegionalKey '{regionalKey.keyArn}' is previously configured as the DefaultKey."));
                }
            });

            // parse the secondary keys
            var secondaryKeys = algorithmConfiguration
                .SecondaryKeys?
                .Select(keyArn => (
                    region: KeyArnUtilities.GetRegion(keyArn),
                    keyArn: keyArn))
                .ToArray();

            // validate the secondary keys
            Array.ForEach(secondaryKeys ?? [], secondaryKey =>
            {
                if (secondaryKey.region is null)
                {
                    exs.Add(new ConfigurationErrorsException($"The SecondaryKey '{secondaryKey.keyArn}' is invalid."));
                }
            });

            // group the secondary keys by arn - each key should be unique
            var secondaryKeyGroups = secondaryKeys?
                .Where(secondaryKey => secondaryKey.region is not null)
                .GroupBy(secondaryKey => secondaryKey.keyArn)
                .ToArray();

            // enumerate each group - should be one
            Array.ForEach(secondaryKeyGroups ?? [], group =>
            {
                if (group.Count() <= 1) return;

                exs.Add(new ConfigurationErrorsException($"A SecondaryKey '{group.Key} is specified more than once."));
            });

            // enumerate each secondary key - should not be the default or in the regional keys
            Array.ForEach(secondaryKeys ?? [], secondaryKey =>
            {
                if (defaultKey.keyArn == secondaryKey.keyArn)
                {
                    exs.Add(new ConfigurationErrorsException($"The SecondaryKey '{secondaryKey.keyArn}' is previously configured as the DefaultKey."));
                }

                if (regionalKeys?.Any(regionalKey => regionalKey == secondaryKey) is true)
                {
                    exs.Add(new ConfigurationErrorsException($"The SecondaryKey '{secondaryKey.keyArn}' is previously configured as a RegionalKey."));
                }
            });

            // if there are any exceptions, then throw an aggregate exception of all exceptions
            if (exs.Count > 0)
            {
                throw new AggregateException(exs);
            }

            return new KMSAlgorithmResources(
                defaultKey: (region: defaultKey.region!, keyArn: defaultKey.keyArn),
                regionalKeys: regionalKeys?
                    .Select(regionalKey => (
                        region: regionalKey.region!,
                        keyArn: regionalKey.keyArn))
                    .ToArray(),
                secondaryKeys: secondaryKeys?
                    .Select(secondaryKey => (
                        region: secondaryKey.region!,
                        keyArn: secondaryKey.keyArn))
                    .ToArray());
        }
    }
}
