using System.Linq.Expressions;
using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.Expressions;

public class ExpressionConverterTests
{
    private string MessageValue()
    {
        return "yes";
    }

    [Test]
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
        Expression<Func<ITestItem, bool>> predicate = item => item.Message == MessageValue();

        // convert to an expression using TestItem
        var expression = converter.Convert(predicate);

        // execute
        var selected = items.AsQueryable().Where(expression).ToArray();

        Snapshot.Match(selected);
    }

    [Test]
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
        Expression<Func<ITestItem, bool>> predicate = item => item.Id == 1;

        // convert to an expression using TestItem
        var expression = converter.Convert(predicate);

        // execute
        var selected = items.AsQueryable().Where(expression).ToArray();

        Snapshot.Match(selected);
    }

    [Test]
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
        Expression<Func<ITestItem, bool>> predicate = item => item.Message == "yes";

        // convert to an expression using TestItem
        var expression = converter.Convert(predicate);

        // execute
        var selected = items.AsQueryable().Where(expression).ToArray();

        Snapshot.Match(selected);
    }
}
