using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;

namespace Trelnex.Core.Api.Rewrite;

/// <summary>
/// Extension method to add the rewrite rules to the <see cref="WebApplication"/>.
/// </summary>
public static class RewriteExtensions
{
    /// <summary>
    /// Add the rewrite rules to the specified <see cref="WebApplication"/>.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to add the Swagger endpoints to.</param>
    /// <returns>The <see cref="WebApplication"/>.</returns>
    public static WebApplication UseRewriteRules(
        this WebApplication app)
    {
        // get the rewrite rules, if any
        var rewriteRules = app.Configuration.GetSection("RewriteRules").Get<RewriteRule[]>();
        if (rewriteRules?.Length is null or <= 0) return app;

        var rewriteOptions = new RewriteOptions();

        Array.ForEach(rewriteRules, rule =>
        {
            rewriteOptions.AddRewrite(rule.Regex, rule.Replacement, rule.SkipRemainingRules);
        });

        app.UseRewriter(rewriteOptions);

        return app;
    }

    /// <summary>
    /// Represents the rewrite rule.
    /// </summary>
    /// <param name="Regex">The regex string to compare with.</param>
    /// <param name="Replacement">If the regex matches, what to replace the uri with.</param>
    /// <param name="SkipRemainingRules">If the regex matches, conditionally stop processing other rules.</param>
    private record RewriteRule(
        string Regex,
        string Replacement,
        bool SkipRemainingRules);
}
