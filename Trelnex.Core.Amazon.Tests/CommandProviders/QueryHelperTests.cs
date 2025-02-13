using System.Text.Json.Serialization;
using Snapshooter.NUnit;
using Trelnex.Core.Amazon.CommandProviders;

namespace Trelnex.Core.Amazon.Tests.CommandProviders;

public class QueryHelperTests
{
    [Test]
    public void QueryHelper_Where_Id_Equal_Constant()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Id == 1);

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_Id_Equal_Method()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Id == TestItem.GetMaxId());

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_Id_NotEqual_Constant()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Id != 1);

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_Id_NotEqual_Method()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Id != TestItem.GetMaxId());

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_Id_GreaterThan_Constant()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Id > 1);

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_Id_GreaterThan_Method()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Id > TestItem.GetMaxId());

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_Id_GreaterThanOrEqual_Constant()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Id >= 1);

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_Id_GreaterThanOrEqual_Method()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Id >= TestItem.GetMaxId());

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_Id_LessThan_Constant()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Id < 1);

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_Id_LessThan_Method()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Id < TestItem.GetMaxId());

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_Id_LessThanOrEqual_Constant()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Id <= 1);

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_Id_LessThanOrEqual_Method()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Id <= TestItem.GetMaxId());

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_Message_IsNull()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Message == null);

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_Message_IsNotNull()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Message != null);

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_Message_BeginsWith_Constant()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Message.StartsWith("ab"));

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_Message_BeginsWith_Method()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Message.StartsWith( TestItem.GetPrefix() ));

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_Message_Contains_Constant()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Message.Contains("ab"));

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_Message_Contains_Method()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Message.Contains( TestItem.GetPrefix() ));

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_Values_Contains_Constant()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Values.Contains( 1 ));

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_Values_Contains_Method()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Values.Contains( TestItem.GetMaxId() ));

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_Multiple()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Id == 1).Where(x => x.Message == "yes");

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    [Test]
    public void QueryHelper_Where_PlusLinqExpressions()
    {
        var q = Enumerable.Empty<TestItem>().AsQueryable();
        q = q.Where(x => x.Id == 1).OrderBy(x => x.Id).Skip(1).Take(2);

        var queryHelper = QueryHelper<TestItem>.FromLinqExpression(q.Expression);

        Snapshot.Match(queryHelper.ToJson());
    }

    private class TestItem
    {
        public static int GetMinId() => 1;
        public static int GetMaxId() => 99;
        public static string GetPrefix() => "ab";

        [JsonPropertyName("id")]
        public required int Id { get; init; }

        [JsonPropertyName("message")]
        public required string Message { get; init;}

        [JsonPropertyName("values")]
        public required int[] Values { get; init; }
    }
}