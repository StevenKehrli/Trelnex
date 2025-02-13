namespace Trelnex.Core.Identity;

public class AccessTokenUnavailableException(
    string? message,
    Exception? innerException)
    : Exception(message, innerException)
{
}
