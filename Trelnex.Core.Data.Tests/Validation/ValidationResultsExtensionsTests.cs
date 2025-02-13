using FluentValidation;
using FluentValidation.Results;
using Snapshooter.NUnit;
using Trelnex.Core.Validation;
using ValidationException = Trelnex.Core.Validation.ValidationException;

namespace Trelnex.Core.Data.Tests.Validation;

public class ValidationResultExtensionsTests
{
    [Test]
    public void ValidationResultExtensionsTests_Single()
    {
        var testItem = new TestItem
        {
            Id = -1,
            Message = "no"
        };

        var validatorFirst = new InlineValidator<TestItem>();
        validatorFirst.RuleFor(k => k.Id).Must(k => k > 0);

        var validatorSecond = new InlineValidator<TestItem>();
        validatorSecond.RuleFor(k => k.Message).Must(k => k == "yes");

        var compositeValidator = new CompositeValidator<TestItem>(validatorFirst, validatorSecond);
        var result = compositeValidator.Validate(testItem);

        // this will throw a validation exception
        var ex = Assert.Throws<ValidationException>(
            () => result.ValidateOrThrow<TestItem>())!;

        var o = new
        {
            ex.HttpStatusCode,
            ex.Message,
            ex.Errors
        };

        Snapshot.Match(o);
    }

    [Test]
    public void ValidationResultExtensionsTests_Collection()
    {
        var testItem1 = new TestItem
        {
            Id = -1,
            Message = "no"
        };

        var testItem2 = new TestItem
        {
            Id = 0,
            Message = "maybe"
        };

        var validatorFirst = new InlineValidator<TestItem>();
        validatorFirst.RuleFor(k => k.Id).Must(k => k > 0);

        var validatorSecond = new InlineValidator<TestItem>();
        validatorSecond.RuleFor(k => k.Message).Must(k => k == "yes");

        var compositeValidator = new CompositeValidator<TestItem>(validatorFirst, validatorSecond);
        var result1 = compositeValidator.Validate(testItem1);
        var result2 = compositeValidator.Validate(testItem2);
        var results = new ValidationResult[] { result1, result2 };

        // this will throw a validation exception
        var ex = Assert.Throws<ValidationException>(
            () => results.ValidateOrThrow<TestItem>())!;

        var o = new
        {
            ex.HttpStatusCode,
            ex.Message,
            ex.Errors
        };

        Snapshot.Match(o);
    }
}
