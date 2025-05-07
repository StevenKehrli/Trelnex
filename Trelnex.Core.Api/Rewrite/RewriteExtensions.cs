using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;

namespace Trelnex.Core.Api.Rewrite;

/// <summary>
/// Provides extension methods for configuring URL rewriting rules.
/// </summary>
/// <remarks>
/// Rules are configured in the "RewriteRules" section.
/// </remarks>
public static class RewriteExtensions
{
    /// <summary>
    /// Configures URL rewriting rules from application configuration.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for method chaining.</returns>
    public static WebApplication UseRewriteRules(
        this WebApplication app)
    {
        // Load rewrite rules from configuration.
        var rewriteRules = app.Configuration
            .GetSection("RewriteRules")
            .Get<RewriteRule[]>();

        // If no rules are configured, do nothing.
        if (rewriteRules?.Length is null or <= 0) return app;

        // Create rewrite options and add each configured rule.
        var rewriteOptions = new RewriteOptions();

        Array.ForEach(rewriteRules, rule =>
        {
            rewriteOptions.AddRewrite(rule.Regex, rule.Replacement, rule.SkipRemainingRules);
        });

        // Add the URL rewriting middleware to the pipeline.
        app.UseRewriter(rewriteOptions);

        return app;
    }

    /// <summary>
    /// Represents a URL rewriting rule configuration.
    /// </summary>
    /// <param name="Regex">The regular expression pattern.</param>
    /// <param name="Replacement">The replacement pattern.</param>
    /// <param name="SkipRemainingRules">Whether to skip processing remaining rules.</param>
    private record RewriteRule(
        string Regex,
        string Replacement,
        bool SkipRemainingRules);
}
