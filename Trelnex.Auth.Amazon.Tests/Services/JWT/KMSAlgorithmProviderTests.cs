using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using Snapshooter.NUnit;
using Trelnex.Auth.Amazon.Services.JWT;
using Trelnex.Core.Identity;

namespace Trelnex.Auth.Amazon.Tests.Services.JWT;

public class KMSAlgorithmCollectionTests
{
    private static readonly ICredentialProvider<AWSCredentials> _credentialProvider = new CredentialProvider();

    [Test]
    public void KMSAlgorithmCollection_DuplicateRegional()
    {
        // create the test configuration
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

        var ex = Assert.Throws<AggregateException>(() =>
        {
            KMSAlgorithmCollection.Create(
                bootstrapLogger,
                _credentialProvider,
                defaultKey: defaultKey,
                regionalKeys: regionalKeys);
        });

        var o = ex.InnerExceptions.Select(e => e.Message).ToArray();

        Snapshot.Match(o);
    }

    [Test]
    public void KMSAlgorithmCollection_DuplicateSecondary()
    {
        // create the test configuration
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

        var ex = Assert.Throws<AggregateException>(() =>
        {
            KMSAlgorithmCollection.Create(
                bootstrapLogger,
                _credentialProvider,
                defaultKey: defaultKey,
                secondaryKeys: secondaryKeys);
        });

        var o = ex.InnerExceptions.Select(e => e.Message).ToArray();

        Snapshot.Match(o);
    }

    [Test]
    public void KMSAlgorithmCollection_InvalidDefault()
    {
        // create the test configuration
        var defaultKey = "Invalid";

        using ILoggerFactory factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        var bootstrapLogger = factory.CreateLogger("Trelnex.Auth.Amazon.Tests");

        var ex = Assert.Throws<AggregateException>(() =>
        {
            KMSAlgorithmCollection.Create(
                bootstrapLogger,
                _credentialProvider,
                defaultKey: defaultKey);
        });

        var o = ex.InnerExceptions.Select(e => e.Message).ToArray();

        Snapshot.Match(o);
    }

    [Test]
    public void KMSAlgorithmCollection_InvalidRegional()
    {
        // create the test configuration
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

        var ex = Assert.Throws<AggregateException>(() =>
        {
            KMSAlgorithmCollection.Create(
                bootstrapLogger,
                _credentialProvider,
                defaultKey: defaultKey,
                regionalKeys: regionalKeys);
        });

        var o = ex.InnerExceptions.Select(e => e.Message).ToArray();

        Snapshot.Match(o);
    }

    [Test]
    public void KMSAlgorithmCollection_InvalidSecondary()
    {
        // create the test configuration
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

        var ex = Assert.Throws<AggregateException>(() =>
        {
            KMSAlgorithmCollection.Create(
                bootstrapLogger,
                _credentialProvider,
                defaultKey: defaultKey,
                secondaryKeys: secondaryKeys);
        });

        var o = ex.InnerExceptions.Select(e => e.Message).ToArray();

        Snapshot.Match(o);
    }

    [Test]
    public void KMSAlgorithmCollection_RegionalSpecifiedAsDefault()
    {
        // create the test configuration
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

        var ex = Assert.Throws<AggregateException>(() =>
        {
            KMSAlgorithmCollection.Create(
                bootstrapLogger,
                _credentialProvider,
                defaultKey: defaultKey,
                regionalKeys: regionalKeys);
        });

        var o = ex.InnerExceptions.Select(e => e.Message).ToArray();

        Snapshot.Match(o);
    }

    [Test]
    public void KMSAlgorithmCollection_SecondarySpecifiedAsDefault()
    {
        // create the test configuration
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

        var ex = Assert.Throws<AggregateException>(() =>
        {
            KMSAlgorithmCollection.Create(
                bootstrapLogger,
                _credentialProvider,
                defaultKey: defaultKey,
                secondaryKeys: secondaryKeys);
        });

        var o = ex.InnerExceptions.Select(e => e.Message).ToArray();

        Snapshot.Match(o);
    }

    [Test]
    public void KMSAlgorithmCollection_SecondarySpecifiedAsRegional()
    {
        // create the test configuration
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

        Snapshot.Match(o);
    }

    private class CredentialProvider : ICredentialProvider<AWSCredentials>
    {
        private static readonly AWSCredentials _credentials = FallbackCredentialsFactory.GetCredentials();

        public string Name => "Amazon";

        public IAccessTokenProvider GetAccessTokenProvider(
            string scope)
        {
            throw new NotImplementedException();
        }

        public AWSCredentials GetCredential()
        {
            return _credentials;
        }

        public CredentialStatus GetStatus()
        {
            throw new NotImplementedException();
        }
    }
}
