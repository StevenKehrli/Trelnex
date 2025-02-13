using FluentValidation;
using Snapshooter.NUnit;
using Trelnex.Core.Validation;

namespace Trelnex.Core.Data.Tests.Validation;

public class ValidatorTests
{

    [Test]
    public void CompositeValidator_BothFail()
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

        Snapshot.Match(result);
    }

    [Test]
    public void CompositeValidator_FirstFails()
    {
        var testItem = new TestItem
        {
            Id = -1,
            Message = "yes"
        };

        var validatorFirst = new InlineValidator<TestItem>();
        validatorFirst.RuleFor(k => k.Id).Must(k => k > 0);

        var validatorSecond = new InlineValidator<TestItem>();
        validatorSecond.RuleFor(k => k.Message).Must(k => k == "yes");

        var compositeValidator = new CompositeValidator<TestItem>(validatorFirst, validatorSecond);
        var result = compositeValidator.Validate(testItem);

        Snapshot.Match(result);
    }

    [Test]
    public void CompositeValidator_NoSecond()
    {
        var testItem = new TestItem
        {
            Id = 1,
            Message = "no"
        };

        var validatorFirst = new InlineValidator<TestItem>();
        validatorFirst.RuleFor(k => k.Id).Must(k => k > 0);

        var compositeValidator = new CompositeValidator<TestItem>(validatorFirst);
        var result = compositeValidator.Validate(testItem);

        Snapshot.Match(result);
    }

    [Test]
    public void CompositeValidator_SecondFails()
    {
        var testItem = new TestItem
        {
            Id = 1,
            Message = "no"
        };

        var validatorFirst = new InlineValidator<TestItem>();
        validatorFirst.RuleFor(k => k.Id).Must(k => k > 0);

        var validatorSecond = new InlineValidator<TestItem>();
        validatorSecond.RuleFor(k => k.Message).Must(k => k == "yes");

        var compositeValidator = new CompositeValidator<TestItem>(validatorFirst, validatorSecond);
        var result = compositeValidator.Validate(testItem);

        Snapshot.Match(result);
    }
}
