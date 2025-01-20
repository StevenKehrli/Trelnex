using Snapshooter.NUnit;

namespace Trelnex.Core.Data.Tests.Events;

public class UpdateCommandEventTests
{
    private readonly string _typeName = "test-item";

    [Test]
    public async Task UpdateCommandEvent()
    {
        var id = "404d6b21-f7ba-48c4-813c-7d3b5bf4f549";
        var partitionKey = "d9a7a840-ce5c-43c9-9839-a8432068b197";

        var startDateTime = DateTime.UtcNow;

        var requestContext = TestRequestContext.Create();

        // create our command provider
        var factory = await InMemoryCommandProviderFactory.Create();

        var commandProvider = factory.Create<ITestItem, TestItem>(
                typeName: _typeName);

        var createCommand = commandProvider.Create(
            id: id,
            partitionKey: partitionKey);

        createCommand.Item.PublicId = 1;
        createCommand.Item.PublicMessage = "Public #1";
        createCommand.Item.PrivateMessage = "Private #1";

        // save it
        await createCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        var updateCommand = await commandProvider.UpdateAsync(
            id: id,
            partitionKey: partitionKey);

        Assert.That(updateCommand, Is.Not.Null);
        Assert.That(updateCommand!.Item, Is.Not.Null);

        updateCommand.Item.PublicId = 2;
        updateCommand.Item.PublicMessage = "Public #2";
        updateCommand.Item.PrivateMessage = "Private #2";

        // save it
        await updateCommand.SaveAsync(
            requestContext: requestContext,
            cancellationToken: default);

        // get the events
        var events = (commandProvider as InMemoryCommandProvider<ITestItem, TestItem>)!.GetEvents();

        // snapshooter does a poor job of the serialization of dynamic
        // so explicit check of the changes array

        Assert.Multiple(() =>
        {
            Assert.That(
                events[0].Changes!,
                Has.Length.EqualTo(2));

            Assert.That(
                events[0].Changes![0].OldValue!.GetInt32(),
                Is.EqualTo(0));

            Assert.That(
                events[0].Changes![0].NewValue!.GetInt32(),
                Is.EqualTo(1));

            Assert.That(
                events[0].Changes![1].OldValue,
                Is.Null);

            Assert.That(
                events[0].Changes![1].NewValue!.GetString(),
                Is.EqualTo("Public #1"));

            Assert.That(
                events[1].Changes![0].OldValue!.GetInt32(),
                Is.EqualTo(1));

            Assert.That(
                events[1].Changes![0].NewValue!.GetInt32(),
                Is.EqualTo(2));

            Assert.That(
                events[1].Changes![1].OldValue!.GetString(),
                Is.EqualTo("Public #1"));

            Assert.That(
                events[1].Changes![1].NewValue!.GetString(),
                Is.EqualTo("Public #2"));
        });

        Snapshot.Match(
            events,
            matchOptions => matchOptions
                .IgnoreField("**.Changes")
                .Assert(fieldOption =>
                {
                    Assert.Multiple(() =>
                    {
                        var currentDateTime = DateTime.UtcNow;

                        // id
                        Assert.That(
                            fieldOption.Field<Guid>("[0].Id"),
                            Is.Not.Default);

                        // createdDate
                        Assert.That(
                            fieldOption.Field<DateTime>("[0].CreatedDate"),
                            Is.InRange(startDateTime, currentDateTime));

                        // updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("[0].UpdatedDate"),
                            Is.InRange(startDateTime, currentDateTime));

                        // createdDate == updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("[0].CreatedDate"),
                            Is.EqualTo(fieldOption.Field<DateTime>("[0].UpdatedDate")));

                        // _eTag
                        Assert.That(
                            fieldOption.Field<Guid>("[0].ETag"),
                            Is.Not.Default);

                        // context.objectId
                        Assert.That(
                            fieldOption.Field<Guid>("[0].Context.ObjectId"),
                            Is.Not.Default);

                        // context.httpTraceIdentifier
                        Assert.That(
                            fieldOption.Field<Guid>("[0].Context.HttpTraceIdentifier"),
                            Is.Not.Default);

                        // context.httpRequestPath
                        Assert.That(
                            fieldOption.Field<Guid>("[0].Context.HttpRequestPath"),
                            Is.Not.Default);

                        // id
                        Assert.That(
                            fieldOption.Field<Guid>("[1].Id"),
                            Is.Not.Default);

                        // createdDate
                        Assert.That(
                            fieldOption.Field<DateTime>("[1].CreatedDate"),
                            Is.InRange(startDateTime, currentDateTime));

                        // updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("[1].UpdatedDate"),
                            Is.InRange(startDateTime, currentDateTime));

                        // createdDate == updatedDate
                        Assert.That(
                            fieldOption.Field<DateTime>("[1].CreatedDate"),
                            Is.EqualTo(fieldOption.Field<DateTime>("[1].UpdatedDate")));

                        // _eTag
                        Assert.That(
                            fieldOption.Field<Guid>("[1].ETag"),
                            Is.Not.Default);

                        // context.objectId
                        Assert.That(
                            fieldOption.Field<Guid>("[1].Context.ObjectId"),
                            Is.Not.Default);

                        // context.httpTraceIdentifier
                        Assert.That(
                            fieldOption.Field<Guid>("[1].Context.HttpTraceIdentifier"),
                            Is.Not.Default);

                        // context.httpRequestPath
                        Assert.That(
                            fieldOption.Field<Guid>("[1].Context.HttpRequestPath"),
                            Is.Not.Default);

                        // context.objectId
                        Assert.That(
                            fieldOption.Field<Guid>("[0].Context.ObjectId"),
                            Is.EqualTo(fieldOption.Field<Guid>("[1].Context.ObjectId")));

                        // context.httpTraceIdentifier
                        Assert.That(
                            fieldOption.Field<Guid>("[0].Context.HttpTraceIdentifier"),
                            Is.EqualTo(fieldOption.Field<Guid>("[1].Context.HttpTraceIdentifier")));

                        // context.httpRequestPath
                        Assert.That(
                            fieldOption.Field<Guid>("[0].Context.HttpRequestPath"),
                            Is.EqualTo(fieldOption.Field<Guid>("[1].Context.HttpRequestPath")));
                    });
                }));
    }
}
