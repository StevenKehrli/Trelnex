using System.Net;

namespace Trelnex.Core.Data.Tests.Objects;

[Category("BaseItem")]
public class BaseItemTests
{
    [Test]
    public void ValidateETag_DoesNotThrow_WhenETagsMatch()
    {
        // Arrange
        var item = new TestItem
        {
            ETag = "test-etag-1"
        };

        // Act & Assert
        item.ValidateETag("test-etag-1");
    }

    [Test]
    public void ValidateETag_ThrowsConflict_WhenETagsDontMatch()
    {
        // Arrange
        var item = new TestItem
        {
            ETag = "test-etag-1"
        };

        // Act & Assert
        var exception = Assert.Throws<HttpStatusCodeException>(() => item.ValidateETag("test-etag-2"));
        Assert.That(exception.HttpStatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    private record TestItem : BaseItem
    {
    }
}
