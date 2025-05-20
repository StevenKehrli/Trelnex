using Microsoft.AspNetCore.Authorization;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// An attribute that specifies the required permission policy for a method.
/// </summary>
/// <remarks>
/// This attribute is used to enforce authorization based on predefined permission policies.
/// It can be applied multiple times to a method to require multiple policies.
/// </remarks>
/// <param name="policy">The name of the permission policy to enforce.</param>
internal class PermissionAttribute(string policy) : AuthorizeAttribute(policy);
