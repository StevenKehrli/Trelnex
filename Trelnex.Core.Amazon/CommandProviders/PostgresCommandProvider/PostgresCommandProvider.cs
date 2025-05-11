using System.Text.RegularExpressions;
using FluentValidation;
using LinqToDB;
using Npgsql;
using Trelnex.Core.Data;

namespace Trelnex.Core.Amazon.CommandProviders;

/// <summary>
/// PostgreSQL implementation of <see cref="DbCommandProvider{TInterface, TItem}"/>.
/// </summary>
/// <typeparam name="TInterface">Interface type for the items.</typeparam>
/// <typeparam name="TItem">Concrete implementation type for the items.</typeparam>
/// <remarks>
/// Provides PostgreSQL-specific exception handling.
/// </remarks>
internal partial class PostgresCommandProvider<TInterface, TItem> : DbCommandProvider<TInterface, TItem>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface, new()
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresCommandProvider{TInterface, TItem}"/> class.
    /// </summary>
    /// <param name="dataOptions">The data connection options for PostgreSQL.</param>
    /// <param name="typeName">Type name to filter items by.</param>
    /// <param name="validator">Optional validator for items.</param>
    /// <param name="commandOperations">Operations allowed for this provider.</param>
    public PostgresCommandProvider(
        DataOptions dataOptions,
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null)
        : base(dataOptions, typeName, validator, commandOperations)
    {
    }

    #endregion

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