using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Provides an endpoint for deleting scopes from resources in the RBAC system.
/// </summary>
/// <remarks>
/// This endpoint allows administrators to remove authorization boundaries (scopes) from
/// protected resources. When a scope is deleted, all role assignments that reference this
/// scope are also removed, effectively revoking permissions granted within that authorization
/// boundary. The endpoint validates both the resource name and scope name before proceeding
/// with the deletion operation.
///
/// This operation is typically used during:
/// - Cleanup of deprecated environments or regions
/// - Reorganization of the authorization model
/// - Removal of testing or temporary authorization boundaries
///
/// Note that scope deletion is a significant operation that affects all principals with
/// role assignments in that scope and cannot be undone. Deleted scopes must be recreated
/// if needed again in the future.
/// </remarks>
internal static class DeleteScopeEndpoint
{
    #region Private Static Fields

    /// <summary>
    /// Pre-configured validation exception for the request is not valid.
    /// </summary>
    private static readonly ValidationException _validationException = new(
        $"The '{typeof(DeleteScopeRequest).Name}' is not valid.");

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Maps the endpoint for deleting a scope to the API's routing configuration.
    /// </summary>
    /// <param name="erb">The endpoint route builder to configure the endpoint on.</param>
    /// <remarks>
    /// This method configures a DELETE endpoint at "/scopes" that accepts a <see cref="DeleteScopeRequest"/>
    /// in JSON format. The endpoint requires the <see cref="RBACPermission.RBACDeletePolicy"/> permission,
    /// ensuring that only authorized administrators can delete scopes. The method defines the possible
    /// response types, including successful completion and various error scenarios.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the DELETE endpoint for deleting a scope to "/scopes".
        erb.MapDelete(
                "/scopes",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACDeletePolicy>()
            .Accepts<DeleteScopeRequest>(MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("DeleteScope")
            .WithDescription("Deletes the specified scope")
            .WithTags("Scopes");
    }

    /// <summary>
    /// Handles the scope deletion request by validating inputs and performing the deletion operation.
    /// </summary>
    /// <param name="rbacRepository">The repository for RBAC operations.</param>
    /// <param name="resourceNameValidator">The validator for resource names.</param>
    /// <param name="scopeNameValidator">The validator for scope names.</param>
    /// <param name="parameters">The request parameters containing the scope deletion details.</param>
    /// <returns>An HTTP result indicating the outcome of the operation.</returns>
    /// <remarks>
    /// This method processes a scope deletion request by:
    /// 1. Validating the resource name using the resource name validator
    /// 2. Validating the scope name using the scope name validator
    /// 3. Deleting the scope from the RBAC repository if validations pass
    ///
    /// If validation fails, an appropriate exception is thrown, which will be transformed
    /// into an HTTP error response.
    /// </remarks>
    /// <exception cref="ValidationException">
    /// Thrown when the resource name or scope name validation fails.
    /// </exception>
    public static async Task<IResult> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [FromBody] DeleteScopeRequest? request)
    {
        // Validate the request.
        if (request is null) throw _validationException;
        if (request.ResourceName is null) throw _validationException;
        if (request.ScopeName is null) throw _validationException;

        // Delete the scope.
        await rbacRepository.DeleteScopeAsync(
            resourceName: request.ResourceName,
            scopeName: request.ScopeName);

        // Return a 200 OK result.
        return Results.Ok();
    }

    #endregion
}
