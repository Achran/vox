using FluentValidation.TestHelper;
using Vox.Application.Features.Auth.Commands.Login;

namespace Vox.Application.Tests.Features.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_PassesValidation()
    {
        var command = new LoginCommand("test@example.com", "anypassword");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_WithInvalidEmail_FailsValidation(string email)
    {
        var command = new LoginCommand(email, "anypassword");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithEmptyPassword_FailsValidation()
    {
        var command = new LoginCommand("test@example.com", "");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
