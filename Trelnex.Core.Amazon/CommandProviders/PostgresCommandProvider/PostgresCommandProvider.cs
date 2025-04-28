using System.Text.RegularExpressions;
using FluentValidation;
using LinqToDB;
using Npgsql;
using Trelnex.Core.Data;

namespace Trelnex.Core.Amazon.CommandProviders;

/// <summary>
/// An implementation of <see cref="ICommandProvider{TInterface}"/> that uses a Postgres table as a backing store.
/// </summary>
internal partial class PostgresCommandProvider<TInterface, TItem> : DbCommandProvider<TInterface, TItem>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresCommandProvider{TInterface, TItem}"/> class.
    /// </summary>
    /// <param name="dataOptions">The data connection options.</param>
    /// <param name="typeName">The type name.</param>
    /// <param name="validator">The validator.</param>
    /// <param name="commandOperations">The command operations.</param>
    public PostgresCommandProvider(
        DataOptions dataOptions,
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null)
        : base(dataOptions, typeName, validator, commandOperations)
    {
    }

    /// <inheritdoc />
    protected override bool IsDatabaseException(Exception ex) =>
        ex is NpgsqlException || ex is PostgresException;

    /// <inheritdoc />
    protected override bool IsPreconditionFailedException(Exception ex) =>
        ex is PostgresException pgEx && PreconditionFailedRegex().IsMatch(pgEx.Message);

    /// <inheritdoc />
    protected override bool IsPrimaryKeyViolationException(Exception ex) =>
        ex is PostgresException pgEx && PrimaryKeyViolationRegex().IsMatch(pgEx.Message);

    [GeneratedRegex(@"^23505: duplicate key value violates unique constraint")]
    private static partial Regex PrimaryKeyViolationRegex();

    [GeneratedRegex(@"^23514: Precondition Failed.$")]
    private static partial Regex PreconditionFailedRegex();
}