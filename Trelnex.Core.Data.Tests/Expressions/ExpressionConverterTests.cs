using System.Linq.Expressions;
using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.Expressions;

[Category("Expressions")]
public class ExpressionConverterTests
{
    private string MessageValue()
    {
        return "yes";
    }

    [Test]
    [Description("Tests that ExpressionConverter can convert expressions that use method calls")]
    public void ExpressionConverter_WherePropertyMatchMethod()
    {
        // Create the converter for translating between interface and implementation
        var converter = new ExpressionConverter<ITestItem, TestItem>();

        // Set up test data with varied message values
        TestItem[] items = [
            new TestItem
            {
                Id = 1,
                Message = "yes"
            },
            new TestItem
            {
                Id = 2,
                Message = "no"
            },
            new TestItem
            {
                Id = 3,
                Message = "yes"
            }
        ];

        // Create an expression using ITestItem that utilizes a method call
        // This tests whether the converter can handle expressions with external method invocations
        Expression<Func<ITestItem, bool>> predicate = item => item.Message == MessageValue();

        // Convert the expression to work with the concrete TestItem type
        var expression = converter.Convert(predicate);

        // Apply the converted expression to filter the collection
        var selected = items.AsQueryable().Where(expression).ToArray();

        // Verify that only items with Message="yes" are selected (items 1 and 3)
        Snapshot.Match(selected);
    }

    [Test]
    [Description("Tests that ExpressionConverter can convert expressions that match on numeric id property")]
    public void ExpressionConverter_WherePropertyMatchOne()
    {
        // Create the converter for translating between interface and implementation
        var converter = new ExpressionConverter<ITestItem, TestItem>();

        // Set up test data with different id values
        TestItem[] items = [
            new TestItem
            {
                Id = 1,
                Message = "yes"
            },
            new TestItem
            {
                Id = 2,
                Message = "no"
            },
            new TestItem
            {
                Id = 3,
                Message = "yes"
            }
        ];

        // Create an expression using ITestItem that filters by numeric id
        // This tests basic property equality comparison with a constant value
        Expression<Func<ITestItem, bool>> predicate = item => item.Id == 1;

        // Convert the expression to work with the concrete TestItem type
        var expression = converter.Convert(predicate);

        // Apply the converted expression to filter the collection
        var selected = items.AsQueryable().Where(expression).ToArray();

        // Verify that only the item with id=1 is selected
        Snapshot.Match(selected);
    }

    [Test]
    [Description("Tests that ExpressionConverter can convert expressions that match on string message property")]
    public void ExpressionConverter_WherePropertyMatchTwo()
    {
        // Create the converter for translating between interface and implementation
        var converter = new ExpressionConverter<ITestItem, TestItem>();

        // Set up test data with varied message values
        TestItem[] items = [
            new TestItem
            {
                Id = 1,
                Message = "yes"
            },
            new TestItem
            {
                Id = 2,
                Message = "no"
            },
            new TestItem
            {
                Id = 3,
                Message = "yes"
            }
        ];

        // Create an expression using ITestItem that filters by string content
        // This tests basic property equality comparison with a string literal
        Expression<Func<ITestItem, bool>> predicate = item => item.Message == "yes";

        // Convert the expression to work with the concrete TestItem type
        var expression = converter.Convert(predicate);

        // Apply the converted expression to filter the collection
        var selected = items.AsQueryable().Where(expression).ToArray();

        // Verify that only items with Message="yes" are selected (items 1 and 3)
        Snapshot.Match(selected);
    }
}
