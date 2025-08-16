using System.Text.RegularExpressions;
using FluentValidation;
using LinqToDB;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Data;

namespace Trelnex.Core.Azure.DataProviders;

/// <summary>
/// SQL Server implementation of database data provider with SQL Server-specific error handling.
/// </summary>
/// <param name="dataOptions">LinqToDB connection options for SQL Server.</param>
/// <param name="typeName">Type name identifier for filtering items.</param>
/// <param name="itemValidator">Optional validator for items before saving.</param>
/// <param name="commandOperations">Optional CRUD operations override.</param>
/// <param name="eventTimeToLive">Optional TTL for events in seconds.</param>
/// <param name="logger">Optional logger for diagnostics.</param>
internal partial class SqlDataProvider<TItem>(
    string typeName,
    DataOptions dataOptions,
    IValidator<TItem>? itemValidator = null,
    CommandOperations? commandOperations = null,
    int? eventTimeToLive = null,
    ILogger? logger = null)
    : DbDataProvider<TItem>(
        typeName: typeName,
        dataOptions: dataOptions,
        itemValidator: itemValidator,
        commandOperations: commandOperations,
        eventTimeToLive: eventTimeToLive,
        logger: logger)
    where TItem : BaseItem, new()
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
    /// Gets regex pattern for detecting SQL Server primary key violation errors.
    /// </summary>
    /// <returns>Compiled regex for matching primary key constraint violations.</returns>
    [GeneratedRegex(@"^Violation of PRIMARY KEY constraint ")]
    private static partial Regex PrimaryKeyViolationRegex();

    /// <summary>
    /// Gets regex pattern for detecting SQL Server precondition failed errors.
    /// </summary>
    /// <returns>Compiled regex for matching precondition failed messages.</returns>
    [GeneratedRegex(@"^Precondition Failed\.$")]
    private static partial Regex PreconditionFailedRegex();

    #endregion
}
