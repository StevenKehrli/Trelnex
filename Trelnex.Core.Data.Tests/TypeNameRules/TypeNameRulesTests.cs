namespace Trelnex.Core.Data.Tests.TypeNameRules;

public class TypeNameRulesTests
{
    [Test]
    public void TypeNameRules_HyphenEnd()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () =>
            {
                var factory = await InMemoryCommandProviderFactory.Create();

                var commandProvider = factory.Create<ITestItem, TestItem>(
                    typeName: "end-");
            },
            "The type 'end-' does not follow the naming rules: lowercase letters and hyphens; start and end with a lowercase letter.");
    }

    [Test]
    public void TypeNameRules_HyphenStart()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () =>
            {
                var factory = await InMemoryCommandProviderFactory.Create();

                var commandProvider = factory.Create<ITestItem, TestItem>(
                    typeName: "-start");
            },
            "The type '-start' does not follow the naming rules: lowercase letters and hyphens; start and end with a lowercase letter.");
    }

    [Test]
    public void TypeNameRules_Number()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () =>
            {
                var factory = await InMemoryCommandProviderFactory.Create();

                var commandProvider = factory.Create<ITestItem, TestItem>(
                    typeName: "number1");
            },
            $"The type 'number1' does not follow the naming rules: lowercase letters and hyphens; start and end with a lowercase letter.");
    }

    [Test]
    public void TypeNameRules_Reserved()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () =>
            {
                var factory = await InMemoryCommandProviderFactory.Create();

                var commandProvider = factory.Create<ITestItem, TestItem>(
                    typeName: "event");
            },
            $"The typeName 'event' is a reserved type name.");
    }

    [Test]
    public void TypeNameRules_Underscore()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () =>
            {
                var factory = await InMemoryCommandProviderFactory.Create();

                var commandProvider = factory.Create<ITestItem, TestItem>(
                    typeName: "snake_case");
            },
            $"The type 'snake_case' does not follow the naming rules: lowercase letters and hyphens; start and end with a lowercase letter.");
    }

    [Test]
    public void TypeNameRules_UpperCase()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () =>
            {
                var factory = await InMemoryCommandProviderFactory.Create();

                var commandProvider = factory.Create<ITestItem, TestItem>(
                    typeName: "UpperCase");
            },
            $"The type 'UpperCase' does not follow the naming rules: lowercase letters and hyphens; start and end with a lowercase letter.");
    }

}
