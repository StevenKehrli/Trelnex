# Validation

The Validation directory provides components built on top of FluentValidation to enable robust validation capabilities throughout Trelnex applications. These components simplify the creation of validation rules and handle validation failures in a consistent way.

## Components

### CompositeValidator\<T>

`CompositeValidator<T>` is a fluent validator that combines multiple validators into a single validation pipeline, allowing for modular and reusable validation logic.

#### Features:
- Inherits from FluentValidation's `AbstractValidator<T>`
- Combines multiple validators into a single validator
- Enables separation of validation concerns into smaller, focused validators
- Supports optional secondary validators

#### Usage:
```csharp
// Create individual validators with specific validation concerns
var domainValidator = new DomainValidator();
var businessRulesValidator = new BusinessRulesValidator();

// Combine them into a composite validator
var compositeValidator = new CompositeValidator<YourModel>(
    domainValidator, 
    businessRulesValidator);

// Use like any other FluentValidator
var result = compositeValidator.Validate(model);
```

### ValidationException

`ValidationException` is a specialized exception that inherits from `HttpStatusCodeException`, designed to represent validation failures with appropriate HTTP status code (422 Unprocessable Content) and structured error details.

#### Features:
- Uses HTTP status code 422 (Unprocessable Content) for validation failures
- Provides structured error information in a consistent format
- Supports optional inner exceptions
- Organizes validation errors by property name for easy client consumption

#### Usage:
```csharp
// Creating a validation exception directly
throw new ValidationException(
    message: "The user data is not valid.",
    errors: new Dictionary<string, string[]> {
        { "email", new[] { "Invalid email format" } }
    });

// More commonly used through extension methods
validationResult.ValidateOrThrow<User>();
```

### ValidatorExtensions

Extends FluentValidation's rule builder with custom validation methods, including:

#### Features:
- `NotDefault<T>` for `DateTime` types - Ensures date values are not default or empty
- `NotDefault<T>` for `Guid` types - Validates that GUIDs are not empty

#### Usage:
```csharp
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.CreatedDate).NotDefault();
        RuleFor(x => x.Id).NotDefault();
    }
}
```

### ValidationResultExtensions

Provides powerful extension methods to handle validation results:

#### Features:
- `ValidateOrThrow` - Converts validation failures into exceptions with well-structured error information:
  - Support for both single-object and collection validation
  - Intelligent error message formatting for collections, maintaining proper indexing
  - Aggregation of multiple validation errors by property name
  - Consistent error response structure

#### Usage:
```csharp
// For single objects
ValidationResult result = validator.Validate(model);
result.ValidateOrThrow<UserModel>();  // Throws if validation failed

// For collections of objects
IEnumerable<ValidationResult> results = items.Select(item => validator.Validate(item));
results.ValidateOrThrow<OrderItem>();  // Throws if any validation failed
```

## Benefits

- **Consistent Validation Handling**: Standardized validation error format across all APIs
- **Modular Design**: Compose validation rules from smaller, reusable validators
- **Clean Controller Code**: Controllers can call `.ValidateOrThrow()` without manual error checking
- **Client-Friendly Errors**: Structured error format makes it easy for clients to display field-specific errors
- **Type-Safe Extensions**: Generic extensions ensure type safety with minimal boilerplate
