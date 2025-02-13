using Trelnex.Core.Api.Authentication;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

internal class RBACPermission : JwtBearerPermission
{
    protected override string ConfigSectionName => "Auth:trelnex-api-rbac";

    public override string JwtBearerScheme => "Bearer.trelnex-api-rbac";

    public override void AddAuthorization(
        IPoliciesBuilder policiesBuilder)
    {
        policiesBuilder
            .AddPolicy<RBACCreatePolicy>()
            .AddPolicy<RBACReadPolicy>()
            .AddPolicy<RBACUpdatePolicy>()
            .AddPolicy<RBACDeletePolicy>();
    }

    public class RBACCreatePolicy : IPermissionPolicy
    {
        public string[] RequiredRoles => ["rbac.create"];
    }

    public class RBACReadPolicy : IPermissionPolicy
    {
        public string[] RequiredRoles => ["rbac.read"];
    }

    public class RBACUpdatePolicy : IPermissionPolicy
    {
        public string[] RequiredRoles => ["rbac.update"];
    }

    public class RBACDeletePolicy : IPermissionPolicy
    {
        public string[] RequiredRoles => ["rbac.delete"];
    }
}
