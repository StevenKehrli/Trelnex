namespace Trelnex.Core.Data.Tests;

/// <summary>
/// Provides utilities for creating test request contexts for unit testing.
/// </summary>
internal static class TestRequestContext
{
    /// <summary>
    /// Creates an instance of <see cref="IRequestContext"/> with randomly generated values
    /// that can be used in unit tests to simulate a real request context.
    /// </summary>
    /// <returns>
    /// A new instance of <see cref="IRequestContext"/> containing random values for its properties.
    /// </returns>
    public static IRequestContext Create()
    {
        return new RequestContext(
            ObjectId: Guid.NewGuid().ToString(),
            HttpTraceIdentifier: Guid.NewGuid().ToString(),
            HttpRequestPath: Guid.NewGuid().ToString());
    }

    /// <summary>
    /// A private implementation of the <see cref="IRequestContext"/> interface used for testing purposes.
    /// </summary>
    /// <param name="ObjectId">The unique identifier associated with the user's ClaimsPrincipal for this request.</param>
    /// <param name="HttpTraceIdentifier">The unique identifier used for tracing and correlating log entries for this request.</param>
    /// <param name="HttpRequestPath">The relative path of the requested resource, excluding query string parameters.</param>
    /// <remarks>
    /// This record provides a lightweight implementation of <see cref="IRequestContext"/> 
    /// that can be easily instantiated with test values.
    /// </remarks>
    private record RequestContext(
        string? ObjectId,
        string? HttpTraceIdentifier,
        string? HttpRequestPath) : IRequestContext;
}
