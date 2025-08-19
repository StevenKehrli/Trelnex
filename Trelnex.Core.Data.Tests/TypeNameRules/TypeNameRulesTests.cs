namespace Trelnex.Core.Data.Tests.TypeNameRules;

[Category("TypeNameRules")]
public class TypeNameRulesTests
{
    [Test]
    [Description("Tests that type names cannot end with a hyphen")]
    public void TypeNameRules_HyphenEnd()
    {
        // Attempt to create a data provider with an invalid type name ending with a hyphen
        // This should throw an ArgumentException
        Assert.ThrowsAsync<ArgumentException>(
            async () =>
            {
                var factory = await InMemoryDataProviderFactory.Create();

                var dataProvider = factory.Create<TestItem>(
                    typeName: "end-");
            },
            "The type 'end-' does not follow the naming rules: lowercase letters and hyphens; start and end with a lowercase letter.");
    }

    [Test]
    [Description("Tests that type names cannot start with a hyphen")]
    public void TypeNameRules_HyphenStart()
    {
        // Attempt to create a data provider with an invalid type name starting with a hyphen
        // This should throw an ArgumentException
        Assert.ThrowsAsync<ArgumentException>(
            async () =>
            {
                var factory = await InMemoryDataProviderFactory.Create();

                var dataProvider = factory.Create<TestItem>(
                    typeName: "-start");
            },
            "The type '-start' does not follow the naming rules: lowercase letters and hyphens; start and end with a lowercase letter.");
    }

    [Test]
    [Description("Tests that type names cannot contain numbers")]
    public void TypeNameRules_Number()
    {
        // Attempt to create a data provider with an invalid type name containing numbers
        // This should throw an ArgumentException
        Assert.ThrowsAsync<ArgumentException>(
            async () =>
            {
                var factory = await InMemoryDataProviderFactory.Create();

                var dataProvider = factory.Create<TestItem>(
                    typeName: "number1");
            },
            $"The type 'number1' does not follow the naming rules: lowercase letters and hyphens; start and end with a lowercase letter.");
    }

    [Test]
    [Description("Tests that reserved words cannot be used as type names")]
    public void TypeNameRules_Reserved()
    {
        // Attempt to create a data provider with a reserved word as type name
        // This should throw an ArgumentException
        Assert.ThrowsAsync<ArgumentException>(
            async () =>
            {
                var factory = await InMemoryDataProviderFactory.Create();

                var dataProvider = factory.Create<TestItem>(
                    typeName: "event");
            },
            $"The typeName 'event' is a reserved type name.");
    }

    [Test]
    [Description("Tests that type names cannot contain underscores")]
    public void TypeNameRules_Underscore()
    {
        // Attempt to create a data provider with an invalid type name containing underscores
        // This should throw an ArgumentException
        Assert.ThrowsAsync<ArgumentException>(
            async () =>
            {
                var factory = await InMemoryDataProviderFactory.Create();

                var dataProvider = factory.Create<TestItem>(
                    typeName: "snake_case");
            },
            $"The type 'snake_case' does not follow the naming rules: lowercase letters and hyphens; start and end with a lowercase letter.");
    }

    [Test]
    [Description("Tests that type names must be all lowercase")]
    public void TypeNameRules_UpperCase()
    {
        // Attempt to create a data provider with an invalid type name containing uppercase letters
        // This should throw an ArgumentException
        Assert.ThrowsAsync<ArgumentException>(
            async () =>
            {
                var factory = await InMemoryDataProviderFactory.Create();

                var dataProvider = factory.Create<TestItem>(
                    typeName: "UpperCase");
            },
            $"The type 'UpperCase' does not follow the naming rules: lowercase letters and hyphens; start and end with a lowercase letter.");
    }
}
