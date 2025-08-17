using System.Text.RegularExpressions;
using FluentValidation;
using LinqToDB;
using Microsoft.Extensions.Logging;
using Npgsql;
using Trelnex.Core.Data;

namespace Trelnex.Core.Amazon.DataProviders;

/// <summary>
/// PostgreSQL implementation of database data provider with PostgreSQL-specific error handling.
/// </summary>
/// <param name="dataOptions">LinqToDB connection options for PostgreSQL.</param>
/// <param name="typeName">Type name identifier for filtering items.</param>
/// <param name="itemValidator">Optional validator for items before saving.</param>
/// <param name="commandOperations">Optional CRUD operations override.</param>
/// <param name="eventPolicy">Optional event policy for change tracking.</param>
/// <param name="eventTimeToLive">Optional TTL for events in seconds.</param>
/// <param name="logger">Optional logger for diagnostics.</param>
internal partial class PostgresDataProvider<TItem>(
    string typeName,
    DataOptions dataOptions,
    IValidator<TItem>? itemValidator = null,
    CommandOperations? commandOperations = null,
    EventPolicy? eventPolicy = null,
    int? eventTimeToLive = null,
    ILogger? logger = null)
    : DbDataProvider<TItem>(
        typeName: typeName,
        dataOptions: dataOptions,
        itemValidator: itemValidator,
        commandOperations: commandOperations,
        eventPolicy: eventPolicy,
        eventTimeToLive: eventTimeToLive,
        logger: logger)
    where TItem : BaseItem, new()
{
    #region Protected Methods

    /// <inheritdoc />
    protected override bool IsDatabaseException(
        Exception ex)
    {
        return ex is NpgsqlException || ex is PostgresException;
    }

    /// <inheritdoc />
    protected override bool IsPreconditionFailedException(
        Exception ex)
    {
        return ex is PostgresException pgEx && PreconditionFailedRegex().IsMatch(pgEx.Message);
    }

    /// <inheritdoc />
    protected override bool IsPrimaryKeyViolationException(
        Exception ex)
    {
        return ex is PostgresException pgEx && PrimaryKeyViolationRegex().IsMatch(pgEx.Message);
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Gets regex pattern for detecting PostgreSQL primary key violation errors.
    /// </summary>
    /// <returns>Compiled regex for matching duplicate key constraint violations.</returns>
    [GeneratedRegex(@"^\d{5}: duplicate key value violates unique constraint")]
    private static partial Regex PrimaryKeyViolationRegex();

    /// <summary>
    /// Gets regex pattern for detecting PostgreSQL precondition failed errors.
    /// </summary>
    /// <returns>Compiled regex for matching precondition failed messages.</returns>
    [GeneratedRegex(@"^\d{5}: Precondition Failed.$")]
    private static partial Regex PreconditionFailedRegex();

    #endregion
}