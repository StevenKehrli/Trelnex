using System.Configuration;
using Amazon;
using Amazon.Runtime;
using Trelnex.Core.Identity;

namespace Trelnex.Auth.Amazon.Services.JWT;

internal class KMSAlgorithmCollection
{
    private readonly KMSAlgorithm _defaultAlgorithm;

    private readonly KMSAlgorithm[]? _regionalAlgorithms;

    private readonly KMSAlgorithm[]? _secondaryAlgorithms;

    private KMSAlgorithmCollection(
        KMSAlgorithm defaultAlgorithm,
        KMSAlgorithm[]? regionalAlgorithms,
        KMSAlgorithm[]? secondaryAlgorithms)
    {
        _defaultAlgorithm = defaultAlgorithm;
        _regionalAlgorithms = regionalAlgorithms;
        _secondaryAlgorithms = secondaryAlgorithms;
    }

    public static KMSAlgorithmCollection Create(
        ILogger bootstrapLogger,
        ICredentialProvider<AWSCredentials> credentialProvider,
        string defaultKey,
        string[]? regionalKeys = null,
        string[]? secondaryKeys = null)
    {
        // any exceptions
        var exs = new List<ConfigurationErrorsException>();

        // get the aws credentials
        var credentials = credentialProvider.GetCredential();

        // parse the configuration into the resources
        var algorithmResources = KMSAlgorithmResources.GetResources(
            defaultKey,
            regionalKeys,
            secondaryKeys);

        // get the default algorithm
        var defaultAlgorithmTask = (
            keyArn: algorithmResources.DefaultKey.keyArn,
            algorithmTask: KMSAlgorithm.CreateAsync(
                credentials,
                algorithmResources.DefaultKey.regionEndpoint,
                algorithmResources.DefaultKey.keyArn));

        // get the regional algorithms
        var regionalAlgorithmTasks = algorithmResources
            .RegionalKeys?
            .Select(regionalKey => (
                keyArn: regionalKey.keyArn,
                algorithmTask: KMSAlgorithm.CreateAsync(
                    credentials,
                    regionalKey.regionEndpoint,
                    regionalKey.keyArn)))
            .ToArray();

        // get the secondary algorithms
        var secondaryAlgorithmTasks = algorithmResources
            .SecondaryKeys?
            .Select(secondaryKey => (
                keyArn: secondaryKey.keyArn,
                algorithmTask: KMSAlgorithm.CreateAsync(
                    credentials,
                    secondaryKey.regionEndpoint,
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

        // create the collection
        return new KMSAlgorithmCollection(
            defaultAlgorithm,
            regionalAlgorithms,
            secondaryAlgorithms);
    }

    public KMSAlgorithm DefaultAlgorithm => _defaultAlgorithm;

    public KMSAlgorithm[]? RegionalAlgorithms => _regionalAlgorithms;

    public KMSAlgorithm[]? SecondaryAlgorithms => _secondaryAlgorithms;

    private class KMSAlgorithmResources
    {
        public (RegionEndpoint regionEndpoint, string keyArn) DefaultKey { get; init; }

        public (RegionEndpoint regionEndpoint, string keyArn)[]? RegionalKeys { get; init; }

        public (RegionEndpoint regionEndpoint, string keyArn)[]? SecondaryKeys { get; init; }

        private KMSAlgorithmResources(
            (RegionEndpoint regionEndpoint, string keyArn) defaultKey,
            (RegionEndpoint regionEndpoint, string keyArn)[]? regionalKeys,
            (RegionEndpoint regionEndpoint, string keyArn)[]? secondaryKeys)
        {
            DefaultKey = defaultKey;
            RegionalKeys = regionalKeys;
            SecondaryKeys = secondaryKeys;
        }

        /// <summary>
        /// Get the KMS algorithm resources from the KMS keys.
        /// </summary>
        /// <param name="defaultKey">The default key to use for the signing algorithm.</param>
        /// <param name="regionalKeys">The regional keys to use for the signing algorithm.</param>
        /// <param name="secondaryKeys">The secondary keys to use for the signing algorithm.</param>
        /// <returns></returns>
        /// <exception cref="AggregateException">An aggregate exception of all errors that occurred during validation.</exception>
        public static KMSAlgorithmResources GetResources(
            string defaultKey,
            string[]? regionalKeys,
            string[]? secondaryKeys)
        {
            // any exceptions
            var exs = new List<ConfigurationErrorsException>();

            // parse the default key
            var defaultKeyTuple = (
                regionEndpoint: KeyArnUtilities.GetRegion(defaultKey),
                keyArn: defaultKey);

            if (defaultKeyTuple.regionEndpoint is null)
            {
                exs.Add(new ConfigurationErrorsException($"The DefaultKey '{defaultKeyTuple.keyArn}' is invalid."));
            }

            // parse the regional keys
            var regionalKeyTuples = regionalKeys?
                .Select(key => (
                    regionEndpoint: KeyArnUtilities.GetRegion(key),
                    keyArn: key))
                .ToArray();

            Array.ForEach(regionalKeyTuples ?? [], regionalKeyTuple =>
            {
                if (regionalKeyTuple.regionEndpoint is null)
                {
                    exs.Add(new ConfigurationErrorsException($"The RegionalKey '{regionalKeyTuple.keyArn}' is invalid."));
                }
            });

            // group the regional keys by region - only one key per region
            var regionalKeyGroups = regionalKeyTuples?
                .Where(regionalKeyTuple => regionalKeyTuple.regionEndpoint is not null)
                .GroupBy(regionalKeyTuple => regionalKeyTuple.regionEndpoint)
                .ToArray();

            // enumerate each group - should be one
            Array.ForEach(regionalKeyGroups ?? [], group =>
            {
                if (group.Count() <= 1) return;

                exs.Add(new ConfigurationErrorsException($"A RegionalKey for Region '{group.Key?.SystemName}' is specified more than once."));
            });

            // enumerate each regional key - should not be the default
            Array.ForEach(regionalKeyTuples ?? [], regionalKeyTuple =>
            {
                if (defaultKeyTuple.keyArn == regionalKeyTuple.keyArn)
                {
                    exs.Add(new ConfigurationErrorsException($"The RegionalKey '{regionalKeyTuple.keyArn}' is previously configured as the DefaultKey."));
                }
            });

            // parse the secondary keys
            var secondaryKeyTuples = secondaryKeys?
                .Select(key => (
                    regionEndpoint: KeyArnUtilities.GetRegion(key),
                    keyArn: key))
                .ToArray();

            // validate the secondary keys
            Array.ForEach(secondaryKeyTuples ?? [], secondaryKeyTuple =>
            {
                if (secondaryKeyTuple.regionEndpoint is null)
                {
                    exs.Add(new ConfigurationErrorsException($"The SecondaryKey '{secondaryKeyTuple.keyArn}' is invalid."));
                }
            });

            // group the secondary keys by keyArn - each key should be unique
            var secondaryKeyGroups = secondaryKeyTuples?
                .Where(secondaryKeyTuple => secondaryKeyTuple.regionEndpoint is not null)
                .GroupBy(secondaryKeyTuple => secondaryKeyTuple.keyArn)
                .ToArray();

            // enumerate each group - should be one
            Array.ForEach(secondaryKeyGroups ?? [], group =>
            {
                if (group.Count() <= 1) return;

                exs.Add(new ConfigurationErrorsException($"A SecondaryKey '{group.Key} is specified more than once."));
            });

            // enumerate each secondary key - should not be the default or in the regional keys
            Array.ForEach(secondaryKeyTuples ?? [], secondaryKeyTuple =>
            {
                if (defaultKeyTuple.keyArn == secondaryKeyTuple.keyArn)
                {
                    exs.Add(new ConfigurationErrorsException($"The SecondaryKey '{secondaryKeyTuple.keyArn}' is previously configured as the DefaultKey."));
                }

                if (regionalKeyTuples?.Any(regionalKeyTuple => regionalKeyTuple == secondaryKeyTuple) is true)
                {
                    exs.Add(new ConfigurationErrorsException($"The SecondaryKey '{secondaryKeyTuple.keyArn}' is previously configured as a RegionalKey."));
                }
            });

            // if there are any exceptions, then throw an aggregate exception of all exceptions
            if (exs.Count > 0)
            {
                throw new AggregateException(exs);
            }

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
    }
}
