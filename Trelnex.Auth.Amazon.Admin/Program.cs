using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;

namespace Trelnex.Auth.Amazon.Admin;

/// <summary>
/// Command-line client for provisioning resources in the Trelnex.Auth.Amazon RBAC system.
/// </summary>
/// <remarks>
/// Creates resources, scopes, roles, and their assignments in the RBAC DynamoDB table.
/// Uses AWS credentials from the default credential chain for authentication.
/// </remarks>
public class Program
{
    /// <summary>
    /// Entry point for the Trelnex.Auth.Amazon.Admin application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static void Main(string[] args)
    {
        // Create the ILogger for logging console output and diagnostic information.
        using var factory = LoggerFactory.Create(builder => builder
            .AddConsole(options =>
            {
                options.FormatterName = nameof(LogFormatter);
            })
            .AddConsoleFormatter<LogFormatter, ConsoleFormatterOptions>());

        var logger = factory.CreateLogger<Program>();

        // Process command-line arguments using CommandLineParser library.
        Parser.Default
            .ParseArguments<Options>(args)
            .WithParsed(o =>
            {
                try
                {
                    HandleOptions(o, logger);
                }
                catch (Exception ex)
                {
                    // Log any exceptions that occur during RBAC provisioning.
                    logger.LogError("Error provisioning the RBAC resource: {message}", ex.Message);

                    // Exit with non-zero status code to indicate failure to the caller.
                    Environment.Exit(1);
                }
            });
    }

    /// <summary>
    /// Retrieves the AWS caller identity ARN using STS.
    /// </summary>
    /// <param name="credentials">AWS credentials to use for the STS call.</param>
    /// <param name="regionEndpoint">The AWS region endpoint for the STS service.</param>
    /// <returns>The ARN of the current AWS caller identity.</returns>
    private static string GetPrincipalId(
        AWSCredentials credentials,
        RegionEndpoint regionEndpoint)
    {
        // Configure the STS client with the specified region endpoint.
        var stsConfig = new AmazonSecurityTokenServiceConfig
        {
            RegionEndpoint = regionEndpoint,
        };

        // Create the STS client using the provided credentials and configuration.
        var stsClient = new AmazonSecurityTokenServiceClient(credentials, stsConfig);

        // Get the caller identity (AWS principal) using STS to determine who is making the request.
        var request = new GetCallerIdentityRequest();
        var response = stsClient
            .GetCallerIdentityAsync(request)
            .GetAwaiter()
            .GetResult();

        // Return the ARN which uniquely identifies the AWS principal.
        return response.Arn;
    }

    /// <summary>
    /// Handles the command-line options by provisioning the RBAC resource with specified scopes and roles.
    /// </summary>
    /// <param name="options">The parsed command-line options containing resource configuration.</param>
    /// <param name="logger">Logger instance for outputting diagnostic information.</param>
    /// <returns>A task representing the asynchronous provisioning operation.</returns>
    private static void HandleOptions(
        Options options,
        ILogger logger)
    {
        // Retrieve AWS credentials using the default credentials provider chain.
        // This will check environment variables, AWS profiles, IAM roles, etc.
        var credentials = DefaultAWSCredentialsIdentityResolver.GetCredentials();

        // Convert the region string to an AWS RegionEndpoint object required by the AWS SDK.
        var regionEndpoint = RegionEndpoint.GetBySystemName(
            systemName: options.Region);

        // Determine the principal ID to use for role and scope assignments.
        // If not provided via command line, get the current AWS caller identity.
        var principalId = options.PrincipalId
            ?? GetPrincipalId(credentials, regionEndpoint);

        // Log the configuration values being used for the provisioning operation.
        logger.LogInformation("Region: {region}", options.Region);
        logger.LogInformation("TableName: {tableName}", options.TableName);
        logger.LogInformation("ResourceName: {resourceName}", options.ResourceName);
        logger.LogInformation("Scopes: {scopes}", string.Join(", ", options.ScopeNames));
        logger.LogInformation("Roles: {roles}", string.Join(", ", options.RoleNames));
        logger.LogInformation("PrincipalId: {principalId}", principalId);
        logger.LogInformation("");

        // Initialize the DynamoDB client with credentials and region for RBAC data storage.
        var client = new AmazonDynamoDBClient(
            credentials: credentials,
            region: regionEndpoint);

        // Create the RBAC repository with validators and the DynamoDB client.
        // The validators ensure that resource names, scope names, and role names follow required formats.
        var repository = new RBACRepository(
            new ResourceNameValidator(),
            new ScopeNameValidator(),
            new RoleNameValidator(),
            client,
            options.TableName);

        // Create the main resource that will contain all scopes and roles.
        logger.LogInformation("Creating resource: {resourceName}", options.ResourceName);

        repository
            .CreateResourceAsync(resourceName: options.ResourceName)
            .GetAwaiter()
            .GetResult();

        logger.LogInformation("");

        // Create each scope and assign it to the specified principal.
        foreach (var scopeName in options.ScopeNames)
        {
            // Create the scope within the resource.
            logger.LogInformation("Creating scope: {scopeName}", scopeName);

            repository
                .CreateScopeAsync(resourceName: options.ResourceName, scopeName: scopeName)
                .GetAwaiter()
                .GetResult();
        }

        logger.LogInformation("");

        foreach (var scopeName in options.ScopeNames)
        {
            // Assign the scope to the principal, granting them access to this scope.
            logger.LogInformation("Creating scope assignment: {scopeName} -> {principalId}", scopeName, principalId);

            repository
                .CreateScopeAssignmentAsync(
                    resourceName: options.ResourceName,
                    scopeName: scopeName,
                    principalId: principalId)
                .GetAwaiter()
                .GetResult();
        }

        logger.LogInformation("");

        // Create each role and assign it to the specified principal.
        foreach (var roleName in options.RoleNames)
        {
            // Create the role within the resource.
            logger.LogInformation("Creating role: {roleName}", roleName);

            repository
                .CreateRoleAsync(
                    resourceName: options.ResourceName,
                    roleName: roleName)
                .GetAwaiter()
                .GetResult();
        }

        logger.LogInformation("");

        foreach (var roleName in options.RoleNames)
        {

            // Assign the role to the principal, granting them this role's permissions.
            logger.LogInformation("Creating role assignment: {roleName} -> {principalId}", roleName, principalId);

            repository
                .CreateRoleAssignmentAsync(
                    resourceName: options.ResourceName,
                    roleName: roleName,
                    principalId: principalId)
                .GetAwaiter()
                .GetResult();
        }

        logger.LogInformation("");

        logger.LogInformation("Successfully provisioned RBAC resource: {resourceName}", options.ResourceName);
        logger.LogInformation("");
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
    [Option("region", Required = true, HelpText = "The AWS region of the DynamoDB table for the RBAC repository.")]
    public required string Region { get; set; } = null!;

    /// <summary>
    /// Gets or sets the DynamoDB table name for the RBAC repository.
    /// </summary>
    [Option("tableName", Required = true, HelpText = "The DynamoDB table name for the RBAC repository.")]
    public required string TableName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the resource name to provision in the RBAC system.
    /// </summary>
    /// <remarks>
    /// Format: URI-like identifier (e.g., "api://amazon.auth.trelnex.com").
    /// </remarks>
    [Option("resourceName", Required = true, HelpText = "The resource name to provision in the RBAC system in URI format.")]
    public required string ResourceName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the array of scope names to create for the resource.
    /// </summary>
    /// <remarks>
    /// Format: scope identifiers (e.g., "rbac").
    /// </remarks>
    [Option("scopes", Required = true, HelpText = "The array of scope names to create for the resource.")]
    public IEnumerable<string> ScopeNames { get; set; } = [];

    /// <summary>
    /// Gets or sets the array of role names to create for the resource.
    /// </summary>
    /// <remarks>
    /// Format: role identifiers (e.g., "rbac.create", "rbac.read", "rbac.update", "rbac.delete").
    /// </remarks>
    [Option("roles", Required = true, HelpText = "The array of role names to create for the resource.")]
    public IEnumerable<string> RoleNames { get; set; } = [];

    /// <summary>
    /// Gets or sets the optional principal identifier for role and scope assignments.
    /// </summary>
    /// <remarks>
    /// If not specified, the principal identifier of the current AWS caller identity will be used.
    /// If specified, the provided value will be used for all role and scope assignments.
    /// Format: AWS ARN (e.g., "arn:aws:iam::123456789012:user/john").
    /// </remarks>
    [Option("principalId", Required = false, HelpText = "The optional principal identifier for role and scope assignments. If not specified, uses the current AWS caller identity.")]
    public string? PrincipalId { get; set; }

    [Usage(ApplicationAlias = "Trelnex.Auth.Amazon.Admin")]
    public static IEnumerable<Example> Examples
    {
        get
        {
            return [
                new("Provision the RBAC resource for Trelnex.Auth.Amazon using the current AWS caller identity", new Options
                {
                    Region = "us-west-2",
                    TableName = "trelnex-auth-amazon-rbac",
                    ResourceName = "api://amazon.auth.trelnex.com",
                    ScopeNames = ["rbac"],
                    RoleNames = ["rbac.create", "rbac.read", "rbac.update", "rbac.delete"]
                }),
                new("Provision the RBAC resource for Trelnex.Auth.Amazon with a specific principal", new Options
                {
                    Region = "us-west-2",
                    TableName = "trelnex-auth-amazon-rbac",
                    ResourceName = "api://amazon.auth.trelnex.com",
                    ScopeNames = ["rbac"],
                    RoleNames = ["rbac.create", "rbac.read", "rbac.update", "rbac.delete"],
                    PrincipalId = "arn:aws:iam::123456789012:user/john"
                })
            ];
        }
    }
}
