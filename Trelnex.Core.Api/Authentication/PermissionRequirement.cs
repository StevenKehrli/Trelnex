using Microsoft.AspNetCore.Authorization;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Represents a requirement for a specific permission policy.
/// Implements the <see cref="IAuthorizationRequirement"/> interface.
/// </summary>
internal class PermissionRequirement : IAuthorizationRequirement;
