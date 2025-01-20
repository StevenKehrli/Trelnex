namespace Trelnex.Core.Data.Tests;

internal static class TestRequestContext
{
    /// Create a <see cref="IRequestContext"/> to mock the request context.
    /// </summary>
    /// <returns></returns>
    public static IRequestContext Create()
    {
        return new RequestContext(
            ObjectId: Guid.NewGuid().ToString(),
            HttpTraceIdentifier: Guid.NewGuid().ToString(),
            HttpRequestPath: Guid.NewGuid().ToString());
    }

    /// <summary>
    /// Represents the request context
    /// </summary>
    /// <param name="ObjectId">Gets the unique object ID associated with the ClaimsPrincipal for this request.</param>
    /// <param name="HttpTraceIdentifier">Gets the unique identifier to represent this request in trace logs.</param>
    /// <param name="HttpRequestPath">Gets the portion of the request path that identifies the requested resource.</param>
    private record RequestContext(
        string? ObjectId,
        string? HttpTraceIdentifier,
        string? HttpRequestPath) : IRequestContext;
}
