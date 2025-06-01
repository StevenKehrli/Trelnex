namespace Trelnex.Core.Data.Tests.CommandProviders;

[Category("CommandProviders")]
public abstract partial class CommandProviderTests
{
    protected ICommandProvider<ITestItem> _commandProvider = null!;
}
