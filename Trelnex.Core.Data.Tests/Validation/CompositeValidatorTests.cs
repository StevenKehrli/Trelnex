using FluentValidation;
using Snapshooter.NUnit;
using Trelnex.Core.Validation;

namespace Trelnex.Core.Data.Tests.Validation;

public class CompositeValidatorTests
{
    [Test]
    [Category("Validation")]
    [Description("Tests that CompositeValidator aggregates validation failures when both validators fail")]
    public void CompositeValidator_BothFail()
    {
        var testItem = new TestItem
        {
            Id = -1,
            Message = "no"
        };

        // the testItem.Id must be greater than 0
        var validatorFirst = new InlineValidator<TestItem>();
        validatorFirst.RuleFor(ti => ti.Id).Must(id => id > 0);

        // the testItem.Message must be "yes"
        var validatorSecond = new InlineValidator<TestItem>();
        validatorSecond.RuleFor(ti => ti.Message).Must(m => m == "yes");

        // create a composite validator of the above two validators
        // validate the test item
        var compositeValidator = new CompositeValidator<TestItem>(validatorFirst, validatorSecond);
        var result = compositeValidator.Validate(testItem);

        // use Snapshooter to match the validation result with the expected output
        Snapshot.Match(result);
    }

    [Test]
    [Category("Validation")]
    [Description("Tests that CompositeValidator returns a successful result when both validators pass")]
    public void CompositeValidator_BothPass()
    {
        var testItem = new TestItem
        {
            Id = 1,
            Message = "yes"
        };

        // the testItem.Id must be greater than 0
        var validatorFirst = new InlineValidator<TestItem>();
        validatorFirst.RuleFor(ti => ti.Id).Must(id => id > 0);

        // the testItem.Message must be "yes"
        var validatorSecond = new InlineValidator<TestItem>();
        validatorSecond.RuleFor(ti => ti.Message).Must(m => m == "yes");

        // create a composite validator of the above two validators
        // validate the test item
        var compositeValidator = new CompositeValidator<TestItem>(validatorFirst, validatorSecond);
        var result = compositeValidator.Validate(testItem);

        // use Snapshooter to match the validation result with the expected output
        Snapshot.Match(result);
    }

    [Test]
    [Category("Validation")]
    [Description("Tests that CompositeValidator reports failures when the first validator fails")]
    public void CompositeValidator_FirstFails()
    {
        var testItem = new TestItem
        {
            Id = -1,
            Message = "yes"
        };

        // the testItem.Id must be greater than 0
        var validatorFirst = new InlineValidator<TestItem>();
        validatorFirst.RuleFor(ti => ti.Id).Must(id => id > 0);

        // the testItem.Message must be "yes"
        var validatorSecond = new InlineValidator<TestItem>();
        validatorSecond.RuleFor(ti => ti.Message).Must(m => m == "yes");

        // create a composite validator of the above two validators
        // validate the test item
        var compositeValidator = new CompositeValidator<TestItem>(validatorFirst, validatorSecond);
        var result = compositeValidator.Validate(testItem);

        // use Snapshooter to match the validation result with the expected output
        Snapshot.Match(result);
    }

    [Test]
    [Category("Validation")]
    [Description("Tests that CompositeValidator works correctly with only one validator")]
    public void CompositeValidator_NoSecond()
    {
        var testItem = new TestItem
        {
            Id = 1,
            Message = "no"
        };

        // the testItem.Id must be greater than 0
        var validatorFirst = new InlineValidator<TestItem>();
        validatorFirst.RuleFor(ti => ti.Id).Must(id => id > 0);

        // create a composite validator with only the first validator
        // validate the test item
        var compositeValidator = new CompositeValidator<TestItem>(validatorFirst);
        var result = compositeValidator.Validate(testItem);

        // use Snapshooter to match the validation result with the expected output
        Snapshot.Match(result);
    }

    [Test]
    [Category("Validation")]
    [Description("Tests that CompositeValidator reports failures when the second validator fails")]
    public void CompositeValidator_SecondFails()
    {
        var testItem = new TestItem
        {
            Id = 1,
            Message = "no"
        };

        // the testItem.Id must be greater than 0
        var validatorFirst = new InlineValidator<TestItem>();
        validatorFirst.RuleFor(ti => ti.Id).Must(id => id > 0);

        // the testItem.Message must be "yes"
        var validatorSecond = new InlineValidator<TestItem>();
        validatorSecond.RuleFor(ti => ti.Message).Must(m => m == "yes");

        // create a composite validator of the above two validators
        // validate the test item
        var compositeValidator = new CompositeValidator<TestItem>(validatorFirst, validatorSecond);
        var result = compositeValidator.Validate(testItem);

        // use Snapshooter to match the validation result with the expected output
        Snapshot.Match(result);
    }
}
