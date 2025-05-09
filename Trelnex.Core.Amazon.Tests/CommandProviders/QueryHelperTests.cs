using System.Text.Json.Serialization;
using Snapshooter.NUnit;
using Trelnex.Core.Amazon.CommandProviders;

namespace Trelnex.Core.Amazon.Tests.CommandProviders;

[TestFixture]
[Category("QueryHelper")]
public class QueryHelperTests
{
    [Test]
    [Description("Tests QueryHelper with a Where clause where Id equals a constant value.")]
    public void QueryHelper_Where_Id_Equal_Constant()
    {
        // Create an empty queryable of TestItem.
        var q = Enumerable.Empty<TestItem>().AsQueryable();

        // Apply a Where clause to filter items where Id equals 1.
        q = q.Where(x => x.Id == 1);

        // Create a QueryHelper from the LINQ expression.
        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        // Match the QueryHelper's JSON representation against a snapshot.
        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    [Description("Tests QueryHelper with a Where clause where Id equals the result of a method call.")]
    public void QueryHelper_Where_Id_Equal_Method()
    {
        // Create an empty queryable of TestItem.
        var q = Enumerable.Empty<TestItem>().AsQueryable();

        // Apply a Where clause to filter items where Id equals the result of GetMaxId().
        q = q.Where(x => x.Id == TestItem.GetMaxId());

        // Create a QueryHelper from the LINQ expression.
        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        // Match the QueryHelper's JSON representation against a snapshot.
        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    [Description("Tests QueryHelper with a Where clause where Id is greater than a constant value.")]
    public void QueryHelper_Where_Id_GreaterThan_Constant()
    {
        // Create an empty queryable of TestItem.
        var q = Enumerable.Empty<TestItem>().AsQueryable();

        // Apply a Where clause to filter items where Id is greater than 1.
        q = q.Where(x => x.Id > 1);

        // Create a QueryHelper from the LINQ expression.
        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        // Match the QueryHelper's JSON representation against a snapshot.
        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    [Description("Tests QueryHelper with a Where clause where Id is greater than the result of a method call.")]
    public void QueryHelper_Where_Id_GreaterThan_Method()
    {
        // Create an empty queryable of TestItem.
        var q = Enumerable.Empty<TestItem>().AsQueryable();

        // Apply a Where clause to filter items where Id is greater than the result of GetMaxId().
        q = q.Where(x => x.Id > TestItem.GetMaxId());

        // Create a QueryHelper from the LINQ expression.
        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        // Match the QueryHelper's JSON representation against a snapshot.
        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    [Description("Tests QueryHelper with a Where clause where Id is greater than or equal to a constant value.")]
    public void QueryHelper_Where_Id_GreaterThanOrEqual_Constant()
    {
        // Create an empty queryable of TestItem.
        var q = Enumerable.Empty<TestItem>().AsQueryable();

        // Apply a Where clause to filter items where Id is greater than or equal to 1.
        q = q.Where(x => x.Id >= 1);

        // Create a QueryHelper from the LINQ expression.
        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        // Match the QueryHelper's JSON representation against a snapshot.
        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    [Description("Tests QueryHelper with a Where clause where Id is greater than or equal to the result of a method call.")]
    public void QueryHelper_Where_Id_GreaterThanOrEqual_Method()
    {
        // Create an empty queryable of TestItem.
        var q = Enumerable.Empty<TestItem>().AsQueryable();

        // Apply a Where clause to filter items where Id is greater than or equal to the result of GetMaxId().
        q = q.Where(x => x.Id >= TestItem.GetMaxId());

        // Create a QueryHelper from the LINQ expression.
        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        // Match the QueryHelper's JSON representation against a snapshot.
        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    [Description("Tests QueryHelper with a Where clause where Id is less than a constant value.")]
    public void QueryHelper_Where_Id_LessThan_Constant()
    {
        // Create an empty queryable of TestItem.
        var q = Enumerable.Empty<TestItem>().AsQueryable();

        // Apply a Where clause to filter items where Id is less than to 1.
        q = q.Where(x => x.Id < 1);

        // Create a QueryHelper from the LINQ expression.
        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        // Match the QueryHelper's JSON representation against a snapshot.
        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    [Description("Tests QueryHelper with a Where clause where Id is less than the result of a method call.")]
    public void QueryHelper_Where_Id_LessThan_Method()
    {
        // Create an empty queryable of TestItem.
        var q = Enumerable.Empty<TestItem>().AsQueryable();

        // Apply a Where clause to filter items where Id is less than the result of GetMaxId().
        q = q.Where(x => x.Id < TestItem.GetMaxId());

        // Create a QueryHelper from the LINQ expression.
        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        // Match the QueryHelper's JSON representation against a snapshot.
        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    [Description("Tests QueryHelper with a Where clause where Id is less than or equal to a constant value.")]
    public void QueryHelper_Where_Id_LessThanOrEqual_Constant()
    {
        // Create an empty queryable of TestItem.
        var q = Enumerable.Empty<TestItem>().AsQueryable();

        // Apply a Where clause to filter items where Id is less than or equal to 1.
        q = q.Where(x => x.Id <= 1);

        // Create a QueryHelper from the LINQ expression.
        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        // Match the QueryHelper's JSON representation against a snapshot.
        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    [Description("Tests QueryHelper with a Where clause where Id is less than or equal to the result of a method call.")]
    public void QueryHelper_Where_Id_LessThanOrEqual_Method()
    {
        // Create an empty queryable of TestItem.
        var q = Enumerable.Empty<TestItem>().AsQueryable();

        // Apply a Where clause to filter items where Id is less than or equal to the result of GetMaxId().
        q = q.Where(x => x.Id <= TestItem.GetMaxId());

        // Create a QueryHelper from the LINQ expression.
        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        // Match the QueryHelper's JSON representation against a snapshot.
        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    [Description("Tests QueryHelper with a Where clause where Message starts with a constant string.")]
    public void QueryHelper_Where_Message_BeginsWith_Constant()
    {
        // Create an empty queryable of TestItem.
        var q = Enumerable.Empty<TestItem>().AsQueryable();

        // Apply a Where clause to filter items where Message starts with 'ab'.
        q = q.Where(x => x.Message.StartsWith("ab"));

        // Create a QueryHelper from the LINQ expression.
        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        // Match the QueryHelper's JSON representation against a snapshot.
        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    [Description("Tests QueryHelper with a Where clause where Message starts with the result of a method call.")]
    public void QueryHelper_Where_Message_BeginsWith_Method()
    {
        // Create an empty queryable of TestItem.
        var q = Enumerable.Empty<TestItem>().AsQueryable();

        // Apply a Where clause to filter items where Message starts with the result of GetPrefix().
        q = q.Where(x => x.Message.StartsWith(TestItem.GetPrefix()));

        // Create a QueryHelper from the LINQ expression.
        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        // Match the QueryHelper's JSON representation against a snapshot.
        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    [Description("Tests QueryHelper with a Where clause where Message contains a constant string.")]
    public void QueryHelper_Where_Message_Contains_Constant()
    {
        // Create an empty queryable of TestItem.
        var q = Enumerable.Empty<TestItem>().AsQueryable();

        // Apply a Where clause to filter items where Message contains 'ab'.
        q = q.Where(x => x.Message.Contains("ab"));

        // Create a QueryHelper from the LINQ expression.
        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        // Match the QueryHelper's JSON representation against a snapshot.
        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    [Description("Tests QueryHelper with a Where clause where Message contains the result of a method call.")]
    public void QueryHelper_Where_Message_Contains_Method()
    {
        // Create an empty queryable of TestItem.
        var q = Enumerable.Empty<TestItem>().AsQueryable();

        // Apply a Where clause to filter items where Message contains the result of GetPrefix().
        q = q.Where(x => x.Message.Contains(TestItem.GetPrefix()));

        // Create a QueryHelper from the LINQ expression.
        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        // Match the QueryHelper's JSON representation against a snapshot.
        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    [Description("Tests QueryHelper with a Where clause where Message is not null.")]
    public void QueryHelper_Where_Message_IsNotNull()
    {
        // Create an empty queryable of TestItem.
        var q = Enumerable.Empty<TestItem>().AsQueryable();

        // Apply a Where clause to filter items where Message is not null.
        q = q.Where(x => x.Message != null);

        // Create a QueryHelper from the LINQ expression.
        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        // Match the QueryHelper's JSON representation against a snapshot.
        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    [Description("Tests QueryHelper with a Where clause where Message is null.")]
    public void QueryHelper_Where_Message_IsNull()
    {
        // Create an empty queryable of TestItem.
        var q = Enumerable.Empty<TestItem>().AsQueryable();

        // Apply a Where clause to filter items where Message is null.
        q = q.Where(x => x.Message == null);

        // Create a QueryHelper from the LINQ expression.
        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        // Match the QueryHelper's JSON representation against a snapshot.
        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    [Description("Tests QueryHelper with multiple Where clauses chained together.")]
    public void QueryHelper_Where_Multiple()
    {
        // Create an empty queryable of TestItem.
        var q = Enumerable.Empty<TestItem>().AsQueryable();

        // Apply multiple Where clauses to filter items where Id equals 1 and Message equals 'yes'.
        q = q.Where(x => x.Id == 1).Where(x => x.Message == "yes");

        // Create a QueryHelper from the LINQ expression.
        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        // Match the QueryHelper's JSON representation against a snapshot.
        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    [Description("Tests QueryHelper with Where, OrderBy, Skip, and Take LINQ expressions.")]
    public void QueryHelper_Where_PlusLinqExpressions()
    {
        // Create an empty queryable of TestItem.
        var q = Enumerable.Empty<TestItem>().AsQueryable();

        // Apply Where, OrderBy, Skip, and Take LINQ expressions.
        q = q.Where(x => x.Id == 1).OrderBy(x => x.Id).Skip(1).Take(2);

        // Create a QueryHelper from the LINQ expression.
        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        // Match the QueryHelper's JSON representation against a snapshot.
        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    [Description("Tests QueryHelper with a Where clause where Values contains a constant integer.")]
    public void QueryHelper_Where_Values_Contains_Constant()
    {
        // Create an empty queryable of TestItem.
        var q = Enumerable.Empty<TestItem>().AsQueryable();

        // Apply a Where clause to filter items where Values contains 1.
        q = q.Where(x => x.Values.Contains(1));

        // Create a QueryHelper from the LINQ expression.
        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        // Match the QueryHelper's JSON representation against a snapshot.
        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    [Description("Tests QueryHelper with a Where clause where Values contains the result of a method call.")]
    public void QueryHelper_Where_Values_Contains_Method()
    {
        // Create an empty queryable of TestItem.
        var q = Enumerable.Empty<TestItem>().AsQueryable();

        // Apply a Where clause to filter items where Values contains the result of GetMaxId().
        q = q.Where(x => x.Values.Contains(TestItem.GetMaxId()));

        // Create a QueryHelper from the LINQ expression.
        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        // Match the QueryHelper's JSON representation against a snapshot.
        Snapshot.Match(queryHelper.ToJson());
    }

    /// <summary>
    /// Represents a test item with an Id, Message, and Values.
    /// </summary>
    private class TestItem
    {
        /// <summary>
        /// Gets the minimum Id value. Used in tests as a constant value.
        /// </summary>
        /// <returns>The minimum Id value.</returns>
        public static int GetMinId() => 1;

        /// <summary>
        /// Gets the maximum Id value. Used in tests as a constant value.
        /// </summary>
        /// <returns>The maximum Id value.</returns>
        public static int GetMaxId() => 99;

        /// <summary>
        /// Gets a prefix string. Used in tests for string comparisons.
        /// </summary>
        /// <returns>The prefix string.</returns>
        public static string GetPrefix() => "ab";

        /// <summary>
        /// Gets or initializes the Id of the test item.
        /// </summary>
        [JsonPropertyName("id")]
        public required int Id { get; init; }

        /// <summary>
        /// Gets or initializes the Message of the test item.
        /// </summary>
        [JsonPropertyName("message")]
        public required string Message { get; init; }

        /// <summary>
        /// Gets or initializes the Values of the test item.
        /// </summary>
        [JsonPropertyName("values")]
        public required int[] Values { get; init; }
    }
}