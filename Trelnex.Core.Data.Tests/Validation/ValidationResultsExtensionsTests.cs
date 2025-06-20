using FluentValidation;
using FluentValidation.Results;
using Snapshooter.NUnit;
using Trelnex.Core.Validation;
using ValidationException = Trelnex.Core.Validation.ValidationException;

namespace Trelnex.Core.Data.Tests.Validation;

[Category("Validation")]
public class ValidationResultExtensionsTests
{
    [Test]
    [Description("Tests that ValidateOrThrow throws an exception when validating a collection with invalid items")]
    public void ValidationResultExtensionsTests_CollectionThrow()
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

        // The testItem.Id must be greater than 0
        var validatorFirst = new InlineValidator<TestItem>();
        validatorFirst.RuleFor(ti => ti.Id).Must(id => id > 0);

        // The testItem.Message must be "yes"
        var validatorSecond = new InlineValidator<TestItem>();
        validatorSecond.RuleFor(ti => ti.Message).Must(m => m == "yes");

        // Create a composite validator of the above two validators
        // Validate the first test item and the second test item
        // Create a collection of the two ValidationResult
        var compositeValidator = new CompositeValidator<TestItem>(validatorFirst, validatorSecond);
        var result1 = compositeValidator.Validate(testItem1);
        var result2 = compositeValidator.Validate(testItem2);
        var results = new ValidationResult[] { result1, result2 };

        // ValidateOrThrow on the ValidationResult
        // This will throw a validation exception
        // Because the testItem1.Id is -1 and the testItem1.Message is "no"
        // Because the testItem2.Id is 0 and the testItem2.Message is "maybe"
        var ex = Assert.Throws<ValidationException>(
            () => results.ValidateOrThrow<TestItem>())!;

        // Create an anonymous object to hold the exception details
        var o = new
        {
            ex.HttpStatusCode,
            ex.Message,
            ex.Errors
        };

        // Use Snapshooter to match the exception details with the expected output
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests that ValidateOrThrow does not throw an exception when validating a collection with valid items")]
    public void ValidationResultExtensionsTests_CollectionValidate()
    {
        var testItem1 = new TestItem
        {
            Id = 1,
            Message = "yes"
        };

        var testItem2 = new TestItem
        {
            Id = 2,
            Message = "yes"
        };

        // The testItem.Id must be greater than 0
        var validatorFirst = new InlineValidator<TestItem>();
        validatorFirst.RuleFor(ti => ti.Id).Must(id => id > 0);

        // The testItem.Message must be "yes"
        var validatorSecond = new InlineValidator<TestItem>();
        validatorSecond.RuleFor(ti => ti.Message).Must(m => m == "yes");

        // Create a composite validator of the above two validators
        // Validate the first test item and the second test item
        // Create a collection of the two ValidationResult
        var compositeValidator = new CompositeValidator<TestItem>(validatorFirst, validatorSecond);
        var result1 = compositeValidator.Validate(testItem1);
        var result2 = compositeValidator.Validate(testItem2);
        var results = new ValidationResult[] { result1, result2 };

        // ValidateOrThrow on the ValidationResult
        // This will return without throwing an exception
        // Because the testItem.Id is 1 and the testItem.Message is "yes"
        // Because the testItem.Id is 2 and the testItem.Message is "yes"
        Assert.DoesNotThrow(() => results.ValidateOrThrow<TestItem>());
    }

    [Test]
    [Description("Tests that ValidateOrThrow throws an exception when validating a single invalid item")]
    public void ValidationResultExtensionsTests_SingleThrow()
    {
        var testItem = new TestItem
        {
            Id = -1,
            Message = "no"
        };

        // The testItem.Id must be greater than 0
        var validatorFirst = new InlineValidator<TestItem>();
        validatorFirst.RuleFor(ti => ti.Id).Must(id => id > 0);

        // The testItem.Message must be "yes"
        var validatorSecond = new InlineValidator<TestItem>();
        validatorSecond.RuleFor(ti => ti.Message).Must(m => m == "yes");

        // Create a composite validator of the above two validators
        // Validate the test item
        var compositeValidator = new CompositeValidator<TestItem>(validatorFirst, validatorSecond);
        var result = compositeValidator.Validate(testItem);

        // ValidateOrThrow on the ValidationResult
        // This will throw a validation exception
        // Because the testItem.Id is -1 and the testItem.Message is "no"
        var ex = Assert.Throws<ValidationException>(
            () => result.ValidateOrThrow<TestItem>())!;

        // Create an anonymous object to hold the exception details
        var o = new
        {
            ex.HttpStatusCode,
            ex.Message,
            ex.Errors
        };

        // Use Snapshooter to match the exception details with the expected output
        Snapshot.Match(o);
    }

    [Test]
    [Description("Tests that ValidateOrThrow does not throw an exception when validating a single valid item")]
    public void ValidationResultExtensionsTests_SingleValidate()
    {
        var testItem = new TestItem
        {
            Id = 1,
            Message = "yes"
        };

        // The testItem.Id must be greater than 0
        var validatorFirst = new InlineValidator<TestItem>();
        validatorFirst.RuleFor(ti => ti.Id).Must(id => id > 0);

        // The testItem.Message must be "yes"
        var validatorSecond = new InlineValidator<TestItem>();
        validatorSecond.RuleFor(ti => ti.Message).Must(m => m == "yes");

        // Create a composite validator of the above two validators
        // Validate the test item
        var compositeValidator = new CompositeValidator<TestItem>(validatorFirst, validatorSecond);
        var result = compositeValidator.Validate(testItem);

        // ValidateOrThrow on the ValidationResult
        // This will return without throwing an exception
        // Because the testItem.Id is 1 and the testItem.Message is "yes"
        Assert.DoesNotThrow(() => result.ValidateOrThrow<TestItem>());
    }
}
