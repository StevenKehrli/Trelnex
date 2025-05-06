using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;

namespace Trelnex.Core.Api.Rewrite;

/// <summary>
/// Provides extension methods for configuring URL rewriting rules in an application.
/// </summary>
/// <remarks>
/// URL rewriting allows incoming request URLs to be modified before they are processed
/// by the application's routing system. This can be useful for implementing friendly URLs,
/// redirecting legacy paths, or transforming requests to match application expectations.
///
/// Rules are configured in the "RewriteRules" section of application configuration.
/// </remarks>
public static class RewriteExtensions
{
    /// <summary>
    /// Configures URL rewriting rules from application configuration.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for method chaining.</returns>
    /// <remarks>
    /// This method:
    /// <list type="number">
    ///   <item>Reads rewrite rules from the "RewriteRules" configuration section</item>
    ///   <item>Creates a rewrite options object with the configured rules</item>
    ///   <item>Adds the URL rewriting middleware to the application pipeline</item>
    /// </list>
    ///
    /// Each rule consists of:
    /// <list type="bullet">
    ///   <item>A regular expression pattern to match against the URL path</item>
    ///   <item>A replacement pattern that can use capture groups from the regex</item>
    ///   <item>A flag indicating whether to skip remaining rules after a match</item>
    /// </list>
    ///
    /// If no rules are configured, this method has no effect.
    /// </remarks>
    /// <example>
    /// Configuration example:
    /// <code>
    /// {
    ///   "RewriteRules": [
    ///     {
    ///       "Regex": "^product/([0-9]+)/details$",
    ///       "Replacement": "api/products/$1",
    ///       "SkipRemainingRules": true
    ///     }
    ///   ]
    /// }
    /// </code>
    /// This example would rewrite requests from "/product/123/details" to "/api/products/123".
    /// </example>
    public static WebApplication UseRewriteRules(
        this WebApplication app)
    {
        // Load rewrite rules from configuration
        var rewriteRules = app.Configuration
            .GetSection("RewriteRules")
            .Get<RewriteRule[]>();

        // If no rules are configured, do nothing
        if (rewriteRules?.Length is null or <= 0) return app;

        // Create rewrite options and add each configured rule
        var rewriteOptions = new RewriteOptions();

        Array.ForEach(rewriteRules, rule =>
        {
            rewriteOptions.AddRewrite(rule.Regex, rule.Replacement, rule.SkipRemainingRules);
        });

        // Add the URL rewriting middleware to the pipeline
        app.UseRewriter(rewriteOptions);

        return app;
    }

    /// <summary>
    /// Represents a URL rewriting rule configuration.
    /// </summary>
    /// <param name="Regex">The regular expression pattern to match against request URLs.</param>
    /// <param name="Replacement">The replacement pattern to transform matched URLs.</param>
    /// <param name="SkipRemainingRules">Whether to skip processing remaining rules when this rule matches.</param>
    /// <remarks>
    /// This record represents a single URL rewriting rule from configuration. The rules
    /// are applied in order, with earlier rules potentially affecting later ones, unless
    /// the SkipRemainingRules flag is set to true.
    ///
    /// The Regex pattern uses .NET regular expression syntax. The Replacement string can
    /// include captured groups from the regex using $n syntax (e.g., $1, $2, etc.).
    /// </remarks>
    private record RewriteRule(
        string Regex,
        string Replacement,
        bool SkipRemainingRules);
}
