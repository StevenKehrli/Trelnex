using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Trelnex.Core.Amazon.Identity;

namespace Trelnex.Auth.Amazon.Token;

/// <summary>
/// Command-line client for obtaining JWT tokens from the Trelnex.Auth.Amazon OAuth 2.0 server.
/// </summary>
/// <remarks>
/// Uses client credentials grant with AWS STS caller identity, signing requests with SigV4.
/// </remarks>
public class Program
{
    /// <summary>
    /// Entry point for the Trelnex.Auth.Amazon.Token application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static async Task<int> Main(string[] args)
    {
        // Create the ILogger for logging console output and diagnostic information
        using var factory = LoggerFactory.Create(builder => builder
            .AddConsole(options =>
            {
                options.FormatterName = nameof(LogFormatter);
            })
            .AddConsoleFormatter<LogFormatter, ConsoleFormatterOptions>());

        var logger = factory.CreateLogger<Program>();

        // Process command-line arguments using CommandLineParser library
        return await Parser.Default
            .ParseArguments<Options>(args)
            .MapResult(
                async o =>
                {
                    try
                    {
                        await HandleOptionsAsync(o, logger);
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        // Log any exceptions that occur during token acquisition
                        logger.LogError(ex, "Error obtaining the access token: {message}", ex.Message);
                        return 1;
                    }
                },
                errors => Task.FromResult(1));
    }

    /// <summary>
    /// Handles the command-line options by obtaining an access token from the OAuth 2.0 server.
    /// </summary>
    /// <param name="options">The parsed command-line options containing authentication configuration.</param>
    /// <param name="logger">Logger instance for outputting diagnostic information.</param>
    /// <returns>A task representing the asynchronous token acquisition operation.</returns>
    private static async Task HandleOptionsAsync(
        Options options,
        ILogger logger)
    {
        // Configure JSON serializer options for consistent token output formatting
        // This ensures the access token is displayed in a readable, properly indented format
        var jsonSerializerOptions = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        // Create the access token client configuration with the specified OAuth 2.0 server URI
        // This tells the credential provider where to send token requests
        var clientConfiguration = new AccessTokenClientConfiguration(
            BaseAddress: options.Uri);

        // Set up AWS credential options combining the AWS region and OAuth 2.0 client configuration
        // The region is used for STS operations to get caller identity and sign requests
        var credentialOptions = new AmazonCredentialOptions(
            Region: options.Region,
            AccessTokenClient: clientConfiguration);

        // Create the Amazon credential provider using the default AWS credential chain
        // This handles the integration between AWS credentials and OAuth 2.0 token acquisition
        var credentialProvider = await AmazonCredentialProvider
            .CreateAsync(credentialOptions, logger);

        // Get an access token provider for the requested scope
        var accessTokenProvider = credentialProvider.GetAccessTokenProvider(options.Scope);

        // Request an access token using the client credentials OAuth 2.0 flow
        // The scope determines what resources and permissions the token will grant access to
        var accessToken = accessTokenProvider.GetAccessToken();

        // Serialize the access token response to formatted JSON and output to console
        // This allows the token to be easily consumed by other tools or scripts
        var json = JsonSerializer.Serialize(accessToken, jsonSerializerOptions);
        Console.WriteLine(json);
    }
}

/// <summary>
/// Defines the command-line options for token acquisition.
/// </summary>
public class Options
{
    /// <summary>
    /// Gets or sets the AWS region name for the SecurityTokenService.
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