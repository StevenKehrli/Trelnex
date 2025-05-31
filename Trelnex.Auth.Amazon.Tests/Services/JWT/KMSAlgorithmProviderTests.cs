using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using Snapshooter.NUnit;
using Trelnex.Auth.Amazon.Services.JWT;
using Trelnex.Core.Identity;

namespace Trelnex.Auth.Amazon.Tests.Services.JWT;

/// <summary>
/// Tests for the KMSAlgorithmCollection class to verify key validation and error handling.
/// </summary>
[Category("JWT")]
public class KMSAlgorithmCollectionTests
{
    /// <summary>
    /// Credential provider for tests that returns basic AWS credentials.
    /// </summary>
    private static readonly ICredentialProvider<AWSCredentials> _credentialProvider = new CredentialProvider();

    /// <summary>
    /// Tests that an exception is thrown when duplicate regional keys are provided.
    /// </summary>
    [Test]
    [Description("Verifies that an exception is thrown when duplicate regional keys are provided.")]
    public void KMSAlgorithmCollection_DuplicateRegional()
    {
        // Create the test configuration with duplicate regional keys
        var defaultKey = "arn:aws:kms:us-east-1:571096773025:key/875de039-9e63-4f2c-abae-1877a2f5a4d4";
        var regionalKeys = new[] {
                "arn:aws:kms:us-east-1:571096773025:key/7bb77ef2-8421-4c14-bdba-4ecea906e145",
                "arn:aws:kms:us-east-1:571096773025:key/dcdf7318-d283-442a-b1fb-b6e538b3ccfe"
            };

        using ILoggerFactory factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        var bootstrapLogger = factory.CreateLogger("Trelnex.Auth.Amazon.Tests");

        // Assert that an AggregateException is thrown when creating the collection
        var ex = Assert.Throws<AggregateException>(() =>
        {
            KMSAlgorithmCollection.Create(
                bootstrapLogger,
                _credentialProvider,
                defaultKey: defaultKey,
                regionalKeys: regionalKeys);
        });

        var o = ex.InnerExceptions.Select(e => e.Message).ToArray();

        // Verify the error messages match expected values
        Snapshot.Match(o);
    }

    /// <summary>
    /// Tests that an exception is thrown when duplicate secondary keys are provided.
    /// </summary>
    [Test]
    [Description("Verifies that an exception is thrown when duplicate secondary keys are provided.")]
    public void KMSAlgorithmCollection_DuplicateSecondary()
    {
        // Create the test configuration with duplicate secondary keys
        var defaultKey = "arn:aws:kms:us-east-1:571096773025:key/875de039-9e63-4f2c-abae-1877a2f5a4d4";
        var secondaryKeys = new[] {
                "arn:aws:kms:us-east-1:571096773025:key/449c147f-267e-4dad-8731-3e150703301c",
                "arn:aws:kms:us-east-1:571096773025:key/449c147f-267e-4dad-8731-3e150703301c"
            };

        using ILoggerFactory factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        var bootstrapLogger = factory.CreateLogger("Trelnex.Auth.Amazon.Tests");

        // Assert that an AggregateException is thrown when creating the collection
        var ex = Assert.Throws<AggregateException>(() =>
        {
            KMSAlgorithmCollection.Create(
                bootstrapLogger,
                _credentialProvider,
                defaultKey: defaultKey,
                secondaryKeys: secondaryKeys);
        });

        var o = ex.InnerExceptions.Select(e => e.Message).ToArray();

        // Verify the error messages match expected values
        Snapshot.Match(o);
    }

    /// <summary>
    /// Tests that an exception is thrown when an invalid default key is provided.
    /// </summary>
    [Test]
    [Description("Verifies that an exception is thrown when an invalid default key is provided.")]
    public void KMSAlgorithmCollection_InvalidDefault()
    {
        // Create the test configuration with an invalid default key
        var defaultKey = "Invalid";

        using ILoggerFactory factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        var bootstrapLogger = factory.CreateLogger("Trelnex.Auth.Amazon.Tests");

        // Assert that an AggregateException is thrown when creating the collection
        var ex = Assert.Throws<AggregateException>(() =>
        {
            KMSAlgorithmCollection.Create(
                bootstrapLogger,
                _credentialProvider,
                defaultKey: defaultKey);
        });

        var o = ex.InnerExceptions.Select(e => e.Message).ToArray();

        // Verify the error messages match expected values
        Snapshot.Match(o);
    }

    /// <summary>
    /// Tests that an exception is thrown when an invalid regional key is provided.
    /// </summary>
    [Test]
    [Description("Verifies that an exception is thrown when an invalid regional key is provided.")]
    public void KMSAlgorithmCollection_InvalidRegional()
    {
        // Create the test configuration with an invalid regional key
        var defaultKey = "arn:aws:kms:us-east-1:571096773025:key/875de039-9e63-4f2c-abae-1877a2f5a4d4";
        var regionalKeys = new[] {
                "Invalid"
            };

        using ILoggerFactory factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        var bootstrapLogger = factory.CreateLogger("Trelnex.Auth.Amazon.Tests");

        // Assert that an AggregateException is thrown when creating the collection
        var ex = Assert.Throws<AggregateException>(() =>
        {
            KMSAlgorithmCollection.Create(
                bootstrapLogger,
                _credentialProvider,
                defaultKey: defaultKey,
                regionalKeys: regionalKeys);
        });

        var o = ex.InnerExceptions.Select(e => e.Message).ToArray();

        // Verify the error messages match expected values
        Snapshot.Match(o);
    }

    /// <summary>
    /// Tests that an exception is thrown when an invalid secondary key is provided.
    /// </summary>
    [Test]
    [Description("Verifies that an exception is thrown when an invalid secondary key is provided.")]
    public void KMSAlgorithmCollection_InvalidSecondary()
    {
        // Create the test configuration with an invalid secondary key
        var defaultKey = "arn:aws:kms:us-east-1:571096773025:key/875de039-9e63-4f2c-abae-1877a2f5a4d4";
        var secondaryKeys = new[] {
                "Invalid"
            };

        using ILoggerFactory factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        var bootstrapLogger = factory.CreateLogger("Trelnex.Auth.Amazon.Tests");

        // Assert that an AggregateException is thrown when creating the collection
        var ex = Assert.Throws<AggregateException>(() =>
        {
            KMSAlgorithmCollection.Create(
                bootstrapLogger,
                _credentialProvider,
                defaultKey: defaultKey,
                secondaryKeys: secondaryKeys);
        });

        var o = ex.InnerExceptions.Select(e => e.Message).ToArray();

        // Verify the error messages match expected values
        Snapshot.Match(o);
    }

    /// <summary>
    /// Tests that an exception is thrown when a regional key is the same as the default key.
    /// </summary>
    [Test]
    [Description("Verifies that an exception is thrown when a regional key is the same as the default key.")]
    public void KMSAlgorithmCollection_RegionalSpecifiedAsDefault()
    {
        // Create the test configuration with a regional key that matches the default key
        var defaultKey = "arn:aws:kms:us-east-1:571096773025:key/875de039-9e63-4f2c-abae-1877a2f5a4d4";
        var regionalKeys = new[] {
                "arn:aws:kms:us-east-1:571096773025:key/875de039-9e63-4f2c-abae-1877a2f5a4d4"
            };

        using ILoggerFactory factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        var bootstrapLogger = factory.CreateLogger("Trelnex.Auth.Amazon.Tests");

        // Assert that an AggregateException is thrown when creating the collection
        var ex = Assert.Throws<AggregateException>(() =>
        {
            KMSAlgorithmCollection.Create(
                bootstrapLogger,
                _credentialProvider,
                defaultKey: defaultKey,
                regionalKeys: regionalKeys);
        });

        var o = ex.InnerExceptions.Select(e => e.Message).ToArray();

        // Verify the error messages match expected values
        Snapshot.Match(o);
    }

    /// <summary>
    /// Tests that an exception is thrown when a secondary key is the same as the default key.
    /// </summary>
    [Test]
    [Description("Verifies that an exception is thrown when a secondary key is the same as the default key.")]
    public void KMSAlgorithmCollection_SecondarySpecifiedAsDefault()
    {
        // Create the test configuration with a secondary key that matches the default key
        var defaultKey = "arn:aws:kms:us-east-1:571096773025:key/875de039-9e63-4f2c-abae-1877a2f5a4d4";
        var secondaryKeys = new[] {
                "arn:aws:kms:us-east-1:571096773025:key/875de039-9e63-4f2c-abae-1877a2f5a4d4"
            };

        using ILoggerFactory factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        var bootstrapLogger = factory.CreateLogger("Trelnex.Auth.Amazon.Tests");

        // Assert that an AggregateException is thrown when creating the collection
        var ex = Assert.Throws<AggregateException>(() =>
        {
            KMSAlgorithmCollection.Create(
                bootstrapLogger,
                _credentialProvider,
                defaultKey: defaultKey,
                secondaryKeys: secondaryKeys);
        });

        var o = ex.InnerExceptions.Select(e => e.Message).ToArray();

        // Verify the error messages match expected values
        Snapshot.Match(o);
    }

    /// <summary>
    /// Tests that an exception is thrown when a secondary key matches a regional key.
    /// </summary>
    [Test]
    [Description("Verifies that an exception is thrown when a secondary key matches a regional key.")]
    public void KMSAlgorithmCollection_SecondarySpecifiedAsRegional()
    {
        // Create the test configuration with a secondary key that matches a regional key
        var defaultKey = "arn:aws:kms:us-east-1:571096773025:key/875de039-9e63-4f2c-abae-1877a2f5a4d4";
        var regionalKeys = new[] {
                "arn:aws:kms:us-east-1:571096773025:key/449c147f-267e-4dad-8731-3e150703301c"
            };
        var secondaryKeys = new[] {
                "arn:aws:kms:us-east-1:571096773025:key/449c147f-267e-4dad-8731-3e150703301c"
            };

        using ILoggerFactory factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        var bootstrapLogger = factory.CreateLogger("Trelnex.Auth.Amazon.Tests");

        // Assert that an AggregateException is thrown when creating the collection
        var ex = Assert.Throws<AggregateException>(() =>
        {
            KMSAlgorithmCollection.Create(
                bootstrapLogger,
                _credentialProvider,
                defaultKey: defaultKey,
                regionalKeys: regionalKeys,
                secondaryKeys: secondaryKeys);
        });

        var o = ex.InnerExceptions.Select(e => e.Message).ToArray();

        // Verify the error messages match expected values
        Snapshot.Match(o);
    }

    /// <summary>
    /// Mock credential provider for testing purposes.
    /// </summary>
    private class CredentialProvider : ICredentialProvider<AWSCredentials>
    {
        /// <summary>
        /// Gets the name of the credential provider.
        /// </summary>
        public string Name => "Amazon";

        /// <summary>
        /// Gets an access token provider for the specified scope.
        /// </summary>
        /// <param name="scope">The scope for which to get an access token provider.</param>
        /// <returns>An access token provider.</returns>
        /// <exception cref="NotImplementedException">This method is not implemented for testing.</exception>
        public IAccessTokenProvider GetAccessTokenProvider(string scope)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets AWS credentials for authentication.
        /// </summary>
        /// <returns>Basic AWS credentials with test values.</returns>
        public AWSCredentials GetCredential()
        {
            return new BasicAWSCredentials("accessKey", "secretKey");
        }

        /// <summary>
        /// Gets the status of the credential provider.
        /// </summary>
        /// <returns>The credential status.</returns>
        /// <exception cref="NotImplementedException">This method is not implemented for testing.</exception>
        public CredentialStatus GetStatus()
        {
            throw new NotImplementedException();
        }
    }
}
