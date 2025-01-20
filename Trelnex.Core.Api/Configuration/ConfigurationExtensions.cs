using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Trelnex.Core.Api.Configuration;

/// <summary>
/// Extension methods to add the configuration to the <see cref="WebApplicationBuilder"/>.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Add the configuration to the <see cref="WebApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/>.</param>
    public static void AddConfiguration(
        this WebApplicationBuilder builder)
    {
        // the array of json files to the configuration
        //   1. appsettings.json
        //   2. appsettings.Development.json | appsettings.Staging.json | appsettings.Production.json (based on {builder.Environment.EnvironmentName})
        //   3. appsettings.User.json
        // ORDER MATTERS!!!
        string[] jsonFiles = [
            "appsettings.json",
            $"appsettings.{builder.Environment.EnvironmentName}.json",
            "appsettings.User.json"
        ];

        builder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFiles(jsonFiles)
            .AddEnvironmentVariables();

        builder.Services.AddOptions();
    }

    /// <summary>
    /// Add the specified json files to the <see cref="IConfigurationBuilder"/>.
    /// </summary>
    /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/>.</param>
    /// <param name="jsonFiles">The array of specified json files.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    private static IConfigurationBuilder AddJsonFiles(
        this IConfigurationBuilder configurationBuilder,
        string[] jsonFiles)
    {
        Array.ForEach(
            jsonFiles,
            jsonFile => configurationBuilder.AddJsonFile(jsonFile, optional: true, reloadOnChange: true));

        return configurationBuilder;
    }
}
