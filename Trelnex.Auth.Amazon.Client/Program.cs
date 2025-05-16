using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommandLine;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Amazon.Identity;

namespace Trelnex.Auth.Amazon.Client;

/// <summary>
/// Command-line client for obtaining JWT tokens from the Trelnex.Auth.Amazon OAuth 2.0 server.
/// </summary>
/// <remarks>
/// Uses client credentials grant with AWS STS caller identity, signing requests with SigV4.
/// </remarks>
public class Program
{
    /// <summary>
    /// Entry point for the Trelnex.Auth.Amazon.Client application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static void Main(string[] args)
    {
        // Create the ILogger for logging console output and diagnostic information.
        using var factory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = factory.CreateLogger<Program>();

        // Configure JSON serializer options for token output formatting.
        var jsonSerializerOptions = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        // Process command-line arguments using CommandLineParser library.
        Parser.Default
            .ParseArguments<Options>(args)
            .WithParsed<Options>(o =>
            {
                try
                {
                    // Create the access token client configuration with the specified server URI.
                    var clientConfiguration = new AccessTokenClientConfiguration(
                        BaseAddress: o.Uri);

                    // Set up AWS credential options with the specified region and client configuration.
                    var credentialOptions = new AmazonCredentialOptions(
                        Region: o.Region,
                        AccessTokenClient: clientConfiguration);

                    // Create the AWS credentials manager.
                    var credentialsManager = AWSCredentialsManager.Create(
                        logger: logger,
                        options: credentialOptions).GetAwaiter().GetResult();

                    // Request an access token using the client credentials flow.
                    var accessToken = credentialsManager.GetAccessToken(
                        scope: o.Scope).GetAwaiter().GetResult();

                    // Serialize the access token to JSON and write to the console.
                    var json = JsonSerializer.Serialize(accessToken, jsonSerializerOptions);
                    Console.WriteLine(json);
                }
                catch (Exception ex)
                {
                    // Log any exceptions that occur during token acquisition.
                    logger.LogError(ex, "Error obtaining access token: {Message}", ex.Message);

                    // Exit with non-zero status code to indicate failure to the caller.
                    Environment.Exit(1);
                }
            });
    }
}

/// <summary>
/// Defines the command-line options.
/// </summary>
public class Options
{
    /// <summary>
    /// Gets or sets the AWS region name.
    /// </summary>
    [Option('r', "region", Required = true, HelpText = "The AWS region name of the SecurityTokenService to sign and validate the request.")]
    public required string Region { get; set; } = null!;

    /// <summary>
    /// Gets or sets the OAuth 2.0 scope.
    /// </summary>
    /// <remarks>
    /// Format: "resource/scope" (e.g., "api://amazon.auth.trelnex.com/.default").
    /// </remarks>
    [Option('s', "scope", Required = true, HelpText = "The requested scope for the client_credentials OAuth 2.0 grant type.")]
    public required string Scope { get; set; } = null!;

    /// <summary>
    /// Gets or sets the URI of the Trelnex.Auth.Amazon OAuth 2.0 server.
    /// </summary>
    [Option('u', "uri", Required = true, HelpText = "The URI of the Trelnex.Auth.Amazon OAuth 2.0 Authorization Server.")]
    public required Uri Uri { get; set; } = null!;
}