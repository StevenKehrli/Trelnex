using Azure.Core;

namespace Trelnex.Core.Azure.CommandProviders;

internal record KeyResolverOptions(
    TokenCredential TokenCredential);
