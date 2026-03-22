using FluentValidation.TestHelper;
using Vox.Application.Features.Auth.Commands.Register;

namespace Vox.Application.Tests.Features.Auth;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_PassesValidation()
    {
        var command = new RegisterCommand("testuser", "test@example.com", "Test User", "Password1");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void Validate_WithInvalidUserName_FailsValidation(string userName)
    {
        var command = new RegisterCommand(userName, "test@example.com", "Test User", "Password1");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserName);
    }

    [Fact]
    public void Validate_WithUserNameExceedingMaxLength_FailsValidation()
    {
        var command = new RegisterCommand(new string('a', 33), "test@example.com", "Test User", "Password1");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserName);
    }

    [Fact]
    public void Validate_WithInvalidEmail_FailsValidation()
    {
        var command = new RegisterCommand("testuser", "not-an-email", "Test User", "Password1");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithEmptyDisplayName_FailsValidation()
    {
        var command = new RegisterCommand("testuser", "test@example.com", "", "Password1");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Theory]
    [InlineData("short")]          // too short
    [InlineData("alllowercase1")]  // no uppercase
    [InlineData("ALLUPPERCASE1")]  // no lowercase
    [InlineData("NoDigitsHere")]   // no digit
    public void Validate_WithWeakPassword_FailsValidation(string password)
    {
        var command = new RegisterCommand("testuser", "test@example.com", "Test User", password);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
