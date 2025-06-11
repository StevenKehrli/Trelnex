namespace Trelnex.Core.Data;

/// <summary>
/// Marks a property for encryption.
/// </summary>
/// <remarks>
/// This attribute indicates that the property should be encrypted when stored.
/// It is typically used for sensitive information such as personal data.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class EncryptAttribute : Attribute;
