using System.Reflection;

namespace Trelnex.Core.Data.Tests.CommandProviders;

/// <summary>
/// Tests for the InMemoryCommandProvider implementation.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="CommandProviderTests"/> to leverage the extensive test suite
/// defined in the base class. The base class implements a comprehensive set of tests for command provider
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
/// By inheriting from CommandProviderTests, this class runs all those tests against the InMemoryCommandProvider
/// implementation specifically. It only needs to:
/// <list type="number">
/// <item>Setup the InMemoryCommandProvider instance in TestFixtureSetup</item>
/// <item>Clean up data between tests using the TestCleanup method</item>
/// </list>
/// </remarks>
[Category("InMemoryCommandProvider")]
public class InMemoryCommandProviderTests : CommandProviderTests
{
    private MethodInfo? _clearMethod = null!;

    /// <summary>
    /// Sets up the InMemoryCommandProvider for testing.
    /// </summary>
    /// <remarks>
    /// This method initializes the command provider that will be tested by all the test methods
    /// inherited from <see cref="CommandProviderTests"/>. It also captures the Clear method
    /// via reflection to allow cleaning up between tests.
    /// </remarks>
    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        // Create our command provider.
        var factory = await InMemoryCommandProviderFactory.Create();

        _commandProvider =
            factory.Create<ITestItem, TestItem>(
                typeName: "test-item",
                TestItem.Validator,
                CommandOperations.All);

        // Use reflection to get the Clear method from the underlying InMemoryCommandProvider.
        // This is necessary because the Clear method is non-public.
        _clearMethod = _commandProvider
            .GetType()
            .GetMethod(
                "Clear",
                BindingFlags.Instance | BindingFlags.NonPublic);
    }

    /// <summary>
    /// Cleans up the InMemoryCommandProvider after each test.
    /// </summary>
    /// <remarks>
    /// This method ensures test isolation by clearing all data from the InMemoryCommandProvider
    /// after each test runs. This prevents state from one test affecting subsequent tests.
    /// </remarks>
    [TearDown]
    public void TestCleanup()
    {
        // Clear the data in the InMemoryCommandProvider to ensure a clean state for each test.
        _clearMethod?.Invoke(_commandProvider, null);
    }
}
