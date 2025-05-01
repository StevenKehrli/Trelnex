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
        // create the converter
        var converter = new ExpressionConverter<ITestItem, TestItem>();

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

        // create an expression using ITestItem
        // this expression uses a method call (MessageValue) to get the comparison value
        Expression<Func<ITestItem, bool>> predicate = item => item.Message == MessageValue();

        // convert to an expression using TestItem
        var expression = converter.Convert(predicate);

        // execute the converted expression against the items collection
        var selected = items.AsQueryable().Where(expression).ToArray();

        // use Snapshooter to match the selected items with the expected output
        Snapshot.Match(selected);
    }

    [Test]
    [Description("Tests that ExpressionConverter can convert expressions that match on numeric ID property")]
    public void ExpressionConverter_WherePropertyMatchOne()
    {
        // create the converter
        var converter = new ExpressionConverter<ITestItem, TestItem>();

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

        // create an expression using ITestItem
        // this expression checks if the Id property equals 1
        Expression<Func<ITestItem, bool>> predicate = item => item.Id == 1;

        // convert to an expression using TestItem
        var expression = converter.Convert(predicate);

        // execute the converted expression against the items collection
        var selected = items.AsQueryable().Where(expression).ToArray();

        // use Snapshooter to match the selected items with the expected output
        Snapshot.Match(selected);
    }

    [Test]
    [Description("Tests that ExpressionConverter can convert expressions that match on string property")]
    public void ExpressionConverter_WherePropertyMatchTwo()
    {
        // create the converter
        var converter = new ExpressionConverter<ITestItem, TestItem>();

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

        // create an expression using ITestItem
        // this expression checks if the Message property equals "yes"
        Expression<Func<ITestItem, bool>> predicate = item => item.Message == "yes";

        // convert to an expression using TestItem
        var expression = converter.Convert(predicate);

        // execute the converted expression against the items collection
        var selected = items.AsQueryable().Where(expression).ToArray();

        // use Snapshooter to match the selected items with the expected output
        Snapshot.Match(selected);
    }
}
