using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommandLine;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Amazon.Identity;

// create the iLogger
using var factory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = factory.CreateLogger<Program>();

var jsonSerializerOptions = new JsonSerializerOptions()
{
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    WriteIndented = true
};

Parser.Default
    .ParseArguments<Options>(args)
    .WithParsed<Options>(o =>
    {
        // create the credentials manager
        var clientConfiguration = new AccessTokenClientConfiguration(
            BaseAddress: o.Uri);

        var credentialOptions = new AmazonCredentialOptions(
            Region: o.Region,
            AccessTokenClient: clientConfiguration);

        var credentialsManager = AWSCredentialsManager.Create(
            logger: logger,
            options: credentialOptions).Result;

        // get the access token
        var accessToken = credentialsManager.GetAccessToken(
            scope: o.Scope).Result;

        // serialize and write to the console
        var json = JsonSerializer.Serialize(accessToken, jsonSerializerOptions);

        Console.WriteLine(json);
    });

class Options
{
    [Option('r', "region", Required = true, HelpText = "The AWS region name of the SecurityTokenService to sign and validate the request.")]
    public required string Region { get; set; } = null!;

    [Option('s', "scope", Required = true, HelpText = "The requested scope for the client_credentials OAuth 2.0 grant type.")]
    public required string Scope { get; set; } = null!;

    [Option('u', "uri", Required = true, HelpText = "The URI of the Trelnex.Auth.Amazon OAuth 2.0 Authorization Server.")]
    public required Uri Uri { get; set; } = null!;
}
