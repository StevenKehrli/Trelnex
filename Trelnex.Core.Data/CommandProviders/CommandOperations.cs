namespace Trelnex.Core.Data;

[Flags]
public enum CommandOperations
{
    None = 0,
    Update = 1,
    Delete = 2,
    All = Update | Delete
}
