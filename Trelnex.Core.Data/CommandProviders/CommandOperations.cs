namespace Trelnex.Core.Data;

/// <summary>
/// Specifies the set of allowed modification operations for command providers.
/// </summary>
/// <remarks>
/// <para>
/// This enum is designed as a flags enum to allow combining multiple operation permissions in a bitwise manner.
/// It is used to configure which operations (update, delete) are permitted for specific entity types 
/// in the command provider system, enabling fine-grained access control at the data layer.
/// </para>
/// <para>
/// Note that Create operations are always permitted and cannot be restricted through this enum, as they are
/// fundamental to the functionality of the data access layer. This restriction capability applies only
/// to modification (Update) and removal (Delete) operations on existing data.
/// </para>
/// <para>
/// A provider configured with <see cref="None"/> will only allow read operations and creation of new items,
/// making it effectively a read/create-only provider. This configuration is useful for entities that should
/// be append-only or immutable after creation.
/// </para>
/// <para>
/// The default configuration for most command providers is <see cref="Update"/> only, which prevents deletion
/// of data while allowing modifications - supporting the common pattern where historical data should be
/// preserved but remain modifiable.
/// </para>
/// <para>
/// When configured with <see cref="All"/>, a command provider allows the full range of data operations,
/// appropriate for entities that should have complete lifecycle management.
/// </para>
/// </remarks>
/// <seealso cref="CommandProvider{TInterface, TItem}"/>
/// <seealso cref="ICommandProvider{TInterface}"/>
[Flags]
public enum CommandOperations
{
    /// <summary>
    /// No modification operations are allowed. The provider will only permit creating new items
    /// and reading existing ones, functioning as a read/create-only provider.
    /// </summary>
    /// <remarks>
    /// This configuration is useful for audit logs, historical records, or other scenarios
    /// where data should be immutable once created.
    /// </remarks>
    None = 0,

    /// <summary>
    /// Update operations are allowed, enabling modification of existing items.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the default configuration for most command providers, allowing items to be modified
    /// after creation but not removed from the system entirely.
    /// </para>
    /// <para>
    /// When update operations are allowed, the <see cref="ICommandProvider{TInterface}.UpdateAsync"/> method
    /// can be used to retrieve an item for modification, and the resulting command can be executed
    /// to persist those changes.
    /// </para>
    /// </remarks>
    Update = 1,

    /// <summary>
    /// Delete operations are allowed, enabling (soft) deletion of existing items.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When delete operations are allowed, the <see cref="ICommandProvider{TInterface}.DeleteAsync"/> method
    /// can be used to mark items as deleted in the system.
    /// </para>
    /// <para>
    /// Note that this implements soft deletion by default, where items are marked as deleted
    /// rather than physically removed from the data store. This preserves the historical record
    /// while making the items appear as if they no longer exist in normal queries.
    /// </para>
    /// </remarks>
    Delete = 2,

    /// <summary>
    /// All modification operations (both Update and Delete) are allowed.
    /// </summary>
    /// <remarks>
    /// This configuration provides the most flexibility but should be used judiciously,
    /// especially for entities where maintaining a historical record or preventing accidental
    /// deletion is important.
    /// </remarks>
    All = Update | Delete
}
