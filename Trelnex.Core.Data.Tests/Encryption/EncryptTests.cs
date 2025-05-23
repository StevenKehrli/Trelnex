using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Snapshooter.NUnit;
using Trelnex.Core.Data.Encryption;

namespace Trelnex.Core.Data.Tests.Encryption;

[Category("Encryption")]
public class EncryptTests
{
    [Test]
    public void Encrypt()
    {
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            TypeInfoResolver = new EncryptedJsonTypeInfoResolver(new EncryptionService()),
            WriteIndented = true
        };

        var testItem = new TestItem
        {
            PublicMessage = "public message",
            EncryptMessage = "encrypt message",
            EncryptInt = 10,
            EncryptGuid = Guid.Parse("1e72606a-70a7-4314-b66c-6364d2702767"),
            EncryptDateTimeOffset = DateTimeOffset.Parse("2025-01-02T03:04:05Z"),
            EncryptNestedTestItem = new TestItem.NestedTestItem
            {
                NestedPublicMessage = "nested public message",
                NestedEncryptMessage = "nested encrypt message",
                NestedEncryptInt = 20,
                NestedEncryptGuid = Guid.Parse("5bee4a73-8382-46a7-9d25-9c8c8cdbb101"),
                NestedEncryptDateTimeOffset = DateTimeOffset.Parse("2026-02-03T04:05:06Z")
            }
        };

        var json = JsonSerializer.Serialize(testItem, options);

        Snapshot.Match(json);
    }

    [Test]
    public void Decrypt()
    {
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            // TypeInfoResolver = new EncryptedJsonTypeInfoResolver(new EncryptionService())
        };

        var testItem = new TestItem
        {
            PublicMessage = "public message",
            EncryptMessage = "encrypt message",
            EncryptInt = 10,
            EncryptGuid = Guid.Parse("1e72606a-70a7-4314-b66c-6364d2702767"),
            EncryptDateTimeOffset = DateTimeOffset.Parse("2025-01-02T03:04:05Z"),
            EncryptNestedTestItem = new TestItem.NestedTestItem
            {
                NestedPublicMessage = "nested public message",
                NestedEncryptMessage = "nested encrypt message",
                NestedEncryptInt = 20,
                NestedEncryptGuid = Guid.Parse("5bee4a73-8382-46a7-9d25-9c8c8cdbb101"),
                NestedEncryptDateTimeOffset = DateTimeOffset.Parse("2026-02-03T04:05:06Z")
            }
        };

        var json = JsonSerializer.Serialize(testItem, options);

        var result = JsonSerializer.Deserialize<TestItem>(json, options);

        Snapshot.Match(result);
    }

    private class EncryptionService : IEncryptionService
    {
        public byte[] Encrypt(byte[] data)
        {
            // Simulate encryption by reversing the byte array
            Array.Reverse(data);
            return data;
        }

        public byte[] Decrypt(byte[] data)
        {
            // Simulate decryption by reversing the byte array back
            Array.Reverse(data);
            return data;
        }
    }
}
