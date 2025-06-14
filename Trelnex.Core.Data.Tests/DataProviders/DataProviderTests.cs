namespace Trelnex.Core.Data.Tests.DataProviders;

[Category("DataProviders")]
public abstract partial class DataProviderTests
{
    protected IDataProvider<ITestItem> _dataProvider = null!;
}
