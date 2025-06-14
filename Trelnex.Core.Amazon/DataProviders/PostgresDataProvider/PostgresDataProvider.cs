using System.Text.RegularExpressions;
using FluentValidation;
using LinqToDB;
using Npgsql;
using Trelnex.Core.Data;

namespace Trelnex.Core.Amazon.DataProviders;

/// <summary>
/// PostgreSQL implementation of <see cref="DbDataProvider{TInterface, TItem}"/>.
/// </summary>
/// <param name="dataOptions">The data connection options for PostgreSQL.</param>
/// <param name="typeName">The type name used to filter items.</param>
/// <param name="validator">Optional validator for items before they are saved.</param>
/// <param name="commandOperations">Optional command operations to override default behaviors.</param>
internal partial class PostgresDataProvider<TInterface, TItem>(
    DataOptions dataOptions,
    string typeName,
    IValidator<TItem>? validator = null,
    CommandOperations? commandOperations = null)
    : DbDataProvider<TInterface, TItem>(dataOptions, typeName, validator, commandOperations)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface, new()
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
    /// Regular expression to identify primary key violation errors.
    /// </summary>
    /// <returns>Compiled regular expression pattern.</returns>
    [GeneratedRegex(@"^\d{5}: duplicate key value violates unique constraint")]
    private static partial Regex PrimaryKeyViolationRegex();

    /// <summary>
    /// Regular expression to identify precondition failed errors.
    /// </summary>
    /// <returns>Compiled regular expression pattern.</returns>
    [GeneratedRegex(@"^\d{5}: Precondition Failed.$")]
    private static partial Regex PreconditionFailedRegex();

    #endregion
}