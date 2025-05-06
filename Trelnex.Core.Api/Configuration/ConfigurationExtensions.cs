using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Trelnex.Core.Api.Configuration;

/// <summary>
/// Provides extension methods for configuring application settings in a standardized way.
/// </summary>
/// <remarks>
/// These extensions establish a consistent configuration approach across applications,
/// with a layered configuration model that combines settings from multiple sources
/// with a well-defined precedence order.
/// </remarks>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Configures the application's configuration sources using a layered approach.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <remarks>
    /// This method sets up configuration sources in the following order of precedence (highest last):
    /// <list type="number">
    ///   <item>Base application settings from appsettings.json</item>
    ///   <item>Environment-specific settings (Development/Staging/Production)</item>
    ///   <item>User-specific settings from appsettings.User.json (for local development)</item>
    ///   <item>Environment variables (highest precedence)</item>
    /// </list>
    ///
    /// The order is important as later sources override earlier ones. This allows for
    /// environment-specific configurations and local development overrides.
    ///
    /// Also registers the options pattern for strongly-typed configuration access.
    /// </remarks>
    public static void AddConfiguration(
        this WebApplicationBuilder builder)
    {
        // Define the layered configuration files in order of precedence
        // Later files override settings from earlier files
        string[] jsonFiles = [
            "appsettings.json",                                    // Base settings
            $"appsettings.{builder.Environment.EnvironmentName}.json", // Environment-specific settings
            "appsettings.User.json"                                // User-specific overrides (not in source control)
        ];

        // Configure the configuration sources
        builder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFiles(jsonFiles)          // Add JSON configuration files
            .AddEnvironmentVariables();        // Environment variables override all JSON settings

        // Register the options pattern for strongly-typed configuration
        builder.Services.AddOptions();
    }

    /// <summary>
    /// Adds multiple JSON configuration files to the configuration builder.
    /// </summary>
    /// <param name="configurationBuilder">The configuration builder to add files to.</param>
    /// <param name="jsonFiles">An array of JSON file paths relative to the base path.</param>
    /// <returns>The same configuration builder for method chaining.</returns>
    /// <remarks>
    /// Each file is configured as optional (won't fail if missing) and will
    /// automatically reload when changed on disk. Files are processed in the
    /// order provided, with later files overriding settings from earlier ones.
    /// </remarks>
    private static IConfigurationBuilder AddJsonFiles(
        this IConfigurationBuilder configurationBuilder,
        string[] jsonFiles)
    {
        // Add each JSON file to the configuration
        Array.ForEach(
            jsonFiles,
            jsonFile => configurationBuilder.AddJsonFile(
                path: jsonFile,
                optional: true,           // Don't fail if the file doesn't exist
                reloadOnChange: true));   // Reload configuration if the file changes

        return configurationBuilder;
    }
}
