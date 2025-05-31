namespace Trelnex.Core.Azure.Identity;

/// <summary>
/// Specifies the source of Azure credentials.
/// </summary>
/// <remarks>
/// Defines the available credential sources for Azure authentication.
/// </remarks>
public enum CredentialSource
{
    /// <summary>
    /// Uses Workload Identity, suitable for Kubernetes and Azure services.
    /// </summary>
    WorkloadIdentity,

    /// <summary>
    /// Uses credentials from the Azure CLI, suitable for development.
    /// </summary>
    AzureCli,
}
