using System.Reflection;

namespace Trelnex.Core.Data.Tests.DataProviders;

/// <summary>
/// Tests for the InMemoryDataProvider implementation.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="DataProviderTests"/> to leverage the extensive test suite
/// defined in the base class. The base class implements a comprehensive set of tests for data provider
/// functionality including:
/// <list type="bullet">
/// <item>Batch command operations (create, update, delete with success and failure scenarios)</item>
/// <item>Create command operations (with success and conflict handling)</item>
/// <item>Delete command operations (with success and precondition failure handling)</item>
/// <item>Query command operations (with various filters, ordering, paging)</item>
/// <item>Read command operations</item>
/// <item>Update command operations (with success and precondition failure handling)</item>
/// </list>
///
/// By inheriting from DataProviderTests, this class runs all those tests against the InMemoryDataProvider
/// implementation specifically. It only needs to:
/// <list type="number">
/// <item>Setup the InMemoryDataProvider instance in TestFixtureSetup</item>
/// <item>Clean up data between tests using the TestCleanup method</item>
/// </list>
/// </remarks>
[Category("InMemoryDataProvider")]
public class InMemoryDataProviderTests : DataProviderTests
{
    private MethodInfo? _clearMethod = null!;

    /// <summary>
    /// Sets up the InMemoryDataProvider for testing.
    /// </summary>
    /// <remarks>
    /// This method initializes the data provider that will be tested by all the test methods
    /// inherited from <see cref="DataProviderTests"/>. It also captures the Clear method
    /// via reflection to allow cleaning up between tests.
    /// </remarks>
    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        // Create our data provider.
        var factory = await InMemoryDataProviderFactory.Create();

        _dataProvider =
            factory.Create<ITestItem, TestItem>(
                typeName: "test-item",
                TestItem.Validator,
                CommandOperations.All);

        // Use reflection to get the Clear method from the underlying InMemoryDataProvider.
        // This is necessary because the Clear method is non-public.
        _clearMethod = _dataProvider
            .GetType()
            .GetMethod(
                nameof(InMemoryDataProvider<ITestItem, TestItem>.Clear),
                BindingFlags.Instance | BindingFlags.NonPublic);
    }

    /// <summary>
    /// Cleans up the InMemoryDataProvider after each test.
    /// </summary>
    /// <remarks>
    /// This method ensures test isolation by clearing all data from the InMemoryDataProvider
    /// after each test runs. This prevents state from one test affecting subsequent tests.
    /// </remarks>
    [TearDown]
    public void TestCleanup()
    {
        // Clear the data in the InMemoryDataProvider to ensure a clean state for each test.
        _clearMethod?.Invoke(_dataProvider, null);
    }
}
