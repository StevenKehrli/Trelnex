using System.Text.RegularExpressions;
using FluentValidation;
using LinqToDB;
using Microsoft.Data.SqlClient;
using Trelnex.Core.Data;

namespace Trelnex.Core.Azure.CommandProviders;

/// <summary>
/// SQL Server implementation of <see cref="DbCommandProvider{TInterface, TItem}"/>.
/// </summary>
/// <param name="dataOptions">The data connection options for SQL Server.</param>
/// <param name="typeName">The type name used to filter items.</param>
/// <param name="validator">Optional validator for items before they are saved.</param>
/// <param name="commandOperations">Optional command operations to override default behaviors.</param>
internal partial class SqlCommandProvider<TInterface, TItem>(
    DataOptions dataOptions,
    string typeName,
    IValidator<TItem>? validator = null,
    CommandOperations? commandOperations = null)
    : DbCommandProvider<TInterface, TItem>(dataOptions, typeName, validator, commandOperations)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface, new()
{
    #region Protected Methods

    /// <inheritdoc />
    protected override bool IsDatabaseException(
        Exception exception)
    {
        // Check if the exception is a SqlException.
        return exception is SqlException;
    }

    /// <inheritdoc />
    /// <remarks>Detects precondition failures by matching error message.</remarks>
    protected override bool IsPreconditionFailedException(
        Exception exception)
    {
        // Check if the exception is a SqlException and the message matches the precondition failed regex.
        return exception is SqlException sqlException && PreconditionFailedRegex().IsMatch(sqlException.Message);
    }

    /// <inheritdoc />
    /// <remarks>Detects primary key violations by matching error message.</remarks>
    protected override bool IsPrimaryKeyViolationException(
        Exception exception)
    {
        // Check if the exception is a SqlException and the message matches the primary key violation regex.
        return exception is SqlException sqlException && PrimaryKeyViolationRegex().IsMatch(sqlException.Message);
    }

    #endregion

    #region Regular Expressions

    /// <summary>
    /// Regular expression to identify primary key violation errors.
    /// </summary>
    /// <returns>Compiled regular expression pattern.</returns>
    [GeneratedRegex(@"^Violation of PRIMARY KEY constraint ")]
    private static partial Regex PrimaryKeyViolationRegex();

    /// <summary>
    /// Regular expression to identify precondition failed errors.
    /// </summary>
    /// <returns>Compiled regular expression pattern.</returns>
    [GeneratedRegex(@"^Precondition Failed\.$")]
    private static partial Regex PreconditionFailedRegex();

    #endregion
}
