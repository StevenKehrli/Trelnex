# Client-Side Encryption for Document Databases

This document outlines a design pattern for implementing client-side encryption in document databases (specifically Cosmos DB, DynamoDB, and similar NoSQL databases) with LINQ2DB.

## Core Concepts

### 1. Attribute-Based Property Encryption

Use a custom attribute to mark which properties should be encrypted:

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class EncryptedAttribute : Attribute
{
    public string KeyId { get; }

    public EncryptedAttribute(string keyId = "default-key")
    {
        KeyId = keyId;
    }
}
```

This allows developers to easily mark sensitive properties in their model classes:

```csharp
public class Customer
{
    public string Id { get; set; }
    
    public string Name { get; set; }
    
    [Encrypted("customer-ssn-key")]
    public string SocialSecurityNumber { get; set; }
    
    [Encrypted]  // Uses default-key
    public string CreditCardNumber { get; set; }
}
```

### 2. Type-Aware Encryption

The encryption system uses the property's type information to correctly serialize/deserialize values before and after encryption:

```csharp
public static class EncryptionTypeConverters
{
    public static byte[] ToBytes<T>(T value)
    {
        if (value == null) return null;
        
        Type type = typeof(T);
        
        if (type == typeof(string))
            return Encoding.UTF8.GetBytes((string)(object)value);
        
        if (type == typeof(int))
            return BitConverter.GetBytes((int)(object)value);
        
        // Add handling for other types...
        
        // For complex types or collections, use JSON serialization
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
    }
    
    public static T FromBytes<T>(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0) return default;
        
        Type type = typeof(T);
        
        if (type == typeof(string))
            return (T)(object)Encoding.UTF8.GetString(bytes);
        
        if (type == typeof(int))
            return (T)(object)BitConverter.ToInt32(bytes, 0);
        
        // Add handling for other types...
        
        // For complex types or collections, use JSON deserialization
        return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes));
    }
}
```

### 3. Non-Deterministic Encryption Service

Implement encryption with random IVs for maximum security:

```csharp
public interface IEncryptionService
{
    byte[] Encrypt(byte[] data, string keyId);
    byte[] Decrypt(byte[] data, string keyId);
}

public class AesEncryptionService : IEncryptionService
{
    private readonly Dictionary<string, byte[]> _keys;
    
    public AesEncryptionService(IKeyProvider keyProvider)
    {
        _keys = keyProvider.GetKeys();
    }

    public byte[] Encrypt(byte[] data, string keyId)
    {
        if (data == null || data.Length == 0)
            return data;
            
        if (!_keys.TryGetValue(keyId, out byte[] key))
            throw new KeyNotFoundException($"Encryption key '{keyId}' not found");
            
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV(); // Always generate a random IV for non-deterministic encryption
        
        using var encryptor = aes.CreateEncryptor();
        var cipherText = encryptor.TransformFinalBlock(data, 0, data.Length);
        
        // Combine IV and cipherText for storage
        var result = new byte[aes.IV.Length + cipherText.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherText, 0, result, aes.IV.Length, cipherText.Length);
        
        return result;
    }

    public byte[] Decrypt(byte[] data, string keyId)
    {
        if (data == null || data.Length == 0)
            return data;
            
        if (!_keys.TryGetValue(keyId, out byte[] key))
            throw new KeyNotFoundException($"Encryption key '{keyId}' not found");
            
        using var aes = Aes.Create();
        aes.Key = key;
        
        // Extract the IV from the beginning of the data
        var iv = new byte[aes.BlockSize / 8];
        Buffer.BlockCopy(data, 0, iv, 0, iv.Length);
        aes.IV = iv;
        
        // Extract the cipherText (everything after the IV)
        var cipherText = new byte[data.Length - iv.Length];
        Buffer.BlockCopy(data, iv.Length, cipherText, 0, cipherText.Length);
        
        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
    }
}
```

### 4. LINQ2DB Integration via Value Converters

Create value converters that intercept property access during serialization/deserialization:

```csharp
public class EncryptionValueConverter<T> : IValueConverter
{
    private readonly IEncryptionService _encryptionService;
    private readonly string _keyId;

    public EncryptionValueConverter(IEncryptionService encryptionService, string keyId)
    {
        _encryptionService = encryptionService;
        _keyId = keyId;
    }

    public object ConvertToDataSource(object value)
    {
        if (value == null) return null;
        
        // Convert value to bytes based on its type
        byte[] bytes = EncryptionTypeConverters.ToBytes((T)value);
        
        // Encrypt bytes (non-deterministic)
        byte[] encrypted = _encryptionService.Encrypt(bytes, _keyId);
        
        // Return as Base64 string for storage
        return Convert.ToBase64String(encrypted);
    }

    public object ConvertFromDataSource(object value)
    {
        if (value == null || value == DBNull.Value) return default(T);
        
        string stringValue = value as string;
        if (string.IsNullOrEmpty(stringValue)) return default(T);
        
        try
        {
            // Convert from Base64 to bytes
            byte[] encrypted = Convert.FromBase64String(stringValue);
            
            // Decrypt bytes
            byte[] decrypted = _encryptionService.Decrypt(encrypted, _keyId);
            
            // Convert back to original type
            return EncryptionTypeConverters.FromBytes<T>(decrypted);
        }
        catch (Exception ex)
        {
            // Handle decryption errors appropriately
            throw new DataException($"Failed to decrypt value for type {typeof(T).Name}", ex);
        }
    }
}
```

### 5. Integration with LINQ2DB MappingSchema

Create a custom attribute reader to identify and process encrypted properties:

```csharp
public class EncryptedAttributeReader : IMetadataReader
{
    private readonly IEncryptionService _encryptionService;

    public EncryptedAttributeReader(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public T[] GetAttributes<T>(Type type, bool inherit) 
        where T : Attribute
    {
        return Array.Empty<T>();
    }

    public T[] GetAttributes<T>(Type type, MemberInfo memberInfo, bool inherit) 
        where T : Attribute
    {
        return Array.Empty<T>();
    }

    public MemberInfo[] GetDynamicColumns(Type type)
    {
        return Array.Empty<MemberInfo>();
    }

    public void GetColumnInfo(MemberInfo memberInfo, ref string columnName, ref bool isColumn)
    {
        var attr = memberInfo.GetCustomAttribute<EncryptedAttribute>();
        if (attr != null && memberInfo is PropertyInfo propertyInfo)
        {
            EncryptedPropertiesToRegister.Add((propertyInfo, attr.KeyId));
        }
    }

    public readonly List<(PropertyInfo Property, string KeyId)> EncryptedPropertiesToRegister = new();
}

// Extension method for MappingSchema
public static class MappingSchemaExtensions
{
    public static MappingSchema EnableEncryption(this MappingSchema schema, IEncryptionService encryptionService)
    {
        // Create attribute reader
        var encryptionReader = new EncryptedAttributeReader(encryptionService);
        
        // Add it as a metadata reader
        schema.AddMetadataReader(encryptionReader);
        
        // Register callback for entity descriptors
        schema.EntityDescriptorCreated += (_, args) => 
        {
            var descriptor = args.Descriptor;
            var entityType = descriptor.TypeAccessor.Type;
            
            // Find encrypted properties for this entity
            var encryptedProps = encryptionReader.EncryptedPropertiesToRegister
                .Where(p => p.Property.DeclaringType == entityType)
                .ToList();
                
            // Apply converters
            foreach (var (property, keyId) in encryptedProps)
            {
                var propertyType = property.PropertyType;
                
                // Create converter
                var converterType = typeof(EncryptionValueConverter<>).MakeGenericType(propertyType);
                var converter = Activator.CreateInstance(
                    converterType, 
                    encryptionService, 
                    keyId) as IValueConverter;
                
                // Find column and apply converter
                var column = descriptor.Columns.FirstOrDefault(c => c.MemberInfo == property);
                if (column != null)
                {
                    column.SetConverter(converter);
                    column.DataType = DataType.NVarChar;
                    column.Length = 4000; // Adjust as needed
                }
            }
        };
        
        return schema;
    }
}
```

## Setup and Usage

### 1. Configuration

```csharp
// Create the encryption service
var keyProvider = new YourKeyProvider(); // Implement your key provider
var encryptionService = new AesEncryptionService(keyProvider);

// Create and configure mapping schema
var mappingSchema = new MappingSchema();

// Add existing readers (if any)
mappingSchema.AddMetadataReader(new JsonPropertyNameAttributeReader());

// Add encryption support
mappingSchema.EnableEncryption(encryptionService);

// Create fluent mapping builder if needed
var fmBuilder = new FluentMappingBuilder(mappingSchema);

// Build mappings
fmBuilder.Build();

// Configure data options
DataOptions.UseMappingSchema(mappingSchema);
```

### 2. Entity Definition

```csharp
public class Customer
{
    [JsonPropertyName("id")]
    [Column(IsPrimaryKey = true)]
    public string Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("ssn")]
    [Encrypted("customer-ssn-key")]
    public string SocialSecurityNumber { get; set; }
    
    [JsonPropertyName("credit_card")]
    [Encrypted]  // Uses default-key
    public string CreditCardNumber { get; set; }
}
```

### 3. Usage in Service Layer

```csharp
public class CustomerService
{
    private readonly DataConnection _db;
    
    public CustomerService(DataConnection db)
    {
        _db = db;
    }
    
    public async Task<List<Customer>> GetAllCustomersAsync()
    {
        // Encryption/decryption happens transparently
        return await _db.GetTable<Customer>().ToListAsync();
    }
    
    public async Task CreateCustomerAsync(Customer customer)
    {
        // Properties are automatically encrypted before insertion
        await _db.InsertAsync(customer);
    }
    
    // For non-deterministic encryption, filter in memory
    public async Task<List<Customer>> FindCustomersByLastFourCardDigitsAsync(string lastFour)
    {
        var customers = await _db.GetTable<Customer>().ToListAsync();
        return customers
            .Where(c => c.CreditCardNumber != null && 
                  c.CreditCardNumber.EndsWith(lastFour))
            .ToList();
    }
}
```

## Key Considerations

1. **Performance**: Client-side filtering is required for encrypted fields
2. **Column Size**: Encrypted data is larger than plaintext
3. **Key Management**: Implement secure key storage and rotation
4. **Error Handling**: Add appropriate decryption error handling
5. **Complex Types**: Special serialization for nested objects or collections

## Differences from Cosmos DB SDK Implementation

The major differences from the Azure Cosmos DB .NET SDK's implementation:

1. **Type Preservation**: Cosmos DB uses type markers; this approach uses property type information
2. **Encryption Policy**: Cosmos DB uses container-level policies; this uses property-level attributes
3. **Key Management**: Cosmos DB has a two-tier key system (KEK/DEK); our example has a simpler approach
4. **Integration Point**: Cosmos DB intercepts at the request level; this integrates with LINQ2DB's mapping

This approach is more suited for applications using LINQ2DB where you control the model definitions and can use attributes to mark sensitive properties.