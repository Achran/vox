using FluentValidation.TestHelper;
using Vox.Application.Features.Users.Commands.CreateUser;

namespace Vox.Application.Tests;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_PassesValidation()
    {
        // Arrange
        var command = new CreateUserCommand("testuser", "test@example.com", "Test User", "Password123");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserName_FailsValidation()
    {
        // Arrange
        var command = new CreateUserCommand("", "test@example.com", "Test User", "Password123");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserName);
    }

    [Fact]
    public void Validate_WithInvalidEmail_FailsValidation()
    {
        // Arrange
        var command = new CreateUserCommand("testuser", "not-an-email", "Test User", "Password123");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithShortPassword_FailsValidation()
    {
        // Arrange
        var command = new CreateUserCommand("testuser", "test@example.com", "Test User", "short");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WithShortUserName_FailsValidation()
    {
        // Arrange
        var command = new CreateUserCommand("ab", "test@example.com", "Test User", "Password123");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserName);
    }
}
