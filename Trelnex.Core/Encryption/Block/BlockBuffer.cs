namespace Trelnex.Core.Encryption;

/// <summary>
/// Represents a buffer allocated for block cipher operations, containing the byte array and current offset.
/// </summary>
/// <param name="buffer">The byte array that will contain the cipher data.</param>
/// <param name="offset">The initial offset within the buffer where cipher operations should begin.</param>
/// <remarks>
/// <para>
/// This class encapsulates the result of block cipher operations, providing access to the allocated buffer
/// and tracking the current position within that buffer.
/// </para>
/// <para>
/// The <see cref="Offset"/> property can be updated by cipher implementations to reflect the final position
/// after encryption or decryption operations complete.
/// </para>
/// </remarks>
public class BlockBuffer(
    byte[] buffer,
    int offset)
{
    /// <summary>
    /// Gets the byte array that contains or will contain the cipher data.
    /// </summary>
    /// <value>The allocated buffer for encrypted or decrypted data.</value>
    public byte[] Buffer { get; } = buffer;

    /// <summary>
    /// Gets or sets the current offset within the buffer.
    /// </summary>
    /// <value>
    /// The zero-based index indicating the current position within the buffer.
    /// This can be updated by cipher operations to reflect the final write position.
    /// </value>
    public int Offset { get; set; } = offset;
}
