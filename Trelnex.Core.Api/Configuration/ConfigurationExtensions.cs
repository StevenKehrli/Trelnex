using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Trelnex.Core.Api.Configuration;

/// <summary>
/// Provides extension methods for configuring application settings.
/// </summary>
/// <remarks>
/// Establishes a consistent configuration approach.
/// </remarks>
public static class ConfigurationExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Configures the application's configuration sources using a layered approach.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    public static void AddConfiguration(
        this WebApplicationBuilder builder)
    {
        // Define the layered configuration files in order of precedence.
        // Later files override settings from earlier files.
        string[] jsonFiles = [
            // Base settings
            "appsettings.json",
            // Environment-specific settings
            $"appsettings.{builder.Environment.EnvironmentName}.json",
            // User-specific overrides (not in source control)
            "appsettings.User.json"
        ];

        // Configure the configuration sources.
        builder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            // Add JSON configuration files
            .AddJsonFiles(jsonFiles)
            // Environment variables override all JSON settings
            .AddEnvironmentVariables();

        // Register the options pattern for strongly-typed configuration.
        builder.Services.AddOptions();
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Adds multiple JSON configuration files to the configuration builder.
    /// </summary>
    /// <param name="configurationBuilder">The configuration builder to add files to.</param>
    /// <param name="jsonFiles">An array of JSON file paths relative to the base path.</param>
    /// <returns>The same configuration builder for method chaining.</returns>
    private static IConfigurationBuilder AddJsonFiles(
        this IConfigurationBuilder configurationBuilder,
        string[] jsonFiles)
    {
        // Add each JSON file to the configuration.
        Array.ForEach(
            jsonFiles,
            jsonFile => configurationBuilder.AddJsonFile(
                path: jsonFile,
                optional: true,
                reloadOnChange: true));

        return configurationBuilder;
    }

    #endregion
}
