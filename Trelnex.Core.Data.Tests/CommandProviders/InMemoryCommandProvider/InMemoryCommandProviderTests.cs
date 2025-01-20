using System.Reflection;

namespace Trelnex.Core.Data.Tests.CommandProviders;

public class InMemoryCommandProviderTests : CommandProviderTests
{
    private MethodInfo? _clearMethod = null!;

    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        _commandProvider =
            factory.Create<ITestItem, TestItem>(
                typeName: "test-item",
                TestItem.Validator,
                CommandOperations.All);

        // use reflection to get the Clear method from the underlying InMemoryCommandProvider
        _clearMethod = _commandProvider
            .GetType()
            .GetMethod(
                "Clear",
                BindingFlags.Instance | BindingFlags.NonPublic);
    }

    [TearDown]
    public void TestCleanup()
    {
        // This method is called after each test case is run.

        // clear
        _clearMethod?.Invoke(_commandProvider, null);
    }
}
