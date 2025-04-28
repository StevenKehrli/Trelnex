using System.Text.RegularExpressions;
using FluentValidation;
using LinqToDB;
using Microsoft.Data.SqlClient;
using Trelnex.Core.Data;

namespace Trelnex.Core.Azure.CommandProviders;

/// <summary>
/// An implementation of <see cref="ICommandProvider{TInterface}"/> that uses a SQL table as a backing store.
/// </summary>
internal partial class SqlCommandProvider<TInterface, TItem> : DbCommandProvider<TInterface, TItem>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlCommandProvider{TInterface, TItem}"/> class.
    /// </summary>
    /// <param name="dataOptions">The data connection options.</param>
    /// <param name="typeName">The type name.</param>
    /// <param name="validator">The validator.</param>
    /// <param name="commandOperations">The command operations.</param>
    public SqlCommandProvider(
        DataOptions dataOptions,
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null)
        : base(dataOptions, typeName, validator, commandOperations)
    {
    }

    /// <inheritdoc />
    protected override bool IsDatabaseException(Exception ex) =>
        ex is SqlException;

    /// <inheritdoc />
    protected override bool IsPreconditionFailedException(Exception ex) =>
        ex is SqlException sqlEx && PreconditionFailedRegex().IsMatch(sqlEx.Message);

    /// <inheritdoc />
    protected override bool IsPrimaryKeyViolationException(Exception ex) =>
        ex is SqlException sqlEx && PrimaryKeyViolationRegex().IsMatch(sqlEx.Message);

    [GeneratedRegex(@"^Violation of PRIMARY KEY constraint ")]
    private static partial Regex PrimaryKeyViolationRegex();

    [GeneratedRegex(@"^Precondition Failed\.$")]
    private static partial Regex PreconditionFailedRegex();
}
