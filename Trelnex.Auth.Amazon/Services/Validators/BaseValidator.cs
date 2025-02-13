internal abstract class BaseValidator
{
    /// <summary>
    /// The regular expression for the resource name; e.g. api://amazon.auth.trelnex.com
    /// </summary>
    protected const string _regexResourceName = @"(?<resourceName>(api|http|urn):\/\/[a-z0-9\.\/-]*[a-z0-9]+)";

    /// <summary>
    /// The regular expression for the role name; e.g. service.read
    /// </summary>
    protected const string _regexRoleName = @"(?<roleName>[a-z0-9\.-]+)";

    /// <summary>
    /// The regular expression for the resource name; e.g. .default
    /// </summary>
    protected const string _regexScopeName = @"(?<scopeName>[a-z0-9\.-]+)";
}
