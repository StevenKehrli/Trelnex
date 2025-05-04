namespace Trelnex.Core.Identity;

/// <summary>
/// Exception thrown when an access token cannot be retrieved.
/// </summary>
/// <param name="message">A message describing why the access token is unavailable.</param>
/// <param name="innerException">The underlying exception that caused the token unavailability.</param>
public class AccessTokenUnavailableException(
    string? message,
    Exception? innerException)
    : Exception(message, innerException);